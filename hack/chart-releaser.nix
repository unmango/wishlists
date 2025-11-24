{
  buildGoModule,
  fetchFromGitHub,
  lib,
}:
buildGoModule rec {
  pname = "chart-releaser";
  version = "1.8.1";

  src = fetchFromGitHub {
    owner = "helm";
    repo = "chart-releaser";
    tag = "v${version}";
    sha256 = "sha256-+Q1r3YkZ8b3p7Y5KX1I6b4Z1Z6Z1Z6Z1Z6Z1Z6Z1Z6Z1Z6=";
  };

  vendorHash = "sha256-29rGyStJsnhJiO01DIFf/ROaYsXGg3YRJatdzC6A7JU=";

  meta = with lib; {
    description = "Hosting Helm Charts via GitHub Pages and Releases";
    homepage = "https://github.com/helm/chart-releaser";
    license = licenses.mit;
    maintainers = [ maintainers.UnstoppableMango ];
    mainProgram = "cr";
  };
}
