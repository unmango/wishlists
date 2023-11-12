#!/bin/bash
set -eum

root="$(git rev-parse --show-toplevel)"

pushd "$root/api/UnMango.Wishlists.Api"
dotnet build
popd

pushd "$root/app/web"
npm ci
npm run build
popd
