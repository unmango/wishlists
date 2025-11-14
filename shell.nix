{
  pkgs ? import <nixpkgs> { },
}:
pkgs.mkShell {
  nativeBuildInputs = with pkgs; [
    git
    gnumake
    docker
    nixd
    nixfmt-rfc-style
    nixfmt-tree
    shellcheck
    watchexec
    nodejs # For JetBrains tools
    bun
    (
      with dotnetCorePackages;
      combinePackages [
        sdk_9_0
        sdk_10_0
      ]
    )
  ];
}
