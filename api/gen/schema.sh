#!/bin/bash
set -eum

if ! command -v dotnet; then
	echo "Install the .NET SDK first. https://dotnet.microsoft.com/en-us/download"
	exit 1
fi

root="$(git rev-parse --show-toplevel)"
trap popd EXIT
pushd "$root/api/UnMango.Wishlists.Api"
schemaFile="$root/api/schema.graphql"
dotnet run -- schema export --output "$schemaFile"
echo '' >> "$schemaFile" # Fix line ending
