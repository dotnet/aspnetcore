// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Microsoft.AspNetCore.Cors
{
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DisableCorsAttribute : Attribute, IDisableCorsAttribute
    {
    }
}