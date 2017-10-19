// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Dispatcher
{
    internal class RoutePatternTemplateFactory : TemplateFactory<DispatcherValueCollection>
    {
        private readonly TemplateAddressSelector _selector;
        private readonly RoutePatternBinderFactory _binderFactory;

        public RoutePatternTemplateFactory(TemplateAddressSelector selector, RoutePatternBinderFactory binderFactory)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            if (binderFactory == null)
            {
                throw new ArgumentNullException(nameof(binderFactory));
            }

            _selector = selector;
            _binderFactory = binderFactory;
        }

        public override Template GetTemplate(DispatcherValueCollection key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var address = _selector.SelectAddress(key);
            if (address == null)
            {
                return null;
            }

            if (address is ITemplateAddress templateAddress)
            {
                var binder = _binderFactory.Create(templateAddress.Template, templateAddress.Defaults);
                return new RoutePatternTemplate(binder);
            }

            return null;
        }
    }
}
