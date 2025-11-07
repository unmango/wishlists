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

        devShell = pkgs.mkShell {
          buildInputs = with pkgs; [
            git
            gnumake
            docker
            nixfmt-rfc-style
            nixfmt-tree
            shellcheck
            watchexec
            dotnetCorePackages.sdk_10_0
            nodejs # For JetBrains tools
            bun
          ];
        };
      }
    );
}
