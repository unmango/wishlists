{
  description = "Wishlists UnstoppableMango style";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
    treefmt-nix = {
      url = "github:numtide/treefmt-nix";
      inputs.nixpkgs.follows = "nixpkgs";
    };
  };

  outputs =
    inputs@{
      self,
      nixpkgs,
      flake-parts,
      treefmt-nix,
    }:
    flake-parts.lib.mkFlake { inherit inputs; } {
      systems = [
        "x86_64-linux"
        "aarch64-linux"
        "x86_64-darwin"
        "aarch64-darwin"
      ];
      imports = [
        treefmt-nix.flakeModule
      ];
      perSystem =
        { pkgs, ... }:
        let
          api = pkgs.callPackage ./default.nix { inherit pkgs; };
        in
        {
          packages.api = api;
          packages.default = api;

          treefmt = {
            programs.actionlint.enable = true;
            programs.nixfmt.enable = true;
          };
        };
    };
}
