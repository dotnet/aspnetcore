// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Provides a <see cref="ApplicationPartFactory"/> type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ProvideApplicationPartFactoryAttribute : Attribute
    {
        public ProvideApplicationPartFactoryAttribute(Type factoryType)
        {
            ApplicationPartFactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
        }

        public Type ApplicationPartFactoryType { get; }
    }
}
