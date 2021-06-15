// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Specifies that the component parameter is required to be provided by the user when authoring it in the editor.
    /// <para>
    /// If a value for this parameter is not provided, editors or build tools may provide warnings indicating the user to
    /// specify a value. This attribute is only valid on properties marked with <see cref="ParameterAttribute"/>.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class EditorRequiredAttribute : Attribute
    {
    }
}
