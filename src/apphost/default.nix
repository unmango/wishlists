{
  perSystem =
    { pkgs, lib, ... }:
    let
      sdk = pkgs.dotnetCorePackages.sdk_10_0_1xx;
      version = "0.0.1";

      apphost = pkgs.buildDotnetModule {
        pname = "apphost";
        inherit version;

        src = ./.;
        projectFile = ./apphost.csproj;
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
      apps.apphost = {
        type = "app";
        program = apphost;
      };

      packages.apphost = apphost;
      legacyPackages.apphost = apphost;
    };
}
