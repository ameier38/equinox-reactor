FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

# set build variables
ARG PROCESSOR_SCHEME=http
ARG PROCESSOR_HOST=localhost
ARG PROCESSOR_PORT=8081
ARG READER_SCHEME=http
ARG READER_HOST=localhost
ARG READER_PORT=8082

WORKDIR /app

ENV DEBIAN_FRONTEND=noninteractive \
    # prevent sending metrics to microsoft
    DOTNET_CLI_TELEMETRY_OPTOUT=1

# install tools
COPY .config .config
RUN dotnet tool restore

# install Node
RUN curl -sL https://deb.nodesource.com/setup_12.x | bash - \
    && apt-get install -y nodejs

# install dependencies
COPY paket.dependencies .
COPY paket.lock .
RUN dotnet paket install

COPY package.json .
COPY package-lock.json .
RUN npm install

# build application
COPY src src
COPY webpack.config.js .
COPY build.fsx .
RUN dotnet fake build -t BuildClient

FROM nginx:1.17-alpine as runner

COPY --from=builder /app/dist /var/www
COPY nginx.conf /etc/nginx/conf.d/default.conf
