// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
