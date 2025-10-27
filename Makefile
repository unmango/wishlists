build:
	dotnet build

docker:
	docker build . -t wishlists:dev

compose:
	docker compose build
