{ lib, buildDotnetModule, dotnetCorePackages }:

buildDotnetModule rec {
  pname = "BetterFanController";
  version = "0.1";

  src = ./.;

  projectFile = "./BetterFanController.sln";
  nugetDeps = ./deps.nix;
  dotnet-sdk = dotnetCorePackages.sdk_9_0;
  dotnet-runtime = dotnetCorePackages.sdk_9_0;
  dotnetFlags = [ "" ];
  executables = [ "BetterFanController" ];
}
