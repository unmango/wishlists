{
  pkgs ? import <nixpkgs> { },
  bun2nix,
  chart-releaser,
}:
pkgs.mkShell {
  nativeBuildInputs = with pkgs; [
    bun
    bun2nix
    chart-testing
    docker
    dotnetCorePackages.sdk_10_0
    gh
    git
    gnumake
    kind
    kubernetes-helm
    nixd
    nixfmt-rfc-style
    nodejs # For JetBrains tools
    shellcheck
    watchexec
  ];

  BUN = pkgs.bun + "/bin/bun";
  BUN2NIX = bun2nix + "/bin/bun2nix";
  # CR = chart-releaser + "/bin/cr";
  CT = pkgs.chart-testing + "/bin/ct";
  DOTNET = pkgs.dotnetCorePackages.sdk_10_0 + "/bin/dotnet";
  HELM = pkgs.kubernetes-helm + "/bin/helm";
  NIXFMT = pkgs.nixfmt-rfc-style + "/bin/nixfmt";
  NODE = pkgs.nodejs + "/bin/node";
}
