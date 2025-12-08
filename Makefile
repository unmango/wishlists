IMAGE ?= wishlists:dev

BUN     ?= bun
BUN2NIX ?= bun2nix
DOTNET  ?= dotnet
DOCKER  ?= docker
DPRINT  ?= dprint
HELM    ?= helm
NIX     ?= nix

bin/apphost-deps.sh: $(addprefix src/apphost/,apphost.csproj default.nix)
	$(NIX) build .#apphost.fetch-deps --out-link $@

.vscode/settings.json: hack/vscode.json
	@mkdir -p .vscode
	cp $< $@
