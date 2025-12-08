# syntax=docker/dockerfile:experimental
FROM --platform=$BUILDPLATFORM oven/bun:1.3.4 AS web

WORKDIR /src

COPY package.json bun.lock ./
RUN bun install --frozen-lockfile

COPY src/web ./src/web
COPY index.html tsconfig.* vite.config.ts ./
RUN bun run build

FROM --platform=$BUILDPLATFORM golang:1.25.5 AS app

WORKDIR /src

COPY go.mod go.sum ./
RUN go mod download

COPY main.go ./
RUN CGO_ENABLED=0 GOOS=linux go build -o /out/wishlists

FROM scratch

COPY --from=web /src/dist /wwwroot
COPY --from=app /out/ /usr/bin/

EXPOSE 8080

CMD ["/usr/bin/wishlists"]
