// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;

namespace Microsoft.AspnetCore.Identity.Service.FunctionalTests
{
    /// <summary>
    /// A handler used to create a loop back into TestServer from the open ID Connect handler.
    /// </summary>
    public class LoopBackHandler : DelegatingHandler
    {
    }
}
