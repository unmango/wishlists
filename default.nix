{
  perSystem = { pkgs, lib, ... }: {
    packages.wishlists = pkgs.buildGoApplication {
      pname = "wishlists";
      version = "0.0.1";

      src = lib.cleanSource ./.;
      modules = ./gomod2nix.toml;

      meta = with lib; {
        description = "Wishlists UnstoppableMango style";
        licenses = [ licenses.mit ];
      };
    };
  };
}
