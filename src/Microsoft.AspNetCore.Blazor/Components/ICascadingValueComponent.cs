// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Rendering;
using System;

namespace Microsoft.AspNetCore.Blazor.Components
{
    internal interface ICascadingValueComponent
    {
        // This interface exists only so that CascadingParameterState has a way
        // to work with all CascadingValue<T> types regardless of T.

        bool CanSupplyValue(Type valueType, string valueName);

        object CurrentValue { get; }

        void Subscribe(ComponentState subscriber);

        void Unsubscribe(ComponentState subscriber);
    }
}
