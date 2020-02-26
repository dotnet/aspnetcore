// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    // Simple typed client for use in tests
    public interface ITestTypedClient
    {
        HttpClient HttpClient { get; }
    }
}