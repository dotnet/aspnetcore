// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionBase
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void Method() { }
    }

    public class GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionClass : GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionBase
    {
        [ProducesResponseType(400)]
        public override void Method() { }
    }
}
