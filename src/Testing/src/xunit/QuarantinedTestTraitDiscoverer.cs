// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

// Do not change this namespace without changing the usage in QuarantinedTestAttribute
namespace Microsoft.AspNetCore.Testing
{
    public class QuarantinedTestTraitDiscoverer : ITraitDiscoverer
    {
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            if (traitAttribute is ReflectionAttributeInfo attribute && attribute.Attribute is QuarantinedTestAttribute quarantinedTestAttribute)
            {
                yield return new KeyValuePair<string, string>("Quarantined", "true");
            }
            else
            {
                throw new InvalidOperationException("The 'QuarantinedTest' attribute is only supported via reflection.");
            }
        }
    }
}
