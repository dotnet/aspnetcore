// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [ApiConventionType(typeof(object))]
    [ApiController]
    [ApiConventionType(typeof(string))]
    public class GetAttributes_BaseTypeWithAttributesBase
    {
    }

    [ApiConventionType(typeof(int))]
    public class GetAttributes_BaseTypeWithAttributesDerived : GetAttributes_BaseTypeWithAttributesBase
    {
    }
}
