// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    internal class ProblemDetailsWrapperProviderFactory : IWrapperProviderFactory
    {
        public IWrapperProvider GetProvider(WrapperProviderContext context)
        {
            if (context.DeclaredType == typeof(ProblemDetails))
            {
                return new WrapperProvider(typeof(ProblemDetailsWrapper), p => new ProblemDetailsWrapper((ProblemDetails)p));
            }

            if (context.DeclaredType == typeof(ValidationProblemDetails))
            {
                return new WrapperProvider(typeof(ValidationProblemDetailsWrapper), p => new ValidationProblemDetailsWrapper((ValidationProblemDetails)p));
            }

            return null;
        }

        private class WrapperProvider : IWrapperProvider
        {
            public WrapperProvider(Type wrappingType, Func<object, object> wrapDelegate)
            {
                WrappingType = wrappingType;
                WrapDelegate = wrapDelegate;
            }

            public Type WrappingType { get; }

            public Func<object, object> WrapDelegate { get; }

            public object Wrap(object original) => WrapDelegate(original);
        }
    }
}
