{
  pkgs ? import <nixpkgs> { },
}:
pkgs.mkShellNoCC {
  packages = with pkgs; [
  	tmux
    git
	docker
    gnumake
    nixfmt-rfc-style
    nixfmt-tree
    shellcheck
    watchexec
    dotnetCorePackages.dotnet_10.sdk
    nodejs # For JetBrains tools
    bun
  ];
}
