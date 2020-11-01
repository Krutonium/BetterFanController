# A Better Fan Controller

## Project Goals:
- Control Multiple GPUs Fans ✔️
- Make it configurable ✔️
- Support Other Vendors ❌
- More Crash Resistant ❌
- Average GPU Temps for smoother changes ️✔️
- Be able to identify the GPU's in question by name ✔️
- Create Systemd service ✔️
- Refactor for Cleanlyness ❌
- Enable configuring Maximum Power Draw ✔️
- Dogfood! ✔️
- Be a Hacktoberfest Project! ✔️

✔️ = Complete
❌ = Incomplete

Want to chat? Join the [Discord](https://discord.gg/5g5cH2a)! 

## Installing as a Systemd Service:

Follow these steps to install and run the app as a Systemd service on Ubuntu 20.04 (these may vary for your distro):

- Publish the application as single file app by running
```shell
dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained false
```

- Copy the published app to `/usr/sbin`
```shell
sudo cp bin/Debug/netcoreapp3.1/linux-x64/publish/BetterFanController
 /usr/sbin/
```

- Copy the `betterfancontroller.service` file from the repo to `/etc/systemd/system`
```shell
 sudo cp betterfancontroller.service /etc/systemd/system
```

- Reload systemd with
```shell
sudo systemctl daemon-reload
```

- Start the service with
```shell
sudo systemctl start betterfancontroller
```

- View service status with
```shell
sudo systemctl status betterfancontroller
```

- Stop the service with
```shell
sudo systemctl stop betterfancontroller
```

- View service logs with
```shell
sudo journalctl -u betterfancontroller
```