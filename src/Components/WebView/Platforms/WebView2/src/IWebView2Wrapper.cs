// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Types in the Microsoft.AspNetCore.Components.WebView.WebView2 are not recommended for use outside
    /// of the Blazor framework. These types will change in a future release.
    ///
    /// Provides an abstraction for different UI frameworks to provide access to APIs from
    /// Microsoft.Web.WebView2.Core.CoreWebView2 and related controls.
    /// </summary>
    public interface IWebView2Wrapper
    {
        /// <summary>
        /// Creates a WebView2 Environment using the installed or a custom WebView2 Runtime version.
        /// The implementation should store the CoreWebView2Environment created in this method so that it can
        /// be used in <see cref="CreateEnvironmentAsync"/>.
        /// </summary>
        Task CreateEnvironmentAsync();

        /// <summary>
        /// The underlying CoreWebView2. Use this property to perform more operations on the WebView2 content than is exposed
        /// on the WebView2. This value is <c>null</c> until it is initialized. You can force the underlying CoreWebView2 to
        /// initialize via the InitializeAsync method.
        /// </summary>
        ICoreWebView2Wrapper CoreWebView2 { get; }

        /// <summary>
        /// Gets or sets the source URI of the control. Setting the source URI causes page navigation.
        /// </summary>
        Uri Source { get; set; }

        /// <summary>
        /// Explicitly trigger initialization of the control's CoreWebView2. The implementation should use the CoreWebView2Environment that was
        /// created in <see cref="CreateEnvironmentAsync"/>.
        /// </summary>
        Task EnsureCoreWebView2Async();

        /// <summary>
        /// Event that occurs when an accelerator key is pressed.
        /// </summary>
        Action AddAcceleratorKeyPressedHandler(EventHandler<ICoreWebView2AcceleratorKeyPressedEventArgsWrapper> eventHandler);
    }
}
