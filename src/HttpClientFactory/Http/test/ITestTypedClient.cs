// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    // Simple typed client for use in tests
    public interface ITestTypedClient
    {
        HttpClient HttpClient { get; }
    }
}