using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Provides an abstraction for different UI frameworks to provide access to APIs from
    /// <see cref="Microsoft.Web.WebView2.Core.CoreWebView2"/> and related controls.
    /// </summary>
    public interface IWebView2Wrapper
    {
        CoreWebView2 CoreWebView2 { get; }
        Uri Source { get; set; }
        Task EnsureCoreWebView2Async(CoreWebView2Environment environment = null);
    }
}
