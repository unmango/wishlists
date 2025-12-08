{
  perSystem =
    { pkgs, lib, ... }:
    let
      bun2nix = pkgs.bun2nix;

      wishlists = pkgs.buildGoApplication {
        pname = "wishlists";
        version = "0.0.1";

        nativeBuildInputs = [
          bun2nix.hook
        ];

        bunDeps = bun2nix.fetchBunDeps {
          bunNix = ./bun.nix;
        };

        preBuild = ''
          bun run build --minify
        '';

        postInstall = ''
          mkdir -p $out/wwwroot
          cp -r ./dist/* $out/wwwroot
        '';

        src = lib.cleanSource ./.;
        modules = ./gomod2nix.toml;

        meta = with lib; {
          description = "Wishlists UnstoppableMango style";
          licenses = [ licenses.mit ];
        };
      };
    in
    {
      apps.wishlists = {
        type = "app";
        package = wishlists;
      };

      packages.wishlists = wishlists;
    };
}
