// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.IISIntegration.FunctionalTests;

public static class TestStartup
{
    public static void Register(IApplicationBuilder app, object startup)
    {
        var delegates = new Dictionary<string, RequestDelegate>();

        var type = startup.GetType();
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var parameters = method.GetParameters();
            if (method.Name != "Configure" &&
                parameters.Length == 1)
            {
                RequestDelegate appfunc = null;

                if (parameters[0].ParameterType == typeof(IApplicationBuilder))
                {
                    var innerAppBuilder = app.New();
                    method.Invoke(startup, new[] { innerAppBuilder });
                    appfunc = innerAppBuilder.Build();
                }
                else if (parameters[0].ParameterType == typeof(HttpContext))
                {
                    appfunc = context => (Task)method.Invoke(startup, new[] { context });
                }

                if (appfunc == null)
                {
                    continue;
                }

                delegates.Add("/" + method.Name, appfunc);
            }
        }

        app.Run(async context =>
        {
            foreach (var requestDelegate in delegates)
            {
                if (context.Request.Path.StartsWithSegments(requestDelegate.Key, out var matchedPath, out var remainingPath))
                {
                    var pathBase = context.Request.PathBase;
                    context.Request.PathBase = pathBase.Add(matchedPath);
                    context.Request.Path = remainingPath;
                    await requestDelegate.Value(context);
                }
            }
        });
    }
}
