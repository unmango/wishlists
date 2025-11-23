{
  pkgs ? import <nixpkgs> { },
  mkBunDerivation,
}:
let
  sharedMeta = with pkgs.lib; {
    license = licenses.mit;
    maintainers = [
      {
        name = "Erik Rasmussen";
        email = "erik.rasmussen@unmango.dev";
      }
    ];
  };
  web = mkBunDerivation {
    packageJson = ./package.json;
    src = pkgs.lib.cleanSource ./.;
    bunNix = ./src/web/bun.nix;

    buildPhase = ''
      bun run build
    '';

    installPhase = ''
      mkdir -p $out/wwwroot
      cp -r dist/* $out/wwwroot
    '';

    meta = {
      description = "Wishlists Web Application";
      inherit (sharedMeta) license maintainers;
    };
  };
  api = pkgs.buildDotnetModule {
    pname = "api";
    version = "0.0.1";
    src = pkgs.lib.cleanSource ./.;

    dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
    dotnet-runtime = pkgs.dotnetCorePackages.aspnetcore_10_0;
    projectFile = "src/UnMango.Wishlists.Api/UnMango.Wishlists.Api.csproj";
    nugetDeps = ./src/UnMango.Wishlists.Api/nix-deps.json;

    meta = {
      description = "Wishlists API";
      inherit (sharedMeta) license maintainers;
    };
  };
  app = pkgs.buildEnv {
    name = "wishlists";
    paths = [
      web
      api
    ];
  };
  docker = pkgs.dockerTools.buildImage {
    name = "wishlists-docker";
    tag = "latest";
    copyToRoot = app;

    config = {
      Cmd = [ "/bin/UnMango.Wishlists.Api" ];
    };

    meta = {
      description = "Docker image for the Wishlists application";
      inherit (sharedMeta) license maintainers;
    };
  };
in
{
  inherit
    api
    app
    docker
    web
    ;
}
