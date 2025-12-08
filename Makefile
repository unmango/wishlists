IMAGE ?= wishlists:dev

BUN       ?= bun
BUN2NIX   ?= bun2nix
DOTNET    ?= dotnet
DOCKER    ?= docker
DPRINT    ?= dprint
GO        ?= go
GOMOD2NIX ?= $(GO) tool gomod2nix
HELM      ?= helm
NIX       ?= nix

build: bin/wishlists
docker: bin/docker.tar
apphost: bin/apphost
deps: src/apphost/deps.nix gomod2nix.toml

dev:
	$(DOTNET) run --project ${CURDIR}/src/apphost

bun.nix: bun.lock package.json
	$(BUN2NIX) --lock-file $< -o $@

gomod2nix.toml: go.mod go.sum
	$(GOMOD2NIX)

bin/wishlists: | result/bin/wishlists
	ln -s $$(readlink -f $|) $@

result/bin/wishlists: default.nix flake.* main.go
	$(NIX) build .#wishlists
bin/docker.tar: Dockerfile go.mod go.sum main.go package.json bun.lock $(wildcard src/web/*)
	$(DOCKER) build ${CURDIR} -o type=tar,dest=$@ -t ${IMAGE} --load

bin/apphost:
	$(NIX) build .#apphost --out-link $@
src/apphost/deps.nix: bin/apphost-deps.sh
	$< $@
bin/apphost-deps.sh: $(addprefix src/apphost/,apphost.csproj default.nix)
	$(NIX) build .#apphost.fetch-deps --out-link $@

.vscode/settings.json: hack/vscode.json
	@mkdir -p .vscode
	cp $< $@
