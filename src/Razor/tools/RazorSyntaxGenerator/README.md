# Razor syntax generator

Syntax generator tool for the Razor syntax tree. This is a modified version of Roslyn's [CSharpSyntaxGenerator](https://github.com/dotnet/roslyn/tree/master/src/Tools/Source/CompilerGeneratorTools/Source/CSharpSyntaxGenerator). For internal use only.

## Usage

dotnet run `path/to/Syntax.xml` `path/to/generated/output`

E.g,

`dotnet run ../Microsoft.AspNetCore.Razor.Language/Syntax/Syntax.xml ../Microsoft.AspNetCore.Razor.Language/Syntax/Generated/`