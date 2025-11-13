IMAGE ?= wishlists:dev

BUN     ?= bun
BUN2NIX ?= bun2nix
DOTNET  ?= dotnet
DOCKER  ?= docker
DPRINT  ?= dprint
NIX     ?= nix

API_DIR   ?= src/UnMango.Wishlists.Api
WEB_DIR   ?= src/web
QUERY_DIR ?= ${API_DIR}/Generated

CS_SRC   != find . -path '*.cs'
TS_SRC   != find . \( -not -path './node_modules/*' \) -path '*.ts'
TSX_SRC  != find . \( -not -path './node_modules/*' \) -path '*.tsx'
PROJ_SRC := ${API_DIR}/UnMango.Wishlists.Api.csproj
API_SRC  := ${CS_SRC} ${PROJ_SRC}
WEB_SRC  := ${TS_SRC} ${TSX_SRC}

export COMPOSE_PROFILES ?= dev

build: api web

api: src/UnMango.Wishlists.Api/bin/Debug/net10.0/UnMango.Wishlists.Api
web: dist/index.html

deps: ${API_DIR}/nix-deps.json ${WEB_DIR}/bun.nix
generate gen: src/web/api/schema.d.ts

lint: .make/bun-lint .make/nix-flake-check
format fmt: .make/nix-fmt .make/dprint-fmt .make/dotnet-format

docker: bin/image.tar
compose:
	$(DOCKER) compose build app
bake: # make bake lol
	$(DOCKER) buildx bake

dotnet-analyzers:
	$(DOTNET) format analyzers

dev:
	$(DOCKER) compose up --build --watch

start:
	$(DOCKER) compose --profile run up --build --watch

stop:
	$(DOCKER) compose --profile run --profile dev down

migration:
	read -p 'Migration name: ' name && \
	$(DOTNET) ef migrations add "$$name" --project ${API_DIR}

clean:
	rm -rf bin dist $(wildcard result*)

precompile-queries: # WIP
	$(DOTNET) ef dbcontext optimize \
	--project ${API_DIR} \
	--output-dir $(notdir ${QUERY_DIR}) \
	--precompile-queries

bin/schema.json:
	$(DOTNET) build ${API_DIR}
	mkdir -p ${@D}
	cp ${API_DIR}/obj/UnMango.Wishlists.Api.json $@

bin/fetch-deps.sh: default.nix flake.nix ${API_DIR}/UnMango.Wishlists.Api.csproj
	$(NIX) build .#api.fetch-deps --out-link $@

bin/image.tar: Dockerfile ${API_SRC} ${WEB_SRC}
	mkdir -p ${@D} && $(DOCKER) build ${CURDIR} \
	--output type=tar,dest=$@ \
	--tag ${IMAGE} \
	--load

src/UnMango.Wishlists.Api/nix-deps.json: bin/fetch-deps.sh
	$< $@
src/UnMango.Wishlists.Api/bin/Debug/net10.0/UnMango.Wishlists.Api: ${API_SRC}
	$(DOTNET) build

src/web/bun.nix: bun.lock
	$(BUN2NIX) --lock-file $< --output-file $@
src/web/api/schema.d.ts: bin/schema.json
	$(BUN)x openapi-typescript $< --output $@

dist/index.html: bun.lock ${WEB_SRC}
	$(BUN) run build

bun.lock: package.json
	$(BUN) install
	@touch $@

.vscode/settings.json: hack/vscode.json
	mkdir -p ${@D} && cp $< $@

.make/dprint-fmt:
	$(DPRINT) fmt
.make/nix-fmt:
	$(NIX) fmt
.make/dotnet-format:
	$(DOTNET) format
.make/jb-cleanupcode:
	$(DOTNET) jb cleanupcode UnMango.Wishlists.slnx

.make/bun-lint:
	$(BUN) run lint
.make/nix-flake-check:
	$(NIX) flake check --all-systems
.make/jb-inspectcode:
	$(DOTNET) jb inspectcode UnMango.Wishlists.slnx
