// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// A <see cref="TagHelper"/> that saves the state of Razor components rendered on the page up to that point.
    /// </summary>
    [HtmlTargetElement(TagHelperName, TagStructure = TagStructure.WithoutEndTag)]
    public class SaveComponentStateTagHelper : TagHelper
    {
        private const string TagHelperName = "save-component-state";

        /// <summary>
        /// Gets or sets the <see cref="Rendering.ViewContext"/> for the current request.
        /// </summary>
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public SaveMode SaveMode { get; set; }

        /// <inheritdoc />
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            var services = ViewContext.HttpContext.RequestServices;
            var manager = services.GetRequiredService<ComponentApplicationLifetime>();
            var store = SaveMode switch
            {
                SaveMode.Server => new ProtectedPrerenderComponentApplicationStore(
                    services.GetRequiredService<IDataProtectionProvider>()),
                SaveMode.WebAssembly => new PrerenderComponentApplicationStore(),
                _ => throw new InvalidOperationException("Invalid save mode.")
            };

            await manager.PauseAsync();
            await manager.PersistStateAsync(store);

            output.TagName = null;
            output.Content.Clear().AppendHtml(store.PersistedState);
        }
    }

    public enum SaveMode
    {
        Server,
        WebAssembly
    }
}
