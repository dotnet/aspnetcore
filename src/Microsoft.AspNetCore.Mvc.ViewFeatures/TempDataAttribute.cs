// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Properties decorated with <see cref="TempDataAttribute"/> will have their values stored in
    /// and loaded from the <see cref="ViewFeatures.TempDataDictionary"/>. <see cref="TempDataAttribute"/>
    /// is supported on properties of Controllers, Razor Pages, and Razor Page Models.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class TempDataAttribute : Attribute
    {
    }
}
