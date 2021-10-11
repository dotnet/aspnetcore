// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class GetAttributes_WithoutMethodOverriding
    {
        [ProducesResponseType(201)]
        public void Method() { }
    }
}
