Project Properties
==================

In addition to the standard set of MSBuild properties supported by Microsoft.NET.Sdk, projects in this repo often use these additional properties.

Property name      | Meaning
-------------------|--------------------------------------------------------------------------------------------
IsProductPackage   | When set to `true`, the package produced by from project is intended for use by customers. Defaults to  `false`, which means the package is intended for internal use only by Microsoft teams.
