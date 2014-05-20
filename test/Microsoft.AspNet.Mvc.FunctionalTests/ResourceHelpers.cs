// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    // This class contains helper methods for reading resources from a given assembly in order
    // to make tests that require comparing against baseline files embedded as resources less
    // verbose.
    public static class ResourceHelpers
    {
        public static async Task<string> ReadResourceAsStringAsync(this Assembly assembly, string resourceName)
        {
            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var streamReader = new StreamReader(resourceStream))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
        }
    }
}