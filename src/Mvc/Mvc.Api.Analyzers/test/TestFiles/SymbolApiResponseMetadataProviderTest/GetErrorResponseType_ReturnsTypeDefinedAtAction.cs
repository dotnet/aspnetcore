// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
