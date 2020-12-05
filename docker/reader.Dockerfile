FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

ENV DEBIAN_FRONTEND=noninteractive \
    # prevent sending metrics to microsoft
    DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /app

# install tools
COPY .config .config
RUN dotnet tool restore

# install dependencies
COPY paket.dependencies .
COPY paket.lock .
RUN dotnet paket install

# copy everything else and build
COPY build.fsx .
COPY src src
RUN dotnet fake build -t PublishReader

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 as runner

WORKDIR /app

COPY --from=builder /app/src/Reader/out .

ENTRYPOINT [ "dotnet", "Reader.dll" ]
