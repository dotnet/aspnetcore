// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

internal sealed class ProblemDetailsWrapperProviderFactory : IWrapperProviderFactory
{
    public IWrapperProvider? GetProvider(WrapperProviderContext context)
    {
        if (context.DeclaredType == typeof(ProblemDetails))
        {
            return new WrapperProvider(typeof(ProblemDetailsWrapper), p => new ProblemDetailsWrapper((ProblemDetails)p!));
        }

        if (context.DeclaredType == typeof(ValidationProblemDetails))
        {
            return new WrapperProvider(typeof(ValidationProblemDetailsWrapper), p => new ValidationProblemDetailsWrapper((ValidationProblemDetails)p!));
        }

        return null;
    }

    private sealed class WrapperProvider : IWrapperProvider
    {
        public WrapperProvider(Type wrappingType, Func<object?, object?> wrapDelegate)
        {
            WrappingType = wrappingType;
            WrapDelegate = wrapDelegate;
        }

        public Type WrappingType { get; }

        public Func<object?, object?> WrapDelegate { get; }

        public object? Wrap(object? original) => WrapDelegate(original);
    }
}
