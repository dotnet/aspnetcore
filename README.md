Universe
=========

This repo is to build the whole ASP.NET Core stack.

## Getting started

    git clone git@github.com:aspnet/Universe.git
    cd Universe
    build

The default build will clone all known repos as subfolders. The clone will be the dev branch.

If the build is run subsequently it will `git pull` the dev branch rather than clone. Note! This will cause a 
merge if you have local changes. We may tweak how this is done if it causes problems.

After folders are up to date, `build.cmd compile` is executed in each of the enlisted subfolders.

If there are errors the build will continue with the next repo. 

The last output is a list of which repos succeeded or failed.

## build targets

`build pull` will only clone or pull all repos.

`build compile` this is the default target, described above.

`build install` works like build compile, but will run `build.cmd install` in each subfolder. This means 
any nupkg produced by the repo are copied into the local `.nuget` folder to be picked up by subsequent 
repositories. The subfolders are built in dependency order.



This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.
