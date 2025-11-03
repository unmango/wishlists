IMAGE ?= wishlists:dev

BUN    ?= bun
DOTNET ?= dotnet
DOCKER ?= docker
DPRINT ?= dprint

API_PATH ?= src/UnMango.Wishlists.Api
WEB_PATH ?= src/web

CS_SRC   != find . -path '*.cs'
TS_SRC   != find . \( -not -path './node_modules/*' \) -path '*.ts'
TSX_SRC  != find . \( -not -path './node_modules/*' \) -path '*.tsx'
PROJ_SRC := ${API_PATH}/UnMango.Wishlists.Api.csproj
API_SRC  := ${CS_SRC} ${PROJ_SRC}
WEB_SRC  := ${TS_SRC} ${TSX_SRC}

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

dev:
	$(DOCKER) compose up --detach db
	$(DOTNET) ef database update --project ${API_PATH}
	$(DOTNET) run --project ${API_PATH}

migration:
	read -p 'Migration name: ' name && \
	$(DOTNET) ef migrations add "$$name" --project ${API_PATH}

precompile-queries: # WIP
	$(DOTNET) ef dbcontext optimize \
	--project ${API_PATH} \
	--output-dir Generated \
	--precompile-queries

bin/schema.json: api
	cp ${API_PATH}/obj/UnMango.Wishlists.Api.json $@
	@touch $@

bin/image.tar: Dockerfile ${API_SRC} ${WEB_SRC}
	mkdir -p ${@D} && $(DOCKER) build ${CURDIR} \
	--output type=tar,dest=$@ \
	--tag ${IMAGE} \
	--load

src/UnMango.Wishlists.Api/bin/Debug/net10.0/UnMango.Wishlists.Api: ${API_SRC}
	$(DOTNET) build

src/web/api/schema.d.ts: bin/schema.json
	$(BUN)x openapi-typescript $< --output $@
	@touch $@

dist/index.html: bun.lock ${WEB_SRC}
	$(BUN) run build

bun.lock: package.json
	$(BUN) install
	@touch $@

.vscode/settings.json: hack/vscode.json
	mkdir -p ${@D} && cp $< $@
