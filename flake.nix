{
  description = "Wishlists UnstoppableMango style";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    systems.url = "github:nix-systems/default";
    flake-parts.url = "github:hercules-ci/flake-parts";

    treefmt-nix.url = "github:numtide/treefmt-nix";
    treefmt-nix.inputs.nixpkgs.follows = "nixpkgs";

    bun2nix.url = "github:nix-community/bun2nix";
    bun2nix.inputs.nixpkgs.follows = "nixpkgs";

    gomod2nix.url = "github:nix-community/gomod2nix";
    gomod2nix.inputs.nixpkgs.follows = "nixpkgs";
  };

  outputs =
    inputs@{ flake-parts, ... }:
    flake-parts.lib.mkFlake { inherit inputs; } {
      systems = import inputs.systems;

      imports = [
        inputs.treefmt-nix.flakeModule
      ];
      perSystem =
        { pkgs, system, ... }:
        let
          build = pkgs.callPackage ./default.nix {
            inherit pkgs;
            inherit (inputs.bun2nix.lib.${system}) mkBunDerivation;
          };
        in
        {
          # TODO: Clean up
          packages.web = build.web;
          packages.api = build.api;
          packages.wishlists = build.app;
          packages.docker = build.docker;
          packages.default = build.docker;

          apps.api = {
            type = "app";
            program = "${build.api}/bin/UnMango.Wishlists.Api";
            meta.description = "Wishlists API";
          };
          apps.wishlists = {
            type = "app";
            program = "${build.app}/bin/UnMango.Wishlists.Api";
            meta.description = "Wishlists Application";
          };

          devShells.default = pkgs.callPackage ./shell.nix {
            inherit pkgs;
            bun2nix = inputs.bun2nix.packages.${system}.default;
          };

          treefmt = {
            programs.actionlint.enable = true;
            programs.nixfmt.enable = true;
          };
        };
    };
}
