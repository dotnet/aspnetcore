// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer.Tools;
using Microsoft.Extensions.CommandLineUtils;

#pragma warning disable CA1852 // Seal internal types
ProjectCommandLineApplication userJwts = new()
{
    Name = "dotnet user-jwts"
};

userJwts.HelpOption("-h|--help");

// dotnet user-jwts list
ListCommand.Register(userJwts);
// dotnet user-jwts create
CreateCommand.Register(userJwts);
// dotnet user-jwts print ecd045
PrintCommand.Register(userJwts);
// dotnet user-jwts delete ecd045
DeleteCommand.Register(userJwts);
// dotnet user-jwts clear
ClearCommand.Register(userJwts);
// dotnet user-jwts key
KeyCommand.Register(userJwts);

// Show help information if no subcommand/option was specified.
userJwts.OnExecute(() => userJwts.ShowHelp());

userJwts.Execute(args);
#pragma warning restore CA1852 // Seal internal types