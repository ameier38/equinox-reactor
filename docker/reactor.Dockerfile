FROM mcr.microsoft.com/dotnet/sdk:5.0 as builder

WORKDIR /app

# prevent sending metrics to microsoft
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# update packages
RUN apt-get update && apt-get upgrade -y

# restore tools
COPY .config .config
RUN dotnet tool restore

# install F# dependencies
COPY paket.dependencies paket.lock ./
RUN dotnet paket install
RUN dotnet paket restore

# copy projects and build script
COPY src src
COPY fake.sh .

# set the runtime identifier
ARG RUNTIME_ID=linux-x64
ENV RUNTIME_ID ${RUNTIME_ID}

# build the client and publish the server 
RUN ./fake.sh PublishReactor

FROM mcr.microsoft.com/dotnet/runtime:5.0 as runner

WORKDIR /app

# copy compiled code from build image
COPY --from=builder /app/src/Reactor/out .

ENTRYPOINT ["dotnet", "Reactor.dll"]
