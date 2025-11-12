{
  pkgs ? import <nixpkgs> { },
  mkBunDerivation,
}:
let
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
  };
  api = pkgs.buildDotnetModule {
    pname = "api";
    version = "0.0.1";
    src = pkgs.lib.cleanSource ./.;

    dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
    projectFile = "src/UnMango.Wishlists.Api/UnMango.Wishlists.Api.csproj";
    nugetDeps = ./src/UnMango.Wishlists.Api/nix-deps.json;

    meta = with pkgs.lib; {
      description = "Wishlists API";
      license = licenses.mit;
      maintainers = [
        {
          name = "Erik Rasmussen";
          email = "erik.rasmussen@unmango.dev";
        }
      ];
    };
  };
in
{
  inherit web api;
}
