// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    // This class contains methods to make easier to read responses in different formats
    // until there is a built-in easier way to do it.
    public static class HttpResponseHelpers
    {
        public static async Task<string> ReadBodyAsStringAsync(this HttpResponse response)
        {
            using (var streamReader = new StreamReader(response.Body))
            {
                return await streamReader.ReadToEndAsync();
            }
        }
    }
}