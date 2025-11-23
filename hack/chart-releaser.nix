{ pkgs, lib, ... }:
pkgs.buildGoModule rec {
  pname = "chart-releaser";
  version = "1.8.1";

  src = pkgs.fetchFromGitHub {
    owner = "helm";
    repo = "chart-releaser";
    tag = "v${version}";
    sha256 = "sha256-+Q1r3YkZ8b3p7Y5KX1I6b4Z1Z6Z1Z6Z1Z6Z1Z6Z1Z6Z1Z6=";
  };

  vendorHash = lib.fakeSha256;

  meta = with lib; {
    description = "Hosting Helm Charts via GitHub Pages and Releases";
    homepage = "https://github.com/helm/chart-releaser";
    license = licenses.mit;
    maintainers = [ maintainers.UnstoppableMango ];
  };
}
