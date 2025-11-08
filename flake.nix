{
  description = "Wishlists UnstoppableMango style";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    {
      self,
      nixpkgs,
      flake-utils,
    }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = nixpkgs.legacyPackages.${system};
      in
      {
        formatter = pkgs.nixfmt-tree;

        packages.default = pkgs.dockerTools.buildImage {
          name = "wishlists";
          tag = "latest";

          copyToRoot = pkgs.buildEnv {
            name = "image-root";
            paths = [
              ./src/UnMango.Wishlists.Api
              ./src/web
            ];
            pathsToLink = [ "/bin" ];
          };
        };

        devShells.default = import ./shell.nix { inherit pkgs; };
      }
    );
}
