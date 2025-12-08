# syntax=docker/dockerfile:experimental
FROM --platform=$BUILDPLATFORM golang:1.25.4 AS build

WORKDIR /src

COPY go.mod go.sum ./
RUN go mod download

COPY main.go ./
RUN CGO_ENABLED=0 GOOS=linux go build -o /out/wishlists

FROM scratch

COPY --from=build /out/wishlists /usr/bin/wishlists

CMD ["/usr/bin/wishlists"]
