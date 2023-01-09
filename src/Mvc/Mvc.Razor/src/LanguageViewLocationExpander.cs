// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// A <see cref="IViewLocationExpander"/> that adds the language as an extension prefix to view names. Language
/// that is getting added as extension prefix comes from <see cref="Microsoft.AspNetCore.Http.HttpContext"/>.
/// </summary>
/// <example>
/// For the default case with no areas, views are generated with the following patterns (assuming controller is
/// "Home", action is "Index" and language is "en")
/// Views/Home/en/Action
/// Views/Home/Action
/// Views/Shared/en/Action
/// Views/Shared/Action
/// </example>
public class LanguageViewLocationExpander : IViewLocationExpander
{
    private const string ValueKey = "language";
    private readonly LanguageViewLocationExpanderFormat _format;

    /// <summary>
    /// Instantiates a new <see cref="LanguageViewLocationExpander"/> instance.
    /// </summary>
    public LanguageViewLocationExpander()
        : this(LanguageViewLocationExpanderFormat.Suffix)
    {
    }

    /// <summary>
    /// Instantiates a new <see cref="LanguageViewLocationExpander"/> instance.
    /// </summary>
    /// <param name="format">The <see cref="LanguageViewLocationExpanderFormat"/>.</param>
    public LanguageViewLocationExpander(LanguageViewLocationExpanderFormat format)
    {
        _format = format;
    }

    /// <inheritdoc />
    public void PopulateValues(ViewLocationExpanderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Using CurrentUICulture so it loads the locale specific resources for the views.
        context.Values[ValueKey] = CultureInfo.CurrentUICulture.Name;
    }

    /// <inheritdoc />
    public virtual IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(viewLocations);

        context.Values.TryGetValue(ValueKey, out var value);

        if (!string.IsNullOrEmpty(value))
        {
            CultureInfo culture;
            try
            {
                culture = new CultureInfo(value);
            }
            catch (CultureNotFoundException)
            {
                return viewLocations;
            }

            return ExpandViewLocationsCore(viewLocations, culture);
        }

        return viewLocations;
    }

    private IEnumerable<string> ExpandViewLocationsCore(IEnumerable<string> viewLocations, CultureInfo cultureInfo)
    {
        foreach (var location in viewLocations)
        {
            var temporaryCultureInfo = cultureInfo;

            while (temporaryCultureInfo != temporaryCultureInfo.Parent)
            {
                if (_format == LanguageViewLocationExpanderFormat.SubFolder)
                {
                    yield return location.Replace("{0}", temporaryCultureInfo.Name + "/{0}");
                }
                else
                {
                    yield return location.Replace("{0}", "{0}." + temporaryCultureInfo.Name);
                }

                temporaryCultureInfo = temporaryCultureInfo.Parent;
            }

            yield return location;
        }
    }
}
