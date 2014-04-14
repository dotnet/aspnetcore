## Music store application

### Run the application:
1. Run build.cmd to restore all the necessary packages and generate project files
2. Open a command prompt and cd \src\<AppFolder>\
3. [Helios]:
	4. Helios.cmd to launch the app on IISExpress.
4. [SelfHost]:
	5. Run Selfhost.cmd (This runs k web)
5. [CustomHost]:
	6. Run CustomHost.cmd (This hosts the app in a console application)

### Adding a new package:
1. Edit the project.json to include the package you want to install
2. Do a build.cmd - This will restore the package and regenerate your csproj files to get intellisense for the installed packages.

### Work against the latest build:
1. Run Clean.cmd - This will clear all the packages and any temporary files generated
2. Continue the topic "Steps to run the application"

### Work against LKG Build:
1. Everytime you do a build.cmd you will get the latest packages of all the included packages. If you would like to go back to an LKG build checkout the LKG.json file in the MusicStore folder.
2. This is a captured snapshot of build numbers which worked for this application. This LKG will be captured once in a while. 

### Note:
1. By default the scripts will start the application at http://localhost:5001/. Modify the scripts to change the url. 
2. Use Visual studio only for editing & intellisense. Don't try to build or run the app from Visual studio.