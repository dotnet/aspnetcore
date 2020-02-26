﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class GetAttributes_WithNewMethodBase
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void VirtualMethod() { }

        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void NotVirtualMethod() { }
    }

    public class GetAttributes_WithNewMethodDerived : GetAttributes_WithNewMethodBase
    {
        [ProducesResponseType(400)]
        public new void VirtualMethod() { }

        [ProducesResponseType(401)]
        public new void NotVirtualMethod() { }
    }
}
