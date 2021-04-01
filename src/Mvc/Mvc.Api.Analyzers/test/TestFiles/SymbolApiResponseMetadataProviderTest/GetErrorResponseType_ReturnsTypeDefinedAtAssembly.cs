// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

[assembly: ProducesErrorResponseType(typeof(Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest.GetErrorResponseType_ReturnsTypeDefinedAtAssemblyModel))]

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest
{
    public class GetErrorResponseType_ReturnsTypeDefinedAtAssemblyController
    {
        public void Action() { }
    }

    public class GetErrorResponseType_ReturnsTypeDefinedAtAssemblyModel { }
}
