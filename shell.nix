{
  pkgs ? import <nixpkgs> { },
}:
pkgs.mkShellNoCC {
  packages = with pkgs; [
    git
    gnumake
    nixfmt-rfc-style
    nixfmt-tree
    shellcheck
    watchexec
    dotnetCorePackages.dotnet_10.sdk
    nodejs
  ];
}
