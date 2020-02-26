// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest
{
    [ProducesErrorResponseType(typeof(GetErrorResponseType_ReturnsTypeDefinedAtControllerModel))]
    public class GetErrorResponseType_ReturnsTypeDefinedAtControllerController
    {
        public void Action() { }
    }

    public class GetErrorResponseType_ReturnsTypeDefinedAtControllerModel { }
}
