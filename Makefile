IMAGE ?= wishlists:dev

BUN    ?= bun
DOTNET ?= dotnet

CS_SRC   != find . -path '*.cs'
TS_SRC   != find . \( -not -path './node_modules/*' \) -path '*.ts'
TSX_SRC  != find . \( -not -path './node_modules/*' \) -path '*.tsx'
PROJ_SRC != find . -path '*.*proj'
API_SRC  := ${CS_SRC} ${PROJ_SRC}
WEB_SRC  := ${TS_SRC} ${TSX_SRC}

build: api web

api: src/UnMango.Wishlists.Api/bin/Debug/net10.0/UnMango.Wishlists.Api
web: dist/index.html

lint: eslint

docker:
	docker build . -t ${IMAGE}

compose:
	docker compose build

eslint:
	$(BUN) run lint

dotnet-analyzers:
	$(DOTNET) format analyzers

src/UnMango.Wishlists.Api/bin/Debug/net10.0/UnMango.Wishlists.Api: ${API_SRC}
	$(DOTNET) build

dist/index.html: bun.lock ${WEB_SRC}
	$(BUN) run build

bun.lock: package.json
	$(BUN) install
	@touch $@
