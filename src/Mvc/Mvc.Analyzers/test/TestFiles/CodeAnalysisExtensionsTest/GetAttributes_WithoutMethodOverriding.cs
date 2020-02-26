// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class GetAttributes_WithoutMethodOverriding
    {
        [ProducesResponseType(201)]
        public void Method() { }
    }
}
