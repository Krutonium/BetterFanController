{ lib, buildDotnetModule, dotnetCorePackages }:

buildDotnetModule rec {
  pname = "BetterFanController";
  version = "0.1";

  src = "./.";

  projectFile = "./BetterFanController.sln";
  nugetDeps = ./deps.nix;
  dotnet-sdk = dotnetCorePackages.sdk_6_0;
  dotnet-runtime = dotnetCorePackages.sdk_6_0;
  dotnetFlags = [ "--runtime linux-x64" ];
  executables = [ "BetterFanController" ];
};
