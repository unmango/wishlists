FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 as build
ARG TARGETARCH

WORKDIR /build
COPY *.csproj .
COPY *packages.lock.json .
RUN dotnet restore -a $TARGETARCH

COPY ./ .
RUN dotnet publish --no-restore -c Release -a $TARGETARCH -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "UnMango.Wishlists.Api.dll"]
