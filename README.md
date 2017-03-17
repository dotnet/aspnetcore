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


## Verifying cross repo changes
You can use the Universe repo to preemptively verify and prepare follow ups for your breaking changes:
- Clone the Universe repo https://github.com/aspnet/Universe 
- Add a branch attribute to the build\Repositories.props file to point to your branch in the repo you’re trying to verify. For instance,
  `<Repository Include="HtmlAbstractions" Commit="" />`
  becomes
  `<Repository Include="HtmlAbstractions" Commit="" Branch=”prkrishn/breaking-changes” />`
- Run from the root of Universe, 
  `build.cmd /p:CompileOnly=true /p:ShallowClone=true /p:BuildGraphOf=HtmlAbstractions`
  This should clone and compile all the repos against your breaking changes branch. If you’d like to additionally run tests in all your dependencies (this will take a while), remove the first parameter: 
  `build.cmd /p:ShallowClone=true /p:BuildGraphOf=HtmlAbstractions`
  The ShallowClone property speeds up git clone and is optional in both cases.


This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.
