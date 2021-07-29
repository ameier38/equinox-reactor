# F# EventStoreDB Subscription
Example project to demonstrate how to use the F# MailboxProcessor
to manage an EventStoreDB subscription as part of a 'Reactor'
in an event sourced system.

## Demo
![demo](./etc/images/demo.gif)

## Architecture

![context](./etc/diagrams/images/context/context.png)

## Usage
Build the services.
```shell
docker-compose build
```

Start the services.
```shell
docker-compose up -d eventstore redis processor reactor reader client
```

## Development
Install tools.
```
dotnet tool restore
```

Build targets.
```
❯ dotnet fake build --list
The last restore is still up to date. Nothing left to do.
The following targets are available:
   BuildClient
   Clean
   Default
   PublishIntegrationTests
   PublishProcessor
   PublishReactor
   PublishReader
   Restore
   StartClient
   StartProcessor
   StartReactor
   StartReader
   TestIntegrations
   TestUnits
```

## Resources
- [.NET Core Generic Host](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host)
