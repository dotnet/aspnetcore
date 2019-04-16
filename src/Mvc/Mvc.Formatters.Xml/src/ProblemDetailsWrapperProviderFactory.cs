// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    internal class ProblemDetailsWrapperProviderFactory : IWrapperProviderFactory
    {
        private readonly MvcXmlOptions _options;

        public ProblemDetailsWrapperProviderFactory(MvcXmlOptions options)
        {
            _options = options;
        }

        public IWrapperProvider GetProvider(WrapperProviderContext context)
        {
            if (context.DeclaredType == typeof(ProblemDetails))
            {
                if (_options.AllowRfc7807CompliantProblemDetailsFormat)
                {
                    return new WrapperProvider(typeof(ProblemDetailsWrapper), p => new ProblemDetailsWrapper((ProblemDetails)p));
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    return new WrapperProvider(typeof(ProblemDetails21Wrapper), p => new ProblemDetails21Wrapper((ProblemDetails)p));
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }

            if (context.DeclaredType == typeof(ValidationProblemDetails))
            {
                if (_options.AllowRfc7807CompliantProblemDetailsFormat)
                {
                    return new WrapperProvider(typeof(ValidationProblemDetailsWrapper), p => new ValidationProblemDetailsWrapper((ValidationProblemDetails)p));
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    return new WrapperProvider(typeof(ValidationProblemDetails21Wrapper), p => new ValidationProblemDetails21Wrapper((ValidationProblemDetails)p));
#pragma warning restore CS0618 // Type or member is obsolete
                }
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