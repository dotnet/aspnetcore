// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ApiConventionAttribute : Attribute, IFilterMetadata
    {
        public ApiConventionAttribute(Type conventionType)
        {
            ConventionType = conventionType ?? throw new ArgumentNullException(nameof(conventionType));

            if (!ConventionType.IsSealed || !ConventionType.IsAbstract)
            {
                // Conventions must be static viz abstract + sealed.
                throw new ArgumentException(Resources.FormatApiConventionMustBeStatic(conventionType), nameof(conventionType));
            }
        }

        public Type ConventionType { get; }
    }
}
