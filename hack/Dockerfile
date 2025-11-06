# syntax=docker/dockerfile:1
FROM oven/bun:1.3.1-slim AS web

WORKDIR /work
COPY package.json bun.lock ./
RUN bun install --frozen-lockfile
COPY vite.config.ts tsconfig*.json index.html public/ ./
COPY src/web/ ./src/web

CMD ["bun", "dev"]

FROM web AS manifest
RUN bun run build

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS api

WORKDIR /work
COPY src/UnMango.Wishlists.Api/packages.lock.json ./
COPY src/UnMango.Wishlists.Api/*.csproj ./
RUN dotnet restore
COPY --from=manifest /work/dist/.vite/manifest.json ./
COPY src/UnMango.Wishlists.Api/ ./

CMD ["dotnet", "run", "--no-restore"]
