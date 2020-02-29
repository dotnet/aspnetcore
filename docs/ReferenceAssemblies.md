Reference assemblies
========================

Most projects in this repo have a `ref` directory next to their `src` that contains the project and source code for a reference assembly.
Reference assemblies contain the public API surface of libraries and are used for ASP.NET Core targeting pack generation.

### When changing public API

Run `dotnet msbuild /t:GenerateReferenceSource` in that project's `src` directory

### When adding a new project

Run `.\eng\scripts\GenerateProjectList.ps1` from the repository root and `dotnet msbuild /t:GenerateReferenceSource` in that project's `src` directory

### To set project properties in a reference assembly project

`ref.csproj` is automaticaly generated and shouldn't be edited. To set project properties on a reference assembly project place a `Directory.Build.props` next to it and add the properties there.

### My project doesn't need a reference assembly

Set `<HasReferenceAssembly>false</HasReferenceAssembly>` in the implementation (`src`) project and re-run `.\eng\scripts\GenerateProjectList.ps1`.

### Regenerate reference assemblies for all projects

Run `.\eng\scripts\GenerateReferenceAssemblies.ps1` from repository root. Make sure you've run `.\restore.cmd` first.
