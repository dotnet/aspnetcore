## Music store application

###Getting Started

The first thing we need to do is setup the tools required to build and run an application.

* Clone the repository
* On the command line execute ```kvm setup``` 
* This command will download the latest version of the SDK and put it on your path so that you can run the rest of the commands in the readme. If you want to know more about what this is doing then you can read the [KVM page](https://github.com/aspnet/Preview/wiki/version-manager) of the wiki.
* If you already have ```kvm``` installed on the machine ignore above steps.

### Run the application:
1. Run ```build.cmd``` to restore all the necessary packages and generate project files
2. Open a command prompt and cd ```\src\<AppFolder>\```
3. **[Helios]:**
	4. ```Helios.cmd``` to launch the app on IISExpress (Application started at URL **http://localhost:5001/**).
4. **[SelfHost]:**
	5. Run ```k web``` (Application started at URL **http://localhost:5002/**)
5. **[CustomHost]:**
	6. Run ```k run``` (This hosts the app in a console application - Application started at URL **http://localhost:5003/**)

### Switching between Desktop CLR and CoreCLR:
By default the app runs on desktop CLR. To switch to run the app on CoreCLR set environment variable ```SET TARGET_FRAMEWORK=k10```. To switch back to desktop CLR ```SET TARGET_FRAMEWORK=``` to empty.

### Adding a new package:
1. Edit the project.json to include the package you want to install
2. Do a ```build.cmd``` - This will restore the package and regenerate your csproj files to get intellisense for the installed packages.

### Work against the latest build:
1. Run ```Clean.cmd``` - This will clear all the packages and any temporary files generated
2. Continue the topic "Run the application"

### Work against LKG Build:
1. Everytime you do a ```build.cmd``` you will get the latest packages of all the included packages. If you would like to go back to an LKG build checkout the *LKG.json* file in the MusicStore folder.
2. This is a captured snapshot of build numbers which worked for this application. This LKG will be captured once in a while. 

### Note:
1. Application is started on different ports on different hosts. To change the port or URL modify ```Helios.cmd``` or project.json commands section in case of self-host and customhost. 
2. Use Visual studio only for editing & intellisense. Don't try to build or run the app from Visual studio.