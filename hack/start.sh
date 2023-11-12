#!/bin/bash
set -eum

root="$(git rev-parse --show-toplevel)"

pushd $root
trap popd EXIT
docker compose up
