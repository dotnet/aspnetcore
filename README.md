NodeServices
========

This repo hosts sources for the `Microsoft.AspNet.AngularServices` and `Microsoft.AspNet.ReactServices` packages, along with samples and the underlying `Microsoft.AspNet.NodeServices project`.

This project is part of ASP.NET 5. You can find samples, documentation and getting started instructions for ASP.NET 5 at the [Home](https://github.com/aspnet/home) repo.

## Trying the samples

To get started,

1. Ensure you have [installed the latest stable version of ASP.NET 5](https://www.asp.net/vnext). Instructions are available for [Windows](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html), [Mac](http://docs.asp.net/en/latest/getting-started/installing-on-mac.html), and [Linux](http://docs.asp.net/en/latest/getting-started/installing-on-linux.html).
2. Ensure you have [installed a recent version of Node.js](https://nodejs.org/en/). To check this works, open a console prompt, and type `node -v`. It should print a version number.
3. Ensure you have installed `gulp` globally. You can check if it's there by running `gulp -v`. If you need to install it:

   ```
   npm install -g gulp
   ```

3. Clone this repository:

   ```
   git clone https://github.com/aspnet/NodeServices.git
   ```

**Using Visual Studio on Windows**

1. Open the solution file, `NodeServices.sln`, in Visual Studio. Wait for it to finish fetching and installing dependencies.

**Using dnx on Windows/Mac/Linux**

1. Ensure you are using a suitable .NET runtime. Currently, this project is tested with version `1.0.0-beta8` on `coreclr`:

   ```
   dnvm use 1.0.0-beta8 -r coreclr
   ```

2. In the solution root directory (`NodeServices` - i.e., the directory that contains `NodeServices.sln`), restore the .NET dependencies:


   ```
   cd NodeServices
   dnu restore
   ```

3. Change directory to whichever sample you want to run, then restore the Node dependencies. For example:

   ```
   cd samples/angular/MusicStore/
   npm install
   ```

4. Build the project

  ```
  gulp
  ```

5. Run the project (and wait until it displays the message `Application started`)

  ```
  dnx web
  ```

6. Browse to [`http://localhost:5000/`](http://localhost:5000/)

