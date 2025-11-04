IMAGE ?= wishlists:dev

BUN    ?= bun
DOTNET ?= dotnet
DOCKER ?= docker
DPRINT ?= dprint

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

generate gen: src/web/api/schema.d.ts

lint: eslint
format fmt:
	$(DPRINT) fmt
	$(DOTNET) format

docker: bin/image.tar
compose:
	$(DOCKER) compose build
bake: # make bake lol
	$(DOCKER) buildx bake

eslint:
	$(BUN) run lint

dotnet-analyzers:
	$(DOTNET) format analyzers

start:
	$(DOCKER) compose --profile run up --build

dev:
	$(DOCKER) compose up --build

stop:
	$(DOCKER) compose --profile run --profile dev down

migration:
	read -p 'Migration name: ' name && \
	$(DOTNET) ef migrations add "$$name" --project ${API_DIR}

precompile-queries: # WIP
	$(DOTNET) ef dbcontext optimize \
	--project ${API_DIR} \
	--output-dir $(notdir ${QUERY_DIR}) \
	--precompile-queries

bin/schema.json:
	$(DOTNET) build ${API_DIR} && mkdir -p ${@D} && cp ${API_DIR}/obj/UnMango.Wishlists.Api.json $@

bin/image.tar: Dockerfile ${API_SRC} ${WEB_SRC}
	mkdir -p ${@D} && $(DOCKER) build ${CURDIR} \
	--output type=tar,dest=$@ \
	--tag ${IMAGE} \
	--load

src/UnMango.Wishlists.Api/bin/Debug/net10.0/UnMango.Wishlists.Api: ${API_SRC}
	$(DOTNET) build

src/web/api/schema.d.ts: bin/schema.json
	$(BUN)x openapi-typescript $< --output $@

dist/index.html: bun.lock ${WEB_SRC}
	$(BUN) run build

bun.lock: package.json
	$(BUN) install
	@touch $@

.vscode/settings.json: hack/vscode.json
	mkdir -p ${@D} && cp $< $@
