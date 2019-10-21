// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Test.Helpers
{
    public static class IComponentExtensions
    {
        public static void SetParameters(
            this IComponent component,
            Dictionary<string, object> parameters)
        {
            component.SetParametersAsync(ParameterView.FromDictionary(parameters));
        }
    }
}
