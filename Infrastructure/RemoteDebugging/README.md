# Remote Debugging Projects and Tools

This directory contains sample projects and tools/scripts for deploying them to machines and running them. The machines are expected to be pre-configured with the necessary runtime and the VS debugger, but otherwise, they have no dependencies.

**NOTE: Only the HelloWorld app works right now, the ASP.NET Core apps don't work because ASP.NET Core 2.1 doesn't properly support .NET Core 2.1 right now**

## Prerequisites

To debug A Linux device (Windows coming soon) with:

1. .NET Core 2.1 installed
2. SSHD configured
3. VSDbg installed

You also need the following on your local development machine:

1. A working `ssh` and `scp`.
2. A private key that can be used to authenticate with the target serve (passwords are **NOT** supported).
3. `ssh-agent` running and configured with the private key (since prompting for passwords is not supported) or, no passphrase on the key (**not** recommended).

## Deploying the app

All the projects in this repo can be deployed to any host using SSH simply by running the following command:

```
dotnet publish -v:n -p:SshHost=[host]
```

Replace `[host]` with the host name or IP address of your target machine. The following MSBuild properties can be used to configure the publishing operation:

* `SshUser` - The username to connect with. Defaults to `pi`
* `SshPath` and `ScpPath` - If `ssh` and `scp` are not on your PATH, you can use this to provide the path to these executables.
* `DeploymentDestination` - The destination path **on the target machine** to publish the app to. Defaults to `/home/pi/app`
* `VsDbgPath` - The path to the `vsdbg` executable **on the target machine**. Defaults to `/opt/vsdbg/vsdbg`
* `AppDll` - The **name** of the executable to run. Defaults to the main output assembly of the project.

## Debugging the app

After deployment, you should see a message like this in the build output:

```
Debugger launch.json file written. Use the following Visual Studio Command in the Command Window to launch the app with debugging:
> DebugAdapterHost.Launch /LaunchJson:"C:\Users\anurse\Code\aspnet\Infrastructure\RemoteDebugging\src\HelloWorld\obj\Debug\netcoreapp2.1\launch.json"
```

The publish process also generates a `launch.json` file for use in Visual Studio 2017. Note that this file is **not** compatible with Visual Studio Code (even though it has the same name). You can use the provided command to launch the application on the remote machine and attach the debugger.

The following video illustrates how to do this: https://youtu.be/inA3etFBumo

The process is:

1. Open the Visual Studio Command Window: `View` > `Other Windows` > `Command Window`
2. Paste the command shown in the build output (without the leading `> `).
3. Press ENTER

It will take quite some time to attach, I'd expect about 20-30 seconds to wait, since it has to launch the debugger and launch the .NET Core app remotely.

## Resources
* https://github.com/Microsoft/MIEngine/wiki/Offroad-Debugging-of-.NET-Core-on-Linux---OSX-from-Visual-Studio
