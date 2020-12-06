module Reactor.Subscription

open EventStore.ClientAPI
open FSharp.Control
open Serilog
open Shared
open System
open System.Threading
open System.Threading.Tasks

type EventHandler = FsCodec.ITimelineEvent<byte []> -> Async<unit>

[<RequireQualifiedAccess>]
type SubscriptionStatus =
    | Subscribed
    | Unsubscribed

type SubscriptionState =
    { Checkpoint: Checkpoint
      CancellationToken: CancellationToken
      SubscriptionStatus: SubscriptionStatus }

type SubscriptionMessage =
    | Subscribe
    | Subscribed of EventStoreStreamCatchUpSubscription
    | Dropped of SubscriptionDropReason * Exception
    | EventAppeared of Checkpoint
    | GetState of AsyncReplyChannel<SubscriptionState>

type SubscriptionMailbox = MailboxProcessor<SubscriptionMessage>

type EventStoreDBSubscription(eventStoreDBConfig: EventStoreDBConfig,
                              name: string,
                              stream: FsCodec.StreamName,
                              eventHandler: EventHandler) =
    let log = Log.ForContext<EventStoreDBSubscription>()
    let conn = EventStoreConnection.Create(Uri(eventStoreDBConfig.Url))

    do conn.ConnectAsync().Wait()

    let creds =
        SystemData.UserCredentials(username = eventStoreDBConfig.User, password = eventStoreDBConfig.Password)

    let settings =
        CatchUpSubscriptionSettings
            (maxLiveQueueSize = 100,
             readBatchSize = 10,
             verboseLogging = false,
             // NB: this is important if reading from projections (e.g. $ce-<category>)
             resolveLinkTos = true,
             subscriptionName = name)

    let subscribe (state: SubscriptionState) (mailbox: SubscriptionMailbox) =
        let eventAppreared =
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task>(fun _ resolvedEvent ->
                let work =
                    async {
                        let encodedEvent = UnionEncoderAdapters.encodedEventOfResolvedEvent resolvedEvent
                        let checkpoint = Checkpoint.StreamPosition encodedEvent.Index
                        do! eventHandler encodedEvent
                        mailbox.Post(EventAppeared checkpoint)
                    }

                Async.StartAsTask(work, cancellationToken = state.CancellationToken) :> Task)

        let subscriptionDropped =
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, exn>(fun _ reason error ->
                mailbox.Post(Dropped(reason, error)))

        log.Debug("Subscribing to {Stream} from checkpoint {Checkpoint}", stream, state.Checkpoint)

        let lastCheckpoint =
            match state.Checkpoint with
            | Checkpoint.StreamStart -> StreamCheckpoint.StreamStart
            | Checkpoint.StreamPosition pos -> Nullable(pos)

        let streamName = FsCodec.StreamName.toString stream

        let subscription =
            conn.SubscribeToStreamFrom
                (settings = settings,
                 stream = streamName,
                 lastCheckpoint = lastCheckpoint,
                 eventAppeared = eventAppreared,
                 subscriptionDropped = subscriptionDropped,
                 userCredentials = creds)

        mailbox.Post(Subscribed subscription)

    let evolve (mailbox: SubscriptionMailbox): SubscriptionState -> SubscriptionMessage -> SubscriptionState =
        fun state msg ->
            match msg with
            | Subscribe ->
                match state.SubscriptionStatus with
                | SubscriptionStatus.Unsubscribed ->
                    subscribe state mailbox
                    state
                | _ -> state
            | Subscribed _ ->
                { state with
                      SubscriptionStatus = SubscriptionStatus.Subscribed }
            | Dropped (reason, error) ->
                match reason with
                | SubscriptionDropReason.ServerError
                | SubscriptionDropReason.EventHandlerException
                | SubscriptionDropReason.ProcessingQueueOverflow
                | SubscriptionDropReason.CatchUpError
                | SubscriptionDropReason.ConnectionClosed ->
                    log.Debug(error, "Subscription dropped: {Reason}; reconnecting...", reason)
                    Thread.Sleep(1000)
                    mailbox.Post(Subscribe)
                | _ ->
                    log.Error(error, "Subscription dropped {Reason}", reason)
                    raise error
                { state with
                      SubscriptionStatus = SubscriptionStatus.Unsubscribed }
            | EventAppeared checkpoint -> { state with Checkpoint = checkpoint }
            | GetState channel ->
                channel.Reply(state)
                state

    let start (initialState: SubscriptionState) =
        let mailbox =
            SubscriptionMailbox.Start((fun inbox ->
                AsyncSeq.initInfiniteAsync (fun _ -> inbox.Receive())
                |> AsyncSeq.fold (evolve inbox) initialState
                |> Async.Ignore), cancellationToken = initialState.CancellationToken)

        mailbox.Post(Subscribe)
        mailbox

    let rec watch (mailbox: SubscriptionMailbox) =
        async {
            let! state = mailbox.PostAndAsyncReply(GetState)
            log.Debug("Stream {Stream} is at checkpoint {Checkpoint}", stream, state.Checkpoint)
            do! Async.Sleep 10000
            return! watch mailbox
        }

    member _.SubscribeAsync(checkpoint: Checkpoint, token: CancellationToken) =
        async {
            let initialState =
                { Checkpoint = checkpoint
                  CancellationToken = token
                  SubscriptionStatus = SubscriptionStatus.Unsubscribed }

            let mailbox = start initialState
            do! watch mailbox
        }
