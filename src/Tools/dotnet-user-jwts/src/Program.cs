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
    Name = "dotnet user-jwts"
};

devJwts.HelpOption("-h|--help");

// dotnet user-jwts list
ListCommand.Register(devJwts);
// dotnet user-jwts create
CreateCommand.Register(devJwts);
// dotnet user-jwts print ecd045
PrintCommand.Register(devJwts);
// dotnet user-jwts delete ecd045
DeleteCommand.Register(devJwts);
// dotnet user-jwts clear
ClearCommand.Register(devJwts);
// dotnet user-jwts key
KeyCommand.Register(devJwts);

devJwts.Execute(args);
<<<<<<< HEAD
=======
#pragma warning restore CA1852 // Seal internal types
>>>>>>> aed8a228a7 (Add dotnet dev-jwts tool)
