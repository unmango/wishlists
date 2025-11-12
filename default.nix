{
  pkgs ? import <nixpkgs> { },
}:
let
  api = pkgs.buildDotnetModule {
    pname = "api";
    version = "0.0.1";
    src = pkgs.lib.cleanSource ./.;

    dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
    projectFile = "src/UnMango.Wishlists.Api/UnMango.Wishlists.Api.csproj";
    nugetDeps = ./src/UnMango.Wishlists.Api/deps.json;

    meta = with pkgs.lib; {
      description = "Wishlists API";
      license = licenses.mit;
      maintainers = with maintainers; [
        {
          name = "Erik Rasmussen";
          email = "erik.rasmussen@unmango.dev";
        }
      ];
    };
  };
in
api
