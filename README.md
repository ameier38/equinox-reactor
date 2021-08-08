# F# Equinox Reactor
Example system to show how to build a _reactor_ using Equinox and Propulsion libraries.

It is also an example of a system that uses an end-to-end write path as described by
Martin Kleppmann in [Designing Data-Intensive Applications](https://dataintensive.net/):

> It would be very natural to extend this programming model to also allow a server to
push state-change events into this client-side event pipeline. Thus, state changes
could flow through an end-to-end write path: from the interaction on one device that
triggers a state change, via event logs and through several derived data systems and
stream processors, all the way to the user interface of a person observing the state
on another device. These state changes could be propagated with fairly low delay — say,
under one second end to end.

## Demo
![demo](./etc/images/demo.gif)

## Architecture

Context

![context](./etc/diagrams/images/context/context.png)

Server

![server](./etc/diagrams/images/server/server.png)

## Setup
Restore tools.
```
dotnet tool restore
```
Create store container.
```
dotnet eqx init --rus 400 cosmos -s $conn -d test -c test
```
Create lease container.
```
dotnet propulsion init --rus 400 cosmos -s $conn -d test -c test
```

## Usage
Build the services.
```shell
docker-compose build
```

Start the services.
```shell
docker-compose up -d
```

## Development
Install tools.
```
dotnet tool restore
```

Build targets.
```
❯ .\fake.cmd
The following targets are available:
The following targets are available:
   BuildClient
   Clean        
   CleanClient  
   InstallClient
   PublishServer
   Restore
   TestIntegrations
   TestIntegrationsHeadless
   Watch
   WatchClient
   WatchServer
```
