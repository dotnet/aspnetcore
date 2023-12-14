// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

internal sealed class ProcessHelpers
{
    public static void AddEnvironmentVariablesToProcess(ProcessStartInfo startInfo, IDictionary<string, string> environmentVariables, ILogger logger)
    {
        var environment = startInfo.Environment;

        foreach (var environmentVariable in environmentVariables)
        {
            SetEnvironmentVariable(environment, environmentVariable.Key, environmentVariable.Value, logger);
        }
    }

    public static void SetEnvironmentVariable(IDictionary<string, string> environment, string name, string value, ILogger logger)
    {
        if (value == null)
        {
            logger.LogInformation("Removing environment variable {name}", name);
            environment.Remove(name);
        }
        else
        {
            logger.LogInformation("SET {name}={value}", name, value);
            environment[name] = value;
        }
    }
}
