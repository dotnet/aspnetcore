// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ActionResult"/> that returns a Found (302), Moved Permanently (301), Temporary Redirect (307),
/// or Permanent Redirect (308) response with a Location header to the supplied local URL.
/// </summary>
public class LocalRedirectResult : ActionResult
{
    private string _localUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRedirectResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="localUrl">The local URL to redirect to.</param>
    public LocalRedirectResult([StringSyntax(StringSyntaxAttribute.Uri, UriKind.Relative)] string localUrl)
         : this(localUrl, permanent: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRedirectResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="localUrl">The local URL to redirect to.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    public LocalRedirectResult([StringSyntax(StringSyntaxAttribute.Uri, UriKind.Relative)] string localUrl, bool permanent)
        : this(localUrl, permanent, preserveMethod: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRedirectResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="localUrl">The local URL to redirect to.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request's method.</param>
    public LocalRedirectResult([StringSyntax(StringSyntaxAttribute.Uri, UriKind.Relative)] string localUrl, bool permanent, bool preserveMethod)
    {
        ArgumentException.ThrowIfNullOrEmpty(localUrl);

        Permanent = permanent;
        PreserveMethod = preserveMethod;
        Url = localUrl;
    }

    /// <summary>
    /// Gets or sets the value that specifies that the redirect should be permanent if true or temporary if false.
    /// </summary>
    public bool Permanent { get; set; }

    /// <summary>
    /// Gets or sets an indication that the redirect preserves the initial request method.
    /// </summary>
    public bool PreserveMethod { get; set; }

    /// <summary>
    /// Gets or sets the local URL to redirect to.
    /// </summary>
    public string Url
    {
        get => _localUrl;

        [MemberNotNull(nameof(_localUrl))]
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);

            _localUrl = value;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="IUrlHelper"/> for this result.
    /// </summary>
    public IUrlHelper? UrlHelper { get; set; }

    /// <inheritdoc />
    public override Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<LocalRedirectResult>>();
        return executor.ExecuteAsync(context, this);
    }
}
