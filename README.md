## Prerequisites

* [Policheck](http://aka.ms/policheck) - http://toolbox/policheck
* CodeSign.Submitter - `\\cp1pd1cdscvlt04\public\Submitter Tool for Download\Submitter 4.1.1.1 (.net v3.5 runtime)\Codesign.Submitter.msi`
* .NET 462 SDK
* Set `Enable Win32 long paths` to true in the Local Group Policy Editor, see https://blogs.msdn.microsoft.com/jeremykuhne/2016/07/30/net-4-6-2-and-long-paths-on-windows-10/
* Win10/Win2016 or higher

### Running locally without code signing

`build /t:LocalBuild`

### Running locally with code signing
* Launch a shell running under redmond\fxsign (https://microsoft.sharepoint.com/teams/fxsign/SitePages/FxSign-Account.aspx)
* `build /t:Verify`

