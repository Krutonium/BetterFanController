{pkg, config, ...}:
{
  sytsem.services.betterfancontroller = {
    description = "A fan controller for AMD GPUs";
    serviceConfig = {
      type = "simple";
      WorkingDirectory = "/tmp/"
      user = "root";
      Restart = "always";
    };
    wantedBy = [ "multi-user.target" ];
    path = [ pkgs.dotnet-sdk_5 ];
    script = ''
      BetterFanController
    '';
    enable = true;
  };
}
