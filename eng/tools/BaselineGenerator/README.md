BaselineGenerator
=================

This tool is used to generate an MSBuild file which sets the "baseline" against which servicing updates are built.

## Usage

1. Add to the [Baseline.xml](/eng/Baseline.xml) a list of package ID's and their latest released versions. The source of this information can typically
  be found in the build.xml file generated during ProdCon builds. See https://github.com/dotnet/versions/blob/master/build-info/dotnet/product/cli/release/2.1.6/build.xml for example.
2. Run `dotnet run` on this project.
