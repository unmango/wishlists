{
  pkgs ? import <nixpkgs> { },
  bun2nix,
}:
pkgs.mkShell {
  nativeBuildInputs = with pkgs; [
    gh
    git
    gnumake
    docker
    nixfmt-rfc-style
    shellcheck
    watchexec
    dotnetCorePackages.sdk_10_0
    nodejs # For JetBrains tools
    bun
    bun2nix
    nixd
  ];

  BUN = pkgs.bun + "/bin/bun";
  BUN2NIX = bun2nix + "/bin/bun2nix";
  DOTNET = pkgs.dotnetCorePackages.sdk_10_0 + "/bin/dotnet";
  NIXFMT = pkgs.nixfmt-rfc-style + "/bin/nixfmt";
  NODE = pkgs.nodejs + "/bin/node";
}
