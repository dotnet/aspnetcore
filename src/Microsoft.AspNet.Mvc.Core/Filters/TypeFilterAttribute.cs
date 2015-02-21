// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [DebuggerDisplay("TypeFilter: Type={ImplementationType} Order={Order}")]
    public class TypeFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        private ObjectFactory _factory;

        public TypeFilterAttribute([NotNull] Type type)
        {
            ImplementationType = type;
        }

        public object[] Arguments { get; set; }

        public Type ImplementationType { get; private set; }

        public int Order { get; set; }

        public IFilter CreateInstance([NotNull] IServiceProvider serviceProvider)
        {
            if (_factory == null)
            {
                var argumentTypes = Arguments?.Select(a => a.GetType())?.ToArray();

                _factory = ActivatorUtilities.CreateFactory(ImplementationType, argumentTypes ?? Type.EmptyTypes);
            }

            return (IFilter)_factory(serviceProvider, Arguments);
        }
    }
}