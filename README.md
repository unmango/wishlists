# Wishlists

This is an application for sharing wishlists between a group of people!
Create your own wishlist, share it with your group, and check off items from their wishlist as they're bought.
The owner of a list never sees what's been bought.

## Architecture

| Role    | Location                                                                   |
| :------ | :------------------------------------------------------------------------- |
| API     | [`src/UnMango.Wishlists.Api/`](./src/UnMango.Wishlists.Api/)               |
| Web App | [`src/web/`](./src/web/)                                                   |
| Tests   | [`test/UnMango.Wishlists.Api.Tests/`](./test/UnMango.Wishlists.Api.Tests/) |

The API is ASP.NET with Marten event sourcing on Postgres.
The web app is React, Vite and Tailwind, served by the API in production.

## Sign-in

Members sign in with Google. Only emails on the allowlist get in.

| Setting                          | Environment variable                      | Notes                                                                     |
| :------------------------------- | :---------------------------------------- | :------------------------------------------------------------------------ |
| `Auth:Google:ClientId`           | `Auth__Google__ClientId`                  | Required in production                                                    |
| `Auth:Google:ClientSecret`       | `Auth__Google__ClientSecret`              | Required in production                                                    |
| `Auth:AllowedEmails`             | `Auth__AllowedEmails__0`, ...             | Inline list                                                               |
| `Auth:AllowedEmailsFile`         | `Auth__AllowedEmailsFile`                 | One email per line, re-read on every sign-in                              |
| `ConnectionStrings:App`          | `ConnectionStrings__App`                  | Postgres                                                                  |
| `ForwardedHeaders:KnownNetworks` | `ForwardedHeaders__KnownNetworks__0`, ... | Ingress CIDRs trusted for `X-Forwarded-*`; any peer is trusted when unset |

Environment variables may also be prefixed with `UNMANGO_`.
The Google OAuth client's redirect URL is `https://<host>/signin-google`.

In Development, use `/auth/dev-login?email=you@example.com` to sign in without Google.

## Building and testing

```shell
make build
make test
```

Tests run against Postgres. They start one with Testcontainers, or use `WISHLISTS_TEST_CONNECTION_STRING` if set.

## Running

```shell
make dev
```
