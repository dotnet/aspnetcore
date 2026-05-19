// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Collection of <see cref="IPageConvention"/>.
/// </summary>
public class PageConventionCollection : Collection<IPageConvention>
{
    private readonly IServiceProvider? _serviceProvider;
    private MvcOptions? _mvcOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PageConventionCollection"/> class that is empty.
    /// </summary>
    public PageConventionCollection()
        : this((IServiceProvider?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PageConventionCollection"/> class
    /// as a wrapper for the specified list.
    /// </summary>
    /// <param name="conventions">The list that is wrapped by the new collection.</param>
    public PageConventionCollection(IList<IPageConvention> conventions)
        : base(conventions)
    {
    }

    internal PageConventionCollection(IServiceProvider? serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    internal MvcOptions MvcOptions
    {
        get
        {
            // Avoid eagerly getting to the MvcOptions from the options setup for RazorPagesOptions.
            _mvcOptions ??= _serviceProvider!.GetRequiredService<IOptions<MvcOptions>>().Value;
            return _mvcOptions;
        }
    }

    /// <summary>
    /// Creates and adds an <see cref="IPageApplicationModelConvention"/> that invokes an action on the
    /// <see cref="PageApplicationModel"/> for the page with the specified name.
    /// </summary>
    /// <param name="pageName">The name of the page e.g. <c>/Users/List</c></param>
    /// <param name="action">The <see cref="Action"/>.</param>
    /// <returns>The added <see cref="IPageApplicationModelConvention"/>.</returns>
    public IPageApplicationModelConvention AddPageApplicationModelConvention(
        string pageName,
        Action<PageApplicationModel> action)
    {
        EnsureValidPageName(pageName);

        ArgumentNullException.ThrowIfNull(action);

        return Add(new PageApplicationModelConvention(pageName, action));
    }

    /// <summary>
    /// Creates and adds an <see cref="IPageApplicationModelConvention"/> that invokes an action on the
    /// <see cref="PageApplicationModel"/> for the page with the specified name located in the specified area.
    /// </summary>
    /// <param name="areaName">The name of area.</param>
    /// <param name="pageName">
    /// The page name e.g. <c>/Users/List</c>
    /// <para>
    /// The page name is the path of the file without extension, relative to the pages root directory for the specified area.
    /// e.g. the page name for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage/Accounts</c>.
    /// </para>
    /// </param>
    /// <param name="action">The <see cref="Action"/>.</param>
    /// <returns>The added <see cref="IPageApplicationModelConvention"/>.</returns>
    public IPageApplicationModelConvention AddAreaPageApplicationModelConvention(
        string areaName,
        string pageName,
        Action<PageApplicationModel> action)
    {
        ArgumentException.ThrowIfNullOrEmpty(areaName);

        EnsureValidPageName(pageName);

        ArgumentNullException.ThrowIfNull(action);

        return Add(new PageApplicationModelConvention(areaName, pageName, action));
    }

    /// <summary>
    /// Creates and adds an <see cref="IPageApplicationModelConvention"/> that invokes an action on
    /// <see cref="PageApplicationModel"/> instances for all page under the specified folder.
    /// </summary>
    /// <param name="folderPath">The path of the folder relative to the Razor Pages root. e.g. <c>/Users/</c></param>
    /// <param name="action">The <see cref="Action"/>.</param>
    /// <returns>The added <see cref="IPageApplicationModelConvention"/>.</returns>
    public IPageApplicationModelConvention AddFolderApplicationModelConvention(string folderPath, Action<PageApplicationModel> action)
    {
        EnsureValidFolderPath(folderPath);

        ArgumentNullException.ThrowIfNull(action);

        return Add(new FolderApplicationModelConvention(folderPath, action));
    }

    /// <summary>
    /// Creates and adds an <see cref="IPageApplicationModelConvention"/> that invokes an action on
    /// <see cref="PageApplicationModel"/> instances for all pages under the specified area folder.
    /// </summary>
    /// <param name="areaName">The name of area.</param>
    /// <param name="folderPath">
    /// The folder path e.g. <c>/Manage/</c>
    /// <para>
    /// The folder path is the path of the folder, relative to the pages root directory for the specified area.
    /// e.g. the folder path for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage</c>.
    /// </para>
    /// </param>
    /// <param name="action">The <see cref="Action"/>.</param>
    /// <returns>The added <see cref="IPageApplicationModelConvention"/>.</returns>
    public IPageApplicationModelConvention AddAreaFolderApplicationModelConvention(
        string areaName,
        string folderPath,
        Action<PageApplicationModel> action)
    {
        ArgumentException.ThrowIfNullOrEmpty(areaName);

        EnsureValidFolderPath(folderPath);

        ArgumentNullException.ThrowIfNull(action);

        return Add(new FolderApplicationModelConvention(areaName, folderPath, action));
    }

    /// <summary>
    /// Creates and adds an <see cref="IPageRouteModelConvention"/> that invokes an action on the
    /// <see cref="PageRouteModel"/> for the page with the specified name.
    /// </summary>
    /// <param name="pageName">The name of the page e.g. <c>/Users/List</c></param>
    /// <param name="action">The <see cref="Action"/>.</param>
    /// <returns>The added <see cref="IPageRouteModelConvention"/>.</returns>
    public IPageRouteModelConvention AddPageRouteModelConvention(string pageName, Action<PageRouteModel> action)
    {
        EnsureValidPageName(pageName);

        ArgumentNullException.ThrowIfNull(action);

        return Add(new PageRouteModelConvention(pageName, action));
    }

    /// <summary>
    /// Creates and adds an <see cref="IPageRouteModelConvention"/> that invokes an action on the
    /// <see cref="PageRouteModel"/> for the page with the specified name located in the specified area.
    /// </summary>
    /// <param name="areaName">The area name.</param>
    /// <param name="pageName">
    /// The page name e.g. <c>/Users/List</c>
    /// <para>
    /// The page name is the path of the file without extension, relative to the pages root directory for the specified area.
    /// e.g. the page name for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage/Accounts</c>.
    /// </para>
    /// </param>
    /// <param name="action">The <see cref="Action"/>.</param>
    /// <returns>The added <see cref="IPageRouteModelConvention"/>.</returns>
    public IPageRouteModelConvention AddAreaPageRouteModelConvention(string areaName, string pageName, Action<PageRouteModel> action)
    {
        ArgumentException.ThrowIfNullOrEmpty(areaName);

        EnsureValidPageName(pageName);

        ArgumentNullException.ThrowIfNull(action);

        return Add(new PageRouteModelConvention(areaName, pageName, action));
    }

    /// <summary>
    /// Creates and adds an <see cref="IPageRouteModelConvention"/> that invokes an action on
    /// <see cref="PageRouteModel"/> instances for all page under the specified folder.
    /// </summary>
    /// <param name="folderPath">The path of the folder relative to the Razor Pages root. e.g. <c>/Users/</c></param>
    /// <param name="action">The <see cref="Action"/>.</param>
    /// <returns>The added <see cref="IPageApplicationModelConvention"/>.</returns>
    public IPageRouteModelConvention AddFolderRouteModelConvention(string folderPath, Action<PageRouteModel> action)
    {
        EnsureValidFolderPath(folderPath);

        ArgumentNullException.ThrowIfNull(action);

        return Add(new FolderRouteModelConvention(folderPath, action));
    }

    /// <summary>
    /// Creates and adds an <see cref="IPageRouteModelConvention"/> that invokes an action on
    /// <see cref="PageRouteModel"/> instances for all page under the specified area folder.
    /// </summary>
    /// <param name="areaName">The area name.</param>
    /// <param name="folderPath">
    /// The folder path e.g. <c>/Manage/</c>
    /// <para>
    /// The folder path is the path of the folder, relative to the pages root directory for the specified area.
    /// e.g. the folder path for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage</c>.
    /// </para>
    /// </param>
    /// <param name="action">The <see cref="Action"/>.</param>
    /// <returns>The added <see cref="IPageApplicationModelConvention"/>.</returns>
    public IPageRouteModelConvention AddAreaFolderRouteModelConvention(string areaName, string folderPath, Action<PageRouteModel> action)
    {
        ArgumentException.ThrowIfNullOrEmpty(areaName);

        EnsureValidFolderPath(folderPath);

        ArgumentNullException.ThrowIfNull(action);

        return Add(new FolderRouteModelConvention(areaName, folderPath, action));
    }

    /// <summary>
    /// Removes all <see cref="IPageConvention"/> instances of the specified type.
    /// </summary>
    /// <typeparam name="TPageConvention">The type to remove.</typeparam>
    public void RemoveType<TPageConvention>() where TPageConvention : IPageConvention
    {
        RemoveType(typeof(TPageConvention));
    }

    /// <summary>
    /// Removes all <see cref="IPageConvention"/> instances of the specified type.
    /// </summary>
    /// <param name="pageConventionType">The type to remove.</param>
    public void RemoveType(Type pageConventionType)
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            var pageConvention = this[i];
            if (pageConvention.GetType() == pageConventionType)
            {
                RemoveAt(i);
            }
        }
    }

    // Internal for unit testing
    internal static void EnsureValidPageName(string pageName, string argumentName = "pageName")
    {
        ArgumentException.ThrowIfNullOrEmpty(pageName);

        if (pageName[0] != '/' || pageName.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(Resources.FormatInvalidValidPageName(pageName), argumentName);
        }
    }

    // Internal for unit testing
    internal static void EnsureValidFolderPath(string folderPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(folderPath);

        if (folderPath[0] != '/')
        {
            throw new ArgumentException(Resources.PathMustBeRootRelativePath, nameof(folderPath));
        }
    }

    private TConvention Add<TConvention>(TConvention convention) where TConvention : IPageConvention
    {
        base.Add(convention);
        return convention;
    }

    private sealed class PageRouteModelConvention : IPageRouteModelConvention
    {
        private readonly string? _areaName;
        private readonly string _path;
        private readonly Action<PageRouteModel> _action;

        public PageRouteModelConvention(string path, Action<PageRouteModel> action)
            : this(null, path, action)
        {
        }

        public PageRouteModelConvention(string? areaName, string path, Action<PageRouteModel> action)
        {
            _areaName = areaName;
            _path = path;
            _action = action;
        }

        public void Apply(PageRouteModel model)
        {
            if (string.Equals(_areaName, model.AreaName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(model.ViewEnginePath, _path, StringComparison.OrdinalIgnoreCase))
            {
                _action(model);
            }
        }
    }

    private sealed class FolderRouteModelConvention : IPageRouteModelConvention
    {
        private readonly string? _areaName;
        private readonly string _folderPath;
        private readonly Action<PageRouteModel> _action;

        public FolderRouteModelConvention(string folderPath, Action<PageRouteModel> action)
            : this(null, folderPath, action)
        {
        }

        public FolderRouteModelConvention(string? areaName, string folderPath, Action<PageRouteModel> action)
        {
            _areaName = areaName;
            _folderPath = folderPath.TrimEnd('/');
            _action = action;
        }

        public void Apply(PageRouteModel model)
        {
            if (string.Equals(_areaName, model.AreaName, StringComparison.OrdinalIgnoreCase) &&
                PathBelongsToFolder(_folderPath, model.ViewEnginePath))
            {
                _action(model);
            }
        }
    }

    private sealed class PageApplicationModelConvention : IPageApplicationModelConvention
    {
        private readonly string? _areaName;
        private readonly string _path;
        private readonly Action<PageApplicationModel> _action;

        public PageApplicationModelConvention(string path, Action<PageApplicationModel> action)
            : this(null, path, action)
        {
        }

        public PageApplicationModelConvention(string? areaName, string path, Action<PageApplicationModel> action)
        {
            _areaName = areaName;
            _path = path;
            _action = action;
        }

        public void Apply(PageApplicationModel model)
        {
            if (string.Equals(model.ViewEnginePath, _path, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(model.AreaName, _areaName, StringComparison.OrdinalIgnoreCase))
            {
                _action(model);
            }
        }
    }

    private sealed class FolderApplicationModelConvention : IPageApplicationModelConvention
    {
        private readonly string? _areaName;
        private readonly string _folderPath;
        private readonly Action<PageApplicationModel> _action;

        public FolderApplicationModelConvention(string folderPath, Action<PageApplicationModel> action)
            : this(null, folderPath, action)
        {
        }

        public FolderApplicationModelConvention(string? areaName, string folderPath, Action<PageApplicationModel> action)
        {
            _areaName = areaName;
            _folderPath = folderPath.TrimEnd('/');
            _action = action;
        }

        public void Apply(PageApplicationModel model)
        {
            if (string.Equals(_areaName, model.AreaName, StringComparison.OrdinalIgnoreCase) &&
                PathBelongsToFolder(_folderPath, model.ViewEnginePath))
            {
                _action(model);
            }
        }
    }

    // Internal for unit testing
    internal static bool PathBelongsToFolder(string folderPath, string viewEnginePath)
    {
        if (folderPath == "/")
        {
            // Root directory covers everything.
            return true;
        }

        return viewEnginePath.Length > folderPath.Length &&
            viewEnginePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase) &&
            viewEnginePath[folderPath.Length] == '/';
    }
}
