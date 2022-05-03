// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer.Tools;
using Microsoft.Extensions.CommandLineUtils;

<<<<<<< HEAD
=======
#pragma warning disable CA1852 // Seal internal types
>>>>>>> aed8a228a7 (Add dotnet dev-jwts tool)
CommandLineApplication devJwts = new()
{
    Name = "dotnet dev-jwts"
};

devJwts.HelpOption("-h|--help");

// dotnet dev-jwts list
ListCommand.Register(devJwts);
// dotnet dev-jwts create
CreateCommand.Register(devJwts);
// dotnet dev-jwts print ecd045
PrintCommand.Register(devJwts);
// dotnet dev-jwts delete ecd045
DeleteCommand.Register(devJwts);
// dotnet dev-jwts clear
ClearCommand.Register(devJwts);
// dotnet dev-jwts key
KeyCommand.Register(devJwts);

devJwts.Execute(args);
<<<<<<< HEAD
=======
#pragma warning restore CA1852 // Seal internal types
>>>>>>> aed8a228a7 (Add dotnet dev-jwts tool)
