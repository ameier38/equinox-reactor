FROM mcr.microsoft.com/dotnet/sdk:5.0 as builder

WORKDIR /app

# prevent sending metrics to microsoft
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# update packages
RUN apt-get update && apt-get upgrade -y

# copy projects and build script
COPY src src
COPY EquinoxReactor.sln .
RUN dotnet restore

# set the runtime identifier
ARG RUNTIME_ID=linux-x64
ENV RUNTIME_ID ${RUNTIME_ID}

# publish the server 
RUN dotnet publish -c release -o ./out

FROM mcr.microsoft.com/dotnet/runtime:5.0 as runner

WORKDIR /app

# copy compiled code from build image
COPY --from=builder /app/out .

ENTRYPOINT ["dotnet", "Server.dll"]
