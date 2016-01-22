// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.AspNetCore.Hosting.Fakes
{
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
}