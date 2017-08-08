## Prerequisites

* Policheck - http://toolbox/policheck
* CodeSign.Submitter - `\\cp1pd1cdscvlt04\public\Submitter Tool for Download\Submitter 4.1.1.1 (.net v3.5 runtime)\Codesign.Submitter.msi`
* BinScope - http://toolbox/binscope

### Running locally without code signing

`build /t:LocalBuild`

### Running locally with code signing
* Launch a shell running under redmond\fxsign (https://microsoft.sharepoint.com/teams/fxsign/SitePages/FxSign-Account.aspx)
* `build /t:Verify /p:COHERENCE_DROP_LOCATION=<coherence-drop-share-on-ci> /p:COHERENCE_PACKAGECACHE_DROP_LOCATION:<coherence-packagecache-drop-on-ci>`

