## Prerequisites

To run this repo, you need the following:

* Win10/Win2016 or higher
* Install [Policheck](http://aka.ms/policheck) - http://toolbox/policheck
* Install the SSL/PKITA certificates for the ESRP client (see the AspNetCoreCerts KeyVault and https://aka.ms/esrpclient for details).

### Running locally without code signing

`build /t:LocalBuild`

### Running locally with code signing
* Launch a shell running under redmond\fxsign (https://microsoft.sharepoint.com/teams/fxsign/SitePages/FxSign-Account.aspx)
* `build /t:Verify`

