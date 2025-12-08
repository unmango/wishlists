{
  description = "Wishlists UnstoppableMango style";

  nixConfig = {
    extra-substituters = [
      "https://cache.nixos.org"
      "https://nix-community.cachix.org"
    ];
    extra-trusted-public-keys = [
      "cache.nixos.org-1:6NCHdD59X431o0gWypbMrAURkbJ16ZPMQFGspcDShjY="
      "nix-community.cachix.org-1:mB9FSh9qf2dCimDSUo8Zy7bkq5CX+/rkCWyvRCYg3Fs="
    ];
  };

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
        ./src/apphost
        ./default.nix
      ];

      perSystem =
        {
          pkgs,
          system,
          inputs',
          ...
        }:
        let
          dotnet = pkgs.dotnetCorePackages.sdk_10_0_1xx;
          build = pkgs.callPackage ./default.nix {
            inherit pkgs;
            inherit (inputs'.bun2nix.packages) bun2nix;
          };
        in
        {
          _module.args.pkgs = import inputs.nixpkgs {
            inherit system;
            overlays = [
              inputs.bun2nix.overlays.default
              inputs.gomod2nix.overlays.default
            ];
          };

          devShells.default = pkgs.mkShellNoCC {
            packages = with pkgs; [
              bun
              bun2nix
              docker
              dotnet
              dprint
              git
              gnumake
              go
              gomod2nix
              jq
              nil
              nixfmt-rfc-style
              nuget-to-json
            ];

            BUN = pkgs.bun + "/bin/bun";
            BUN2NIX = pkgs.bun2nix + "/bin/bun2nix";
            DOCKER = pkgs.docker + "/bin/docker";
            DOTNET = dotnet + "/bin/dotnet";
            DPRINT = pkgs.dprint + "/bin/dprint";
            GO = pkgs.go + "/bin/go";
            GOMOD2NIX = pkgs.gomod2nix + "/bin/gomod2nix";
            NIXFMT = nixfmt + "/bin/nixfmt";
          };

          treefmt = {
            # Need to fix the ct job first
            programs.actionlint.enable = false;
            programs.nixfmt.enable = true;
          };
        };
    };
}
