# Wishlists

This is an application for sharing wishlists between a group of people!
Create your own wishlist, share it with your group, and check off items from their wishlist as they're bought.

## Architecture

C# ASP.NET Web API
Create React App Static Web Application

|Role|Location|
|:---|:-------|
|API|[`/api/UnMango.Wishlists.Api/`](./api/UnMango.Wishlists.Api/)|
|Web App|[`/app/web/`](./app/web/)|

## API

The API documentation can be found [here](./api/UnMango.Wishlists.Api/README.md).

## Building

As a convencience, scripts are provided to build both applications.

```shell
./hack/build.sh
```

```shell
pwsh ./hack/build.ps1
```

### Web App

```shell
cd app/web
npm ci
npm run build
```

## Running

As a convencience, scripts are provided to start both applications.

```shell
./hack/start.sh
```

```shell
pwsh ./hack/start.ps1
```

### Web App

```shell
cd app/web
npm ci
npm run start
```
