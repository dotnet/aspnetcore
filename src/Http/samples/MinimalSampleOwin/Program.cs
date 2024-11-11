// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Owin;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
hostBuilder.Build().Run();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

Func<IDictionary<string, object>, Task> ConfigureOwinPipeline(Func<IDictionary<string, object>, Task> next) => env =>
{
    return next(env);
};
