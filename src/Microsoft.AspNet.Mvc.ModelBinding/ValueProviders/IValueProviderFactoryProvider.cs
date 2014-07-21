using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Provides an activated collection of <see cref="IValueProviderFactory"/> instances.
    /// </summary>
    public interface IValueProviderFactoryProvider
    {
        /// <summary>
        /// Gets a collection of activated IValueProviderFactory instances.
        /// </summary>
        IReadOnlyList<IValueProviderFactory> ValueProviderFactories { get; }
    }
}