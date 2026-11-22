#!/usr/bin/env bash
# Draws every app icon from the SVG master, public/icons/icon.svg:
#
#   1. The PNGs for the manifest and the iPhone home screen. The master keeps
#      its art inside the central 80% circle, so one drawing serves both the
#      "any" icons and the maskable one.
#   2. public/favicon.svg, a copy of the master with rounded corners for the
#      browser tab.
#
# Run from the repo root after you change the master, and commit what it draws.
# Needs nix (with flakes): the script takes resvg from the flake's nixpkgs.
set -euo pipefail

# Re-run this script once inside a nix shell that provides resvg.
if [[ -z "${IN_RENDER_SHELL:-}" ]]; then
	IN_RENDER_SHELL=1 exec nix shell --inputs-from . nixpkgs#resvg --command "$0" "$@"
fi

master=public/icons/icon.svg

render() {
	local size=$1 out=$2
	resvg --width "$size" --height "$size" "$master" "$out"
	echo "Drew $out (${size}x${size})"
}

# 1. PNGs.
render 192 public/icons/icon-192.png
render 512 public/icons/icon-512.png
render 512 public/icons/maskable-512.png
render 180 public/apple-touch-icon.png

# 2. Favicon: round the corners of the master's background square.
sed 's|<rect width="100" height="100" fill|<rect width="100" height="100" rx="20" fill|' \
	"$master" >public/favicon.svg
echo "Drew public/favicon.svg"
