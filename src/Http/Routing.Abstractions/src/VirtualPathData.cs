// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Represents information about the route and virtual path that are the result of
/// generating a URL with the ASP.NET routing middleware.
/// </summary>
public class VirtualPathData
{
    private RouteValueDictionary _dataTokens;
    private string _virtualPath;

    /// <summary>
    ///  Initializes a new instance of the <see cref="VirtualPathData"/> class.
    /// </summary>
    /// <param name="router">The object that is used to generate the URL.</param>
    /// <param name="virtualPath">The generated URL.</param>
    public VirtualPathData(IRouter router, string virtualPath)
        : this(router, virtualPath, dataTokens: null)
    {
    }

    /// <summary>
    ///  Initializes a new instance of the <see cref="VirtualPathData"/> class.
    /// </summary>
    /// <param name="router">The object that is used to generate the URL.</param>
    /// <param name="virtualPath">The generated URL.</param>
    /// <param name="dataTokens">The collection of custom values.</param>
    public VirtualPathData(
        IRouter router,
        string virtualPath,
        RouteValueDictionary dataTokens)
    {
        ArgumentNullException.ThrowIfNull(router);

        Router = router;
        VirtualPath = virtualPath;
        _dataTokens = dataTokens == null ? null : new RouteValueDictionary(dataTokens);
    }

    /// <summary>
    /// Gets the collection of custom values for the <see cref="Router"/>.
    /// </summary>
    public RouteValueDictionary DataTokens
    {
        get
        {
            if (_dataTokens == null)
            {
                _dataTokens = new RouteValueDictionary();
            }

            return _dataTokens;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="IRouter"/> that was used to generate the URL.
    /// </summary>
    public IRouter Router { get; set; }

    /// <summary>
    /// Gets or sets the URL that was generated from the <see cref="Router"/>.
    /// </summary>
    public string VirtualPath
    {
        get
        {
            return _virtualPath;
        }
        set
        {
            _virtualPath = NormalizePath(value);
        }
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        if (!path.StartsWith('/'))
        {
            return "/" + path;
        }

        return path;
    }
}
