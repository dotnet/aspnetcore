// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class DefaultErrorDescriptorFactory : IErrorDescriptionFactory
    {
        private readonly IErrorDescriptorProvider[] _providers;

        public DefaultErrorDescriptorFactory(IEnumerable<IErrorDescriptorProvider> providers)
        {
            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            _providers = providers.OrderBy(p => p.Order).ToArray();
        }

        public object CreateErrorDescription(ActionDescriptor actionDescriptor, object result)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var context = new ErrorDescriptionContext(actionDescriptor)
            {
                Result = result,
            };

            for (var i = 0; i < _providers.Length; i++)
            {
                _providers[i].OnProvidersExecuting(context);
            }

            return context.Result ?? result;
        }
    }
}
