{
  perSystem =
    { pkgs, lib, ... }:
    let
      sdk = pkgs.dotnetCorePackages.sdk_10_0_1xx;
      version = "0.0.1";
      apphost = pkgs.buildDotnetModule {
        inherit version;

        pname = "apphost";
        src = lib.cleanSource ./.;
        projectFile = ./apphost.csproj;
        nugetDeps = ./deps.json;
        dotnet-sdk = sdk;

        meta = with lib; {
          homepage = "https://github.com/unmango/wishlists";
          description = "UnMango Wishlists AppHost";
          license = licenses.mit;
          maintainers = [ maintainers.UnstoppableMango ];
        };
      };
    in
    {
      packages.apphost = apphost;
      legacyPackages.apphost = apphost;
    };
}
