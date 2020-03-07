// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Reflection
{
    internal interface IPropertySetter
    {
        bool Cascading { get; }

        void SetValue(object target, object value);
    }
}
