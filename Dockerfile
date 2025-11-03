# syntax=docker/dockerfile:1
FROM oven/bun:1.3.1-slim AS web-src

WORKDIR /build

COPY package.json bun.lock ./
RUN bun install --frozen-lockfile

COPY vite.config.ts tsconfig*.json index.html public/ ./
COPY src/web/ ./src/web/

FROM web-src AS web
RUN bun run build

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS api-src

RUN apt-get update && apt-get install -y --no-install-recommends \
	clang zlib1g-dev \
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /build
COPY src/UnMango.Wishlists.Api/UnMango.Wishlists.Api.csproj .
# https://github.com/dotnet/sdk/issues/40517
RUN dotnet restore

COPY src/UnMango.Wishlists.Api/ ./

FROM api-src AS api
ARG CONFIGURATION=Release
WORKDIR /build

COPY --from=web /build/dist ./wwwroot

RUN dotnet publish \
    --no-restore \
    --configuration $CONFIGURATION \
    --output /out

# TODO: EF Core AOT is borked
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble AS aot
WORKDIR /app
COPY --from=api /out ./
ENTRYPOINT ["/app/UnMango.Wishlists.Api"]

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble AS app
COPY --from=api /out /app
ENTRYPOINT ["/app/UnMango.Wishlists.Api"]
