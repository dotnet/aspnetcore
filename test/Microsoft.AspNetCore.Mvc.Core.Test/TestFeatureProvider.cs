// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Microsoft.AspNetCore.Mvc
{
    public class TestFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly Func<TypeInfo, bool> _filter;

        public TestFeatureProvider()
            : this(t => true)
        {
        }

        public TestFeatureProvider(Func<TypeInfo, bool> filter)
        {
            _filter = filter;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(t => t.Types).Where(_filter))
            {
                feature.Controllers.Add(type);
            }
        }
    }
}
