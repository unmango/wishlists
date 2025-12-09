{
  perSystem =
    { pkgs, lib, ... }:
    let
      sdk = pkgs.dotnetCorePackages.sdk_10_0_1xx;

      apphost = pkgs.buildDotnetModule {
        pname = "apphost";
        version = "0.0.1";

        src = lib.cleanSource ./.;
        projectFile = "./apphost.csproj";
        nugetDeps = ./deps.json;
        dotnet-sdk = sdk;

        meta = with lib; {
          homepage = "https://github.com/unmango/wishlists";
          description = "UnMango Wishlists AppHost";
          license = licenses.mit;
          maintainers = [ maintainers.UnstoppableMango ];
          mainProgram = "apphost";
        };
      };
    in
    {
      packages.apphost = apphost;
      legacyPackages.apphost = apphost;
    };
}
