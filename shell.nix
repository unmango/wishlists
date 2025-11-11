{
  pkgs ? import <nixpkgs> { },
}:
pkgs.mkShell {
  nativeBuildInputs = with pkgs; [
    git
    gnumake
    docker
    nixfmt-rfc-style
    shellcheck
    watchexec
    dotnetCorePackages.sdk_10_0
    nodejs # For JetBrains tools
    bun
  ];
}
