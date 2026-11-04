# Autobahn Testing

This application is used to provide the server for the [Autobahn Test Suite](http://autobahn.ws/testsuite) 'fuzzingclient' mode to test. It is a simple echo server that echos each frame received back to the client.

In order to run these tests you must install CPython 2.7, Pip, and the test suite modules. You must also have
the `wstest` executable provided by the Autobahn Suite on the `PATH`. See http://autobahn.ws/testsuite/installation.html#installation for more info

Once Autobahn is installed, launch this application in the desired configuration (in IIS Express, or using Kestrel directly) from Visual Studio and get the WebSocket URL from the HTTP response. Use that URL in place of `ws://server:1234` and invoke the `scripts\RunAutobahnTests.ps1` script in this project like so:

```
> .\scripts\RunAutobahnTests.ps1 -ServerUrl ws://server:1234
```

By default, all cases are run and the report is written to the `autobahnreports` sub-directory of the directory in which you run the script. You can change either by using the `-Cases` and `-OutputDir` switches, use `.\script\RunAutobahnTests.ps1 -?` for help.
