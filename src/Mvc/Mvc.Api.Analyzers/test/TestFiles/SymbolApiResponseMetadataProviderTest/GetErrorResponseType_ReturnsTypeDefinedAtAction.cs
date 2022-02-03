// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest
{
    [ProducesErrorResponseType(typeof(ModelStateDictionary))]
    public class GetErrorResponseType_ReturnsTypeDefinedAtActionController
    {
        [ProducesErrorResponseType(typeof(GetErrorResponseType_ReturnsTypeDefinedAtActionModel))]
        public void Action() { }
    }

    public class GetErrorResponseType_ReturnsTypeDefinedAtActionModel { }
}
