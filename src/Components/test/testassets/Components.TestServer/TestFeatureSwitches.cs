// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.Extensions.DependencyInjection;

namespace TestServer;

/// <summary>
/// Overrides Components feature switches for servers that are hosted in the test process, and restores
/// the original process configuration afterwards.
/// </summary>
/// <remarks>
/// Components reads each of these switches once into a static field during static initialization, so
/// calling <see cref="AppContext.SetSwitch(string, bool)"/> from a test or a startup class has no effect
/// once any in-process server has already initialized the field. These helpers update the cached fields
/// as well. The values are process-global, so every override must be paired with a reset; that is only
/// safe because the E2E suite runs serially, and enabling parallelization would let servers and tests
/// that need opposite values race with each other.
/// </remarks>
public static class TestFeatureSwitches
{
    private const string DisableThrowNavigationExceptionSwitchName =
        "Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException";

    private const string MetadataUpdaterIsSupportedSwitchName =
        "System.Reflection.Metadata.MetadataUpdater.IsSupported";

    private const string EnableUrlBasedQuickGridNavigationAndSortingSwitchName =
        "Microsoft.AspNetCore.Components.QuickGrid.EnableUrlBasedQuickGridNavigationAndSorting";

    private const string HotReloadManagerTypeName = "Microsoft.AspNetCore.Components.HotReload.HotReloadManager";

    private static readonly FieldInfo s_throwNavigationExceptionField =
        typeof(RazorComponentsServiceCollectionExtensions).Assembly
            .GetType("Microsoft.AspNetCore.Components.Endpoints.HttpNavigationManager", throwOnError: true)!
            .GetField("s_throwNavigationException", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly FieldInfo s_enableUrlBasedQuickGridNavigationAndSortingField =
        typeof(QuickGrid<>).Assembly
            .GetType("Microsoft.AspNetCore.Components.QuickGrid.QuickGridFeatureFlags", throwOnError: true)!
            .GetField("s_enableUrlBasedQuickGridNavigationAndSorting", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly bool s_defaultDisableThrowNavigationException;
    private static readonly bool s_defaultHotReloadSupported;
    private static readonly bool s_defaultUrlBasedQuickGridNavigationAndSorting;

    // Capturing the defaults from an explicit static constructor, rather than from field initializers,
    // guarantees they are read before the first Set call on this class overwrites the switches.
    static TestFeatureSwitches()
    {
        s_defaultDisableThrowNavigationException =
            AppContext.TryGetSwitch(DisableThrowNavigationExceptionSwitchName, out var disableThrowNavigationException)
            && disableThrowNavigationException;

        s_defaultHotReloadSupported =
            !AppContext.TryGetSwitch(MetadataUpdaterIsSupportedSwitchName, out var isSupported) || isSupported;

        s_defaultUrlBasedQuickGridNavigationAndSorting =
            !AppContext.TryGetSwitch(EnableUrlBasedQuickGridNavigationAndSortingSwitchName, out var isEnabled) || isEnabled;
    }

    public static void SetDisableThrowNavigationException(bool disableThrowNavigationException)
    {
        AppContext.SetSwitch(DisableThrowNavigationExceptionSwitchName, disableThrowNavigationException);
        s_throwNavigationExceptionField.SetValue(null, !disableThrowNavigationException);
    }

    public static void ResetDisableThrowNavigationException()
        => SetDisableThrowNavigationException(s_defaultDisableThrowNavigationException);

    public static void SetHotReloadSupported(bool isSupported)
    {
        AppContext.SetSwitch(MetadataUpdaterIsSupportedSwitchName, isSupported);

        // The shared HotReloadManager source is compiled into several framework assemblies, each with its
        // own cached field, so every loaded copy has to be updated.
        foreach (var field in GetHotReloadSupportedFields())
        {
            field.SetValue(null, isSupported);
        }
    }

    public static void ResetHotReloadSupported()
        => SetHotReloadSupported(s_defaultHotReloadSupported);

    public static void SetUrlBasedQuickGridNavigationAndSorting(bool isEnabled)
    {
        AppContext.SetSwitch(EnableUrlBasedQuickGridNavigationAndSortingSwitchName, isEnabled);
        s_enableUrlBasedQuickGridNavigationAndSortingField.SetValue(null, isEnabled);
    }

    public static void ResetUrlBasedQuickGridNavigationAndSorting()
        => SetUrlBasedQuickGridNavigationAndSorting(s_defaultUrlBasedQuickGridNavigationAndSorting);

    private static IEnumerable<FieldInfo> GetHotReloadSupportedFields()
        => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(HotReloadManagerTypeName, throwOnError: false))
            .OfType<Type>()
            .Select(type => type.GetField("s_isSupported", BindingFlags.Static | BindingFlags.NonPublic))
            .OfType<FieldInfo>();
}
