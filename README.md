## Music store application

### Run the application:
1. Run build.cmd to restore all the necessary packages and generate project files
2. Open a command prompt and cd \src\MusicStore\
3. Execute CopyAspNetLoader.cmd to copy the AspNet.Loader.dll to the bin directory
4. Execute Run.cmd to launch the app on IISExpress. 

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
1. By default this script starts the application at http://localhost:5001/. Modify Run.cmd if you would like to change the url
2. Use Visual studio only for editing & intellisense. Don't try to build or run the app from Visual studio.