#ASP.NET vNext Preview

In the next version of ASP.NET we are working with multiple teams around Microsoft to create a lean, composable .NET stack that provides a familiar and modern framework for web and cloud scenarios.

The Preview repository is a place for the ASP.NET Insiders to log issues and discuss ASP.NET vNext with the product team. It contains some samples and some documentation in the [wiki](https://github.com/aspnet/Preview/wiki).

The samples provided are designed to show some of the features of the new framework as well as setting up a sandbox for you to try out new drops of functionality as they come out. The NuGet.config file in the repo points to a private MyGet feed that has all the packages being developed. The feed is updated every time a full build succeeds.

#Getting Started

* Clone repository
* Optionally add the repo directory to your path, see the note below.
* Open a command prompt and navigate to the repository
* Run GetLatestRuntime.cmd. This will download the SDK you will use to run your applications.
* Navigate to samples\HelloConsole
* Run ```K run```
* You should see some compiler output and a message saying "Hello World"

**The K.cmd file in the root of this repo is designed to be able to be put in your path. It will probe various locations relative to your current directory to find the k tools that it should use. All the steps after this will assume you've done this, so that you can just type ```K Run``` from anywhere. If you choose not to then add the appropriate number of folder traversals (..\\) before each K command.**

#Samples

##Sandbox Samples

These samples, in this repo, are just basic starting points for you to experiment with features. Since there is no File->New Project we thought some simple samples to take the place of scaffolding would be convenient.

+ HelloConsole. This is just basic console app if you want to use it as a starting point. Use it the same as the console app from our earlier samples
+ HelloWeb. This is a minimal startup class that shows welcome page and static file middleware. This is mostly for you to run through the steps in the readme and make sure you have everything setup and working correctly.
+ HelloWebFx. This sample is a basic MVC app. It is not designed to show all the functionality of the new web stack, but to give you a starting point to play with features.

##[Feature Samples](https://github.com/aspnet/Entropy)
The Entropy repo contains samples of specific features in isolation. Each directory contains just enough code to show an aspect of a feature.

##Application Samples
MVC Music Store and [BugTracker](https://github.com/aspnet/BugTracker) application are both being ported. Each of these have their own repository that you can look at as they are working. 

#Running the samples

##Running HelloWeb

1. Clone the repository
2. Change directory to Preview\Samples\HelloWeb
3. Run ```K restore```
4. You should see a bunch of output as all the dependencies of the app are downloaded from MyGet. The K commands all operate on the app that is in the current directory.
5. Run ```K web```
6. You should see build output and a message to show the site is now started
7. Navigate to "http://localhost:5001"
8. You should see the welcome page
9. Navigate to "http://localhost:5001/image.jpg"
10. You should see an image served with the static file middleware

If you can do all of the above then everything should be working. You can try out the WebFx sample now to see some more of the new stack. You should run ```K restore``` before using any sample for the first time.

#Switching to Core CLR

By default when running the applications you are running against Desktop CLR (4.5), you can change that by setting the TARGET_FRAMEWORK variable:

1. Run ```set TARGET_FRAMEWORK=k10```
2. Run ```K web```
3. The first line of your output should say "Loaded Module: klr.core45.dll" instead of "Loaded Module: klr.net45.dll"
4. The HelloWeb app should work the same as when running on Desktop CLR.

**NOTE: There are going to be parts of the stack that work on Desktop but do not work on Core CLR. This set should get smaller and smaller as time goes on, but it is entirely likely as you use Core CLR you will hit errors that can't be worked around as the Core CLR surface area just does not exist yet. An example of this type of problem is using Entity Framework with a database. There are not currently any real database providers that work on Core CLR, so you will be restricted to in-memory EF on Core CLR.**

#Core CLR Packages

Currently the BCL is split into some fairly fine grained packages, which was one of the goals of this effort. However, the packages that exist today do not necessarily represent the list of packages that we will end up with. We are still experimenting with what makes sense to be a package and what the experience should be.

#Feedback

You can log issues in this repo in order to start discussions, ask questions, make suggestions, etc.
