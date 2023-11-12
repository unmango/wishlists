#!/usr/bin/env pwsh

$root=git rev-parse --show-toplevel

try {
	pushd $root
	docker compose up
}
finally {
	popd
}
