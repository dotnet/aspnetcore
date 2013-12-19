#!/bin/sh

export EnableNuGetPackageRestore=true
mono --runtime=v4.0 ".nuget/NuGet.exe" install Sake -pre -o packages
mono $(find packages/Sake.*/tools/Sake.exe|sort -r|head -n1) -f Sakefile.shade -I src/build "$@"

