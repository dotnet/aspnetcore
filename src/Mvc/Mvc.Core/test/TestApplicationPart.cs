// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc
{
    public class TestApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public TestApplicationPart()
        {
            Types = Enumerable.Empty<TypeInfo>();
        }

        public TestApplicationPart(params TypeInfo[] types)
        {
            Types = types;
        }

        public TestApplicationPart(IEnumerable<TypeInfo> types)
        {
            Types = types;
        }

        public TestApplicationPart(IEnumerable<Type> types)
            :this(types.Select(t => t.GetTypeInfo()))
        {
        }

        public TestApplicationPart(params Type[] types)
            : this(types.Select(t => t.GetTypeInfo()))
        {
        }

        public override string Name => "Test part";

        public IEnumerable<TypeInfo> Types { get; }
    }
}
