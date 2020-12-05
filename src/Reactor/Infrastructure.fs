namespace Reactor

open EventStore.ClientAPI
open System

[<RequireQualifiedAccess>]
type Checkpoint =
    | StreamStart
    | StreamPosition of int64

// ref: https://github.com/jet/equinox/blob/729146e5af90d6794a78ecc8c459eca195b38a8b/src/Equinox.EventStore/EventStore.fs#L297
module UnionEncoderAdapters =
    let encodedEventOfResolvedEvent (resolvedEvent:ResolvedEvent): FsCodec.ITimelineEvent<byte[]> =
        let event = resolvedEvent.Event
        let ts = DateTimeOffset.FromUnixTimeMilliseconds(event.CreatedEpoch)
        FsCodec.Core.TimelineEvent.Create(
            resolvedEvent.OriginalEventNumber,
            event.EventType,
            event.Data,
            event.Metadata,
            event.EventId,
            timestamp = ts)
