# Trimmer baseline verification

This project is used to verify that ASP.NET Core APIs do not result in additional trimmer warnings other than the ones that are already known. It works by running the trimmer in "library mode", rooting all of it's public APIs, using a set of baselined suppressions and ensuring no new trimmer warnings are produced.

## Enabling trimming on a new project

For more information about how this tool can be used to add trimming support to ASP.NET Core, see [docs/Trimming.md](../../../docs/Trimming.md).
