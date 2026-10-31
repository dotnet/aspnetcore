// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Provides programmatic configuration for the <see cref="RazorViewEngine"/>.
/// </summary>
public class RazorViewEngineOptions
{
    /// <summary>
    /// Gets a <see cref="IList{IViewLocationExpander}"/> used by the <see cref="RazorViewEngine"/>.
    /// </summary>
    public IList<IViewLocationExpander> ViewLocationExpanders { get; } = new List<IViewLocationExpander>();

    /// <summary>
    /// Gets the locations where <see cref="RazorViewEngine"/> will search for views.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The locations of the views returned from controllers that do not belong to an area.
    /// Locations are format strings (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) which may contain
    /// the following format items:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>{0} - Action Name</description>
    /// </item>
    /// <item>
    /// <description>{1} - Controller Name</description>
    /// </item>
    /// </list>
    /// <para>
    /// The values for these locations are case-sensitive on case-sensitive file systems.
    /// For example, the view for the <c>Test</c> action of <c>HomeController</c> should be located at
    /// <c>/Views/Home/Test.cshtml</c>. Locations such as <c>/views/home/test.cshtml</c> would not be discovered.
    /// </para>
    /// </remarks>
    public IList<string> ViewLocationFormats { get; } = new List<string>();

    /// <summary>
    /// Gets the locations where <see cref="RazorViewEngine"/> will search for views within an
    /// area.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The locations of the views returned from controllers that belong to an area.
    /// Locations are format strings (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) which may contain
    /// the following format items:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>{0} - Action Name</description>
    /// </item>
    /// <item>
    /// <description>{1} - Controller Name</description>
    /// </item>
    /// <item>
    /// <description>{2} - Area Name</description>
    /// </item>
    /// </list>
    /// <para>
    /// The values for these locations are case-sensitive on case-sensitive file systems.
    /// For example, the view for the <c>Test</c> action of <c>HomeController</c> under <c>Admin</c> area should
    /// be located at <c>/Areas/Admin/Views/Home/Test.cshtml</c>.
    /// Locations such as <c>/areas/admin/views/home/test.cshtml</c> would not be discovered.
    /// </para>
    /// </remarks>
    public IList<string> AreaViewLocationFormats { get; } = new List<string>();

    /// <summary>
    /// Gets the locations where <see cref="RazorViewEngine"/> will search for views (such as layouts and partials)
    /// when searched from the context of rendering a Razor Page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Locations are format strings (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) which may contain
    /// the following format items:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>{0} - View Name</description>
    /// </item>
    /// <item>
    /// <description>{1} - Page Name</description>
    /// </item>
    /// </list>
    /// <para>
    /// <see cref="PageViewLocationFormats"/> work in tandem with a view location expander to perform hierarchical
    /// path lookups. For instance, given a Page like /Account/Manage/Index using /Pages as the root, the view engine
    /// will search for views in the following locations:
    ///
    ///  /Pages/Account/Manage/{0}.cshtml
    ///  /Pages/Account/{0}.cshtml
    ///  /Pages/{0}.cshtml
    ///  /Pages/Shared/{0}.cshtml
    ///  /Views/Shared/{0}.cshtml
    /// </para>
    /// </remarks>
    public IList<string> PageViewLocationFormats { get; } = new List<string>();

    /// <summary>
    /// Gets the locations where <see cref="RazorViewEngine"/> will search for views (such as layouts and partials)
    /// when searched from the context of rendering a Razor Page within an area.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Locations are format strings (see <see href="https://msdn.microsoft.com/en-us/library/txafckwd.aspx"/>) which may contain
    /// the following format items:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>{0} - View Name</description>
    /// </item>
    /// <item>
    /// <description>{1} - Page Name</description>
    /// </item>
    /// <item>
    /// <description>{2} - Area Name</description>
    /// </item>
    /// </list>
    /// <para>
    /// <see cref="AreaPageViewLocationFormats"/> work in tandem with a view location expander to perform hierarchical
    /// path lookups. For instance, given a Page like /Areas/Account/Pages/Manage/User.cshtml using /Areas as the area pages root and
    /// /Pages as the root, the view engine will search for views in the following locations:
    ///
    ///  /Areas/Accounts/Pages/Manage/{0}.cshtml
    ///  /Areas/Accounts/Pages/{0}.cshtml
    ///  /Areas/Accounts/Pages/Shared/{0}.cshtml
    ///  /Areas/Accounts/Views/Shared/{0}.cshtml
    ///  /Pages/Shared/{0}.cshtml
    ///  /Views/Shared/{0}.cshtml
    /// </para>
    /// </remarks>
    public IList<string> AreaPageViewLocationFormats { get; } = new List<string>();
}
