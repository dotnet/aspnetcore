dnx-watch
===
`dnx-watch` is a file watcher for `dnx` that restarts the specified application when changes in the source code are detected. Its' purpose is to replace `dnx --watch`.

### How To Install
From a console window run the following command `dnu commands install Microsoft.Dnx.Watcher` 
Then the `dnx-watch` command will become available.

To install the latest unstable release add the following parameter to the command `--fallbacksource https://myget.org/F/aspnetvnext/api/v3/index.json`

### How To Use
`dnx-watch <arguments>`

Example:

* To run the command `kestrel` in the current folder: `dnx-watch kestrel`
* To run the command kestrel in a different folder: `dnx-watch --project C:\myproject --dnx-args kestrel`
* To run the command kestrel in a different folder with extra arguments: `dnx-watch --project C:\myproject --dnx-args kestrel arg1 arg2`

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/fxhto3omtehio3aj/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/dnx-watch/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/dnx-watch.svg?branch=dev)](https://travis-ci.org/aspnet/dnx-watch)


### Remarks:

* Everything after `--dnx-args` is passed to dnx and ignored by the watcher.
* The watcher always passes `--project` to dnx. Do not pass it as a `--dnx-args` argument.

This project is part of ASP.NET 5. You can find samples, documentation and getting started instructions for ASP.NET 5 at the [Home](https://github.com/aspnet/home) repo.
