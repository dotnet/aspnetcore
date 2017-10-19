// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DefaultTemplateFactory : TemplateFactory
    {
        private readonly ITemplateFactoryComponent[] _components;

        public DefaultTemplateFactory(IEnumerable<ITemplateFactoryComponent> components)
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            _components = components.ToArray();
        }

        public override Template GetTemplateFromKey<TKey>(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            for (var i = 0; i < _components.Length; i++)
            {
                var component = _components[i] as TemplateFactory<TKey>;
                if (component == null)
                {
                    continue;
                }

                var template = component.GetTemplate(key);
                if (template != null)
                {
                    return template;
                }
            }

            return null;
        }
    }
}
