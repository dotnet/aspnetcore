// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

/// <summary>
/// Creates an <see cref="EnumerableWrapperProvider"/> for interface types implementing the
/// <see cref="IEnumerable{T}"/> type.
/// </summary>
public class EnumerableWrapperProviderFactory : IWrapperProviderFactory
{
    private readonly IEnumerable<IWrapperProviderFactory> _wrapperProviderFactories;

    /// <summary>
    /// Initializes an <see cref="EnumerableWrapperProviderFactory"/> with a list
    /// <see cref="IWrapperProviderFactory"/>.
    /// </summary>
    /// <param name="wrapperProviderFactories">List of <see cref="IWrapperProviderFactory"/>.</param>
    public EnumerableWrapperProviderFactory(IEnumerable<IWrapperProviderFactory> wrapperProviderFactories)
    {
        ArgumentNullException.ThrowIfNull(wrapperProviderFactories);

        _wrapperProviderFactories = wrapperProviderFactories;
    }

    /// <summary>
    /// Gets an <see cref="EnumerableWrapperProvider"/> for the provided context.
    /// </summary>
    /// <param name="context">The <see cref="WrapperProviderContext"/>.</param>
    /// <returns>An instance of <see cref="EnumerableWrapperProvider"/> if the declared type is
    /// an interface and implements <see cref="IEnumerable{T}"/>.</returns>
    public IWrapperProvider? GetProvider(WrapperProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.IsSerialization)
        {
            // Example: IEnumerable<SerializableError>
            var declaredType = context.DeclaredType;

            // We only wrap interfaces types(ex: IEnumerable<T>, IQueryable<T>, IList<T> etc.) and not
            // concrete types like List<T>, Collection<T> which implement IEnumerable<T>.
            if (declaredType != null && declaredType.IsInterface && declaredType.IsGenericType)
            {
                var enumerableOfT = ClosedGenericMatcher.ExtractGenericInterface(
                    declaredType,
                    typeof(IEnumerable<>));
                if (enumerableOfT != null)
                {
                    var elementType = enumerableOfT.GenericTypeArguments[0];
                    var wrapperProviderContext = new WrapperProviderContext(elementType, context.IsSerialization);

                    var elementWrapperProvider =
                        _wrapperProviderFactories.GetWrapperProvider(wrapperProviderContext);

                    return new EnumerableWrapperProvider(enumerableOfT, elementWrapperProvider);
                }
            }
        }

        return null;
    }
}
