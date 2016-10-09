# MusicStore application

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/ja8a7j6jscj7k3xa/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/MusicStore/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/MusicStore.svg?branch=dev)](https://travis-ci.org/aspnet/MusicStore)

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.

## Run the application on Helios:
* If you have Visual Studio 2015
	1. Open MusicStore.sln in Visual Studio 2015 and run the individual applications on `IIS Express`.
* If you don't have Visual Studio 2015
	1. Open a command prompt and execute `cd \src\MusicStore\`.
	2. Execute `dnu restore`.
	3. Execute `Helios.cmd` to launch the app on IISExpress from command line (Application started at URL **http://localhost:5001/**).
	   
**NOTE:** App and tests require Visual Studio 2015 LocalDB on the machine to run.

## Run on WebListener/Kestrel:
* Open a command prompt and cd `\src\MusicStore\`.
* **[WebListener]:**
	4. Run `dnx . web` (Application started at URL **http://localhost:5002/**).
* **[Kestrel]:**
	5. Run `dnx . kestrel` (Application started at URL **http://localhost:5004/**).
* **[CustomHost]:**
	6. Run `dnx . run` (This hosts the app in a console application - Application started at URL **http://localhost:5003/**).

## Run on Docker Windows Containers

 * [Install Docker for Windows](https://docs.docker.com/docker-for-windows/) or [setup up Docker Windows containers](https://msdn.microsoft.com/en-us/virtualization/windowscontainers/containers_welcome)
 * `docker-compose -f .\docker-compose.windows.yml build`
 * `docker-compose -f .\docker-compose.windows.yml up`
 * Access MusicStore on either the Windows VM IP or (if container is running locally) on the container IP: `docker inspect -f "{{ .NetworkSettings.Networks.nat.IPAddress }}" musicstore_web_1`

## To run the sample on Mac/Mono:
* Follow the instructions at the [Home](https://github.com/aspnet/Home) repository to install Mono and DNVM on Mac OS X.
* Open a command prompt and execute `cd samples/MusicStore.Standalone`.
* Execute `dotnet restore`.
* Try `dotnet run` to run the application.

**NOTE:** Since on Mono SQL client is not available the sample uses an InMemoryStore to run the application. So the changes that you make will not be persisted.

###NTLM authentication
More information at [src/MusicStore/StartupNtlmAuthentication.cs](src/MusicStore/StartupNtlmAuthentication.cs).

###OpenIdConnect authentication
More information at [src/MusicStore/StartupOpenIdConnect.cs](src/MusicStore/StartupOpenIdConnect.cs).
