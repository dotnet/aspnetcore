// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class RelatedAssemblyAttribute : Attribute
    {
        public RelatedAssemblyAttribute(string name)
        {
        }
    }
}
