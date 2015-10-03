// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [DebuggerDisplay("TypeFilter: Type={ImplementationType} Order={Order}")]
    public class TypeFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        private ObjectFactory _factory;

        public TypeFilterAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ImplementationType = type;
        }

        public object[] Arguments { get; set; }

        public Type ImplementationType { get; private set; }

        public int Order { get; set; }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (_factory == null)
            {
                var argumentTypes = Arguments?.Select(a => a.GetType())?.ToArray();

                _factory = ActivatorUtilities.CreateFactory(ImplementationType, argumentTypes ?? Type.EmptyTypes);
            }

            return (IFilterMetadata)_factory(serviceProvider, Arguments);
        }
    }
}