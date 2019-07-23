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
            component.SetParametersAsync(DictionaryToParameterCollection(parameters));
        }

        private static ParameterCollection DictionaryToParameterCollection(
            IDictionary<string, object> parameters)
        {
            var builder = new RenderTreeBuilder();
            builder.OpenComponent<AbstractComponent>(0);
            foreach (var pair in parameters)
            {
                builder.AddAttribute(0, pair.Key, pair.Value);
            }
            builder.CloseElement();

            return new ParameterCollection(builder.GetFrames().Array, 0);
        }

        private abstract class AbstractComponent : IComponent
        {
            public abstract void Attach(RenderHandle renderHandle);
            public abstract Task SetParametersAsync(ParameterCollection parameters);
        }
    }
}
