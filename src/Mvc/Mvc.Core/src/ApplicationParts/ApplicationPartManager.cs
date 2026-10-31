// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

/// <summary>
/// Manages the parts and features of an MVC application.
/// </summary>
public class ApplicationPartManager
{
    /// <summary>
    /// Gets the list of <see cref="IApplicationFeatureProvider"/>s.
    /// </summary>
    public IList<IApplicationFeatureProvider> FeatureProviders { get; } =
        new List<IApplicationFeatureProvider>();

    /// <summary>
    /// Gets the list of <see cref="ApplicationPart"/> instances.
    /// <para>
    /// Instances in this collection are stored in precedence order. An <see cref="ApplicationPart"/> that appears
    /// earlier in the list has a higher precedence.
    /// An <see cref="IApplicationFeatureProvider"/> may choose to use this an interface as a way to resolve conflicts when
    /// multiple <see cref="ApplicationPart"/> instances resolve equivalent feature values.
    /// </para>
    /// </summary>
    public IList<ApplicationPart> ApplicationParts { get; } = new List<ApplicationPart>();

    /// <summary>
    /// Populates the given <paramref name="feature"/> using the list of
    /// <see cref="IApplicationFeatureProvider{TFeature}"/>s configured on the
    /// <see cref="ApplicationPartManager"/>.
    /// </summary>
    /// <typeparam name="TFeature">The type of the feature.</typeparam>
    /// <param name="feature">The feature instance to populate.</param>
    public void PopulateFeature<TFeature>(TFeature feature)
    {
        if (feature == null)
        {
            throw new ArgumentNullException(nameof(feature));
        }

        foreach (var provider in FeatureProviders.OfType<IApplicationFeatureProvider<TFeature>>())
        {
            provider.PopulateFeature(ApplicationParts, feature);
        }
    }

    internal void PopulateDefaultParts(string entryAssemblyName)
    {
        var assemblies = GetApplicationPartAssemblies(entryAssemblyName);

        var seenAssemblies = new HashSet<Assembly>();

        foreach (var assembly in assemblies)
        {
            if (!seenAssemblies.Add(assembly))
            {
                // "assemblies" may contain duplicate values, but we want unique ApplicationPart instances.
                // Note that we prefer using a HashSet over Distinct since the latter isn't
                // guaranteed to preserve the original ordering.
                continue;
            }

            var partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
            foreach (var applicationPart in partFactory.GetApplicationParts(assembly))
            {
                ApplicationParts.Add(applicationPart);
            }
        }
    }

    private static IEnumerable<Assembly> GetApplicationPartAssemblies(string entryAssemblyName)
    {
        var entryAssembly = Assembly.Load(new AssemblyName(entryAssemblyName));

        // Use ApplicationPartAttribute to get the closure of direct or transitive dependencies
        // that reference MVC.
        var assembliesFromAttributes = entryAssembly.GetCustomAttributes<ApplicationPartAttribute>()
            .Select(name => Assembly.Load(name.AssemblyName))
            .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal)
            .SelectMany(GetAssemblyClosure);

        // The SDK will not include the entry assembly as an application part. We'll explicitly list it
        // and have it appear before all other assemblies \ ApplicationParts.
        return GetAssemblyClosure(entryAssembly)
            .Concat(assembliesFromAttributes);
    }

    private static IEnumerable<Assembly> GetAssemblyClosure(Assembly assembly)
    {
        yield return assembly;

        var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: false)
            .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal);

        foreach (var relatedAssembly in relatedAssemblies)
        {
            yield return relatedAssembly;
        }
    }
}
