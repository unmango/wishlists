IMAGE ?= wishlists:dev

BUN     ?= bun
BUN2NIX ?= bun2nix
DOTNET  ?= dotnet
DOCKER  ?= docker
DPRINT  ?= dprint
HELM    ?= helm
NIX     ?= nix

.vscode/settings.json: hack/vscode.json
	@mkdir -p .vscode
	cp $< $@
