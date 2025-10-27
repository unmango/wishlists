# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS base
ARG TARGETOS
ARG TARGETARCH
ARG CONFIGURATION=Release

RUN apt-get update && apt-get install -y --no-install-recommends \
	clang zlib1g-dev \
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /build

# IDK why this is being terrible
#COPY src/UnMango.Wishlists.Api/UnMango.Wishlists.Api.csproj .
#RUN dotnet restore

COPY src/UnMango.Wishlists.Api/* ./

RUN dotnet publish \
    --use-current-runtime \
    --configuration $CONFIGURATION \
    --output /out

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble AS final
WORKDIR /app
COPY --from=base /out ./
ENTRYPOINT ["/app/UnMango.Wishlists.Api"]
