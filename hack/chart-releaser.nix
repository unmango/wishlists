{ pkgs, lib, ... }:
pkgs.buildGoModule rec {
  pname = "chart-releaser";
  version = "1.8.1";

  src = pkgs.fetchFromGitHub {
    owner = "helm";
    repo = "chart-releaser";
    tag = "v${version}";
    sha256 = "sha256-+X1Y5c1r7b3F2Jt1qz";
  };

  vendorHash = "sha256-";

  meta = with lib;{
    description = "Hosting Helm Charts via GitHub Pages and Releases";
    homepage = "https://github.com/helm/chart-releaser";
    license = licenses.mit;
    maintainers = [ maintainers.UnstoppableMango ];
  };
}
