{
  description = "Wishlists UnstoppableMango style";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    systems.url = "github:nix-systems/default";
    flake-parts.url = "github:hercules-ci/flake-parts";

    treefmt-nix.url = "github:numtide/treefmt-nix";
    treefmt-nix.inputs.nixpkgs.follows = "nixpkgs";

    bun2nix.url = "github:baileyluTCD/bun2nix?ref=1.5.2";
    bun2nix.inputs.nixpkgs.follows = "nixpkgs";
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
        { pkgs, ... }:
        let
          dotnet = pkgs.dotnetCorePackages.sdk_10_0_1xx;
          nixfmt = pkgs.nixfmt-rfc-style;
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
              nuget-to-json
            ];

            BUN = pkgs.bun + "/bin/bun";
            DOCKER = pkgs.docker + "/bin/docker";
            DOTNET = dotnet + "/bin/dotnet";
            DPRINT = pkgs.dprint + "/bin/dprint";
            NIXFMT = nixfmt + "/bin/nixfmt";
          };

          treefmt = {
            programs.actionlint.enable = true;
            programs.nixfmt.enable = true;
          };
        };
    };
}
