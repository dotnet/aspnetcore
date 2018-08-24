// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    internal class WrapperProviderFactory : IWrapperProviderFactory
    {
        public WrapperProviderFactory(Type declaredType, Type wrappingType, Func<object, object> wrapper)
        {
            DeclaredType = declaredType;
            WrappingType = wrappingType;
            Wrapper = wrapper;
        }

        public Type DeclaredType { get; }

        public Type WrappingType { get; }

        public Func<object, object> Wrapper { get; }

        public IWrapperProvider GetProvider(WrapperProviderContext context)
        {
            if (context.DeclaredType == DeclaredType)
            {
                return new WrapperProvider(this);
            }

            return null;
        }

        private class WrapperProvider : IWrapperProvider
        {
            private readonly WrapperProviderFactory _wrapperFactory;

            public WrapperProvider(WrapperProviderFactory wrapperFactory)
            {
                _wrapperFactory = wrapperFactory;
            }

            public Type WrappingType => _wrapperFactory.WrappingType;

            public object Wrap(object original)
            {
                return _wrapperFactory.Wrapper(original);
            }
        }
    }
}