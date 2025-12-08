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
        ./src/apphost
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
          devShells.default = pkgs.mkShellNoCC {
            packages = with pkgs; [
              bun
              docker
              dotnet
              dprint
              git
              gnumake
              nil
              nixfmt-rfc-style
            ];

            BUN = pkgs.bun + "/bin/bun";
            DOCKER = pkgs.docker + "/bin/docker";
            DOTNET = dotnet + "/bin/dotnet";
            DPRINT = pkgs.dprint + "/bin/dprint";
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
