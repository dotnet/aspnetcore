> Tested on: Ubuntu 14.04, Mint 17.01

As with all other operating systems you need DNVM to get going with ASP.NEt 5. To get it you run curl to download a .sh file and then run it. However, getting a new Linux machine configured to be able to actually run an ASP.NET 5 application is more complicated.

To setup our Linux machine we will:

 * Get a working version of Mono
 * Get and compile libuv (Required for the Kestrel server)
 * Get DNVM
 * Add sources to NuGet.config (For package restore)

###Docker

There are instructions on how to use the ASP.NET [Docker](https://www.docker.com/) image here: http://blogs.msdn.com/b/webdev/archive/2015/01/14/running-asp-net-5-applications-in-linux-containers-with-docker.aspx

The rest of this section deals with setting up a machine to run applications without the docker image.

### Get Mono

Mono is how .NET applications can run on platforms other than Windows, it is an ongoing effort to port the .NET framework to other platforms. In the process of developing ASP.NET 5 we worked with the Mono team to fix some bugs and add some features that we needed to run ASP.NET applications. Because these changes haven't yet made it into an official Mono release we will either grab a Mono nightly build or compile Mono from source.

#### Option 1: CI build

The Mono CI server builds packages for Linux distributions on each commit. To get them you install a particular snapshot and then run `mono-snapshot APP/VER` to change the current shell to use the provided snapshot. In these instructions we will grab the latest snapshot and set it to be the one to use.

**NOTE: Mono snapshots do not persist outside the current shell, you need to run `mono-snapshot` each time you want to run the newer version of Mono. If this isn't what you want then look at compiling Mono from source option, the instructions here show building from source and installing Mono. If you want other options then you should follow the links to the Mono build instructions. **

To do this we need to add the Mono CI server to apt-get:

```bash
sudo apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://jenkins.mono-project.com/repo/debian sid main" | sudo tee /etc/apt/sources.list.d/mono-jenkins.list
sudo apt-get update
sudo apt-get install mono-snapshot-latest
. mono-snapshot mono
```
**NOTE:** Official Mono instructions that these steps come from are here: http://www.mono-project.com/docs/getting-started/install/linux/ci-packages/.

#### Option 2: Build from source

Building Mono from source can take some time, and the commands below will install the built version of Mono on your machine replacing any version you might already have.

```bash
sudo apt-get install git autoconf libtool automake build-essential mono-devel gettext
PREFIX='/usr/local'
PATH=$PREFIX/bin:$PATH
git clone https://github.com/mono/mono.git
cd mono
./autogen.sh --prefix=$PREFIX
make
sudo make install
cd .. && rm -rf Mono
mozroots --import --sync
```

See http://www.mono-project.com/docs/compiling-mono/linux/ for more details and some other build options.

**NOTE:** Mono on Linux before 3.12 by default didn’t trust any SSL certificates so you got errors when accessing HTTPS resources. This is not required anymore as 3.12 and later include a new tool that runs on package installation and syncs Mono’s certificate store with the system certificate store (on older versions you have to import Mozilla’s list of trusted certificates by running `mozroots --import --sync`. If you get exceptions during package restore this is the most likely reason.

### Get libuv

[Libuv](https://github.com/libuv/libuv) is a multi-platform asynchronous IO library that is used by the [KestrelHttpServer](https://github.com/aspnet/KestrelHttpServer) that we will use to host our web applications.

To build libuv you should do the following:

```
sudo apt-get install automake libtool
curl -sSL https://github.com/libuv/libuv/archive/v1.4.2.tar.gz | sudo tar zxfv - -C /usr/local/src
cd /usr/local/src/libuv-1.4.2
sudo sh autogen.sh
sudo ./configure
sudo make 
sudo make install
sudo rm -rf /usr/local/src/libuv-1.4.2 && cd ~/
sudo ldconfig
```

**NOTE:** `make install` puts `libuv.so.1` in `/usr/local/lib`, in the above commands `ldconfig` is used to update `ld.so.cache` so that `dlopen` (see man dlopen) can load it. If you are getting libuv some other way or not running `make install` then you need to ensure that dlopen is capable of loading `libuv.so.1`

### Get DNVM

Now lets get DNVM. To do this run:

```
curl -sSL https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.sh | DNX_BRANCH=dev sh && source ~/.dnx/dnvm/dnvm.sh
```

(TODO: Need to change dnvinstall.sh to actually put it in bin. It doesn't at the moment but should.
dnvminstall.sh grabs and copies dnvm.sh into your Home directory (~/.dnx/bin) and sources it. It will also try and find bash or zsh profiles and add a call to source dnvm to them so that dnvm will be available all the time. If you don't like this behaviour or want to do something else then you can edit your profile after running dnvminstall.sh or do all the tasks dnvminstall does changing what you like.

Once this step is complete you should be able to run `dnvm` and see some help text.

# Add Sources to NuGet.config

Now that we have DNVM and the other tools needed to run an ASP.NET application we need to add the development configuration sources to get nightly builds of all the ASP.NET packages.

The nightly package source is: https://www.myget.org/F/aspnetvnext/api/v2/

To add this to your package sources you need to edit the NuGet.config.

Edit: ~/.config/NuGet/NuGet.config

The NuGet.config file should look something like the following:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="AspNetVNext" value="https://www.myget.org/F/aspnetvnext/api/v2/" />
    <add key="nuget.org" value="https://www.nuget.org/api/v2/" />
  </packageSources>
  <disabledPackageSources />
</configuration>
```
The important part of this is that you have a package source with aspnetvnext and nuget.org in it.