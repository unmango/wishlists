#!/usr/bin/env bash
# Brings everything that depends on the .NET version in line with global.json,
# so CI can build a Renovate .NET bump (patch, minor or major) without hand edits:
#
#   1. Moves the target framework, the nix SDK/runtime, the Docker images and
#      the Makefile's output path to global.json's major version.
#   2. Updates the flake's nixpkgs if it doesn't have that SDK yet, or only a
#      release candidate of it.
#   3. Refreshes packages.lock.json when `dotnet restore --locked-mode` reports an error,
#      e.g. on a major bump, where Renovate's own restore can't finish the job because
#      the new packages don't support the old target framework (NU1202).
#   4. Regenerates nix-deps.json from the lock files.
#
# Run from the repo root. Needs nix (with flakes) and jq.
set -euo pipefail

major=$(jq -r .sdk.version global.json | cut -d. -f1)
tfm="net${major}.0"
sdk="dotnetCorePackages.sdk_${major}_0"
echo "Syncing to .NET ${major} (${tfm})"

# 1. Everything that names the .NET version.
sed -i -E "s|<TargetFramework>net[0-9]+\.0</TargetFramework>|<TargetFramework>${tfm}</TargetFramework>|" \
	src/*/*.csproj test/*/*.csproj
sed -i -E "s/dotnetCorePackages\.(sdk|aspnetcore)_[0-9]+_0/dotnetCorePackages.\1_${major}_0/g" \
	default.nix shell.nix
sed -i -E "s|(mcr\.microsoft\.com/dotnet/[a-z]+):[0-9]+\.0|\1:${major}.0|g" \
	Dockerfile hack/Dockerfile
sed -i -E "s|bin/Debug/net[0-9]+\.0/|bin/Debug/${tfm}/|g" Makefile

# 2. The flake's nixpkgs must ship this SDK, or nix can't build the app.
# Right after a major release, nixpkgs may only have a release candidate,
# whose runtime can't run the GA build tools (dotnet-getdocument), so try a
# newer nixpkgs for that too.
nix_sdk_version() {
	nix eval --raw --inputs-from . "nixpkgs#${sdk}.version" 2>/dev/null
}
if ! version=$(nix_sdk_version) || [[ "$version" == *-* ]]; then
	echo "The flake's nixpkgs has ${version:-no ${sdk}}; updating nixpkgs"
	nix flake update nixpkgs
	if ! version=$(nix_sdk_version); then
		echo "::error::nixpkgs doesn't ship ${sdk} yet. Wait for it, or package the SDK by hand." >&2
		exit 1
	fi
	if [[ "$version" == *-* ]]; then
		echo "::warning::nixpkgs only has .NET SDK ${version}, so the nix build may fail until it ships the release."
	fi
fi

# 3. Restore with the nix SDK, the same one the nix build uses. It can be a
# different patch (or a release candidate) than global.json pins, so move
# global.json aside while restoring, like default.nix does. The lock files
# don't record the SDK patch.
dotnet=(nix shell --inputs-from . "nixpkgs#${sdk}" --command dotnet)
mv global.json global.json.bak
trap 'mv global.json.bak global.json' EXIT
export DOTNET_NOLOGO=true DOTNET_CLI_TELEMETRY_OPTOUT=true
if ! "${dotnet[@]}" restore --locked-mode >/dev/null; then
	echo "Lock files are stale; refreshing them"
	"${dotnet[@]}" restore --force-evaluate
fi
mv global.json.bak global.json
trap - EXIT

# 4. nix-deps.json, from what the lock files now resolve to.
nix build .#api.fetch-deps --out-link fetch-deps
./fetch-deps src/UnMango.Wishlists.Api/nix-deps.json
rm fetch-deps
