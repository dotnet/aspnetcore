Build Errors
------------

This document is for common build errors and how to resolve them.

### Warning BUILD001

> warning BUILD001: Package references changed since the last release...

This warning indicates a breaking change might have been made to a package or assembly due to the removal of a reference which was used
in a previous release of this assembly. See <./ReferenceResolution.md> for how to suppress.

### Error BUILD002

> error BUILD002: Package references changed since the last release...

Similar to BUILD001, but this error is not suppressable. This error only appears in servicing builds, which should not change references between assemblies or packages.
