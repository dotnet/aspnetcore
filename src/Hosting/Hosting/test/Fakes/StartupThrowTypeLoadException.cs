// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Hosting.Fakes;

public class StartupThrowTypeLoadException
{
    public StartupThrowTypeLoadException()
    {
        // For this exception, the error page should contain details of the LoaderExceptions
        throw new ReflectionTypeLoadException(
            classes: new Type[] { GetType() },
            exceptions: new Exception[] { new FileNotFoundException("Message from the LoaderException") },
            message: "This should not be in the output");
    }

    public void Configure()
    {
    }
}
