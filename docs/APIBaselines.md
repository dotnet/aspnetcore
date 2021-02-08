# API Baselines

This document contains information regarding API baseline files and how to work with them.

## Add baseline files for new projects

When creating a new implementation (i.e. src) project, it's necessary to manually add API baseline files since API baseline are enabled by default. If the project is a non-shipping or test only project, add `<AddPublicApiAnalyzers>false</AddPublicApiAnalyzers>` to the project to disable these checks. To add baseline files to the project:

1. Copy eng\PublicAPI.empty.txt to the project directory and rename it to PublicAPI.Shipped.txt
1. Copy eng\PublicAPI.empty.txt to the project directory and rename it to PublicAPI.Unshipped.txt

## PublicAPI.Shipped.txt

This file contains APIs that were released in the last major version. This file should only be modified after a major release by the build team and should never be modified otherwise.

## PublicAPI.Unshipped.txt

This file contains new APIs since the last major version.

### New APIs

A new entry needs to be added to the PublicAPI.Unshipped.txt file for a new API. For example:

```
#nullable enable
Microsoft.AspNetCore.Builder.NewApplicationBuilder.New() -> Microsoft.AspNetCore.Builder.IApplicationBuilder!
```

### Removed APIs

A new entry needs to be added to the PublicAPI.Unshipped.txt file for a removed API. For example:

```
#nullable enable
*REMOVED*Microsoft.Builder.OldApplicationBuilder.New() -> Microsoft.AspNetCore.Builder.IApplicationBuilder!
```

### Updated APIs

Two new entry needs to be added to the PublicAPI.Unshipped.txt file for an updated API, one to remove the old API and one for the new API. This also applies to APIs that are now nullable aware. For example:

```
#nullable enable
*REMOVED*Microsoft.AspNetCore.DataProtection.Infrastructure.IApplicationDiscriminator.Discriminator.get -> string!
Microsoft.AspNetCore.DataProtection.Infrastructure.IApplicationDiscriminator.Discriminator.get -> string?
```

## Updating baselines after major releases

TODO