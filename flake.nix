{
  description = "Wishlists UnstoppableMango style";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
    treefmt-nix.url = "github:numtide/treefmt-nix";
    treefmt-nix.inputs.nixpkgs.follows = "nixpkgs";
    bun2nix.url = "github:baileyluTCD/bun2nix";
    bun2nix.inputs.nixpkgs.follows = "nixpkgs";
  };

  outputs =
    inputs@{ flake-parts, ... }:
    flake-parts.lib.mkFlake { inherit inputs; } {
      systems = [
        "x86_64-linux"
        "aarch64-linux"
        "x86_64-darwin"
        "aarch64-darwin"
      ];
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
					packages.web = build.web;
          packages.api = build.api;
          packages.default = build.api;

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
