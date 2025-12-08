IMAGE ?= wishlists:dev

BUN     ?= bun
BUN2NIX ?= bun2nix
DOTNET  ?= dotnet
DOCKER  ?= docker
DPRINT  ?= dprint
HELM    ?= helm
NIX     ?= nix

apphost: bin/apphost
deps: src/apphost/deps.nix

bin/apphost:
	$(NIX) build .#apphost --out-link $@
src/apphost/deps.nix: bin/apphost-deps.sh
	$< $@
bin/apphost-deps.sh: $(addprefix src/apphost/,apphost.csproj default.nix)
	$(NIX) build .#apphost.fetch-deps --out-link $@

.vscode/settings.json: hack/vscode.json
	@mkdir -p .vscode
	cp $< $@
