// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Internal.Common.FileProviders;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.FileProviders;
using System.Linq;
using AngleSharp.Parser.Html;
using AngleSharp.Dom;
using AngleSharp;

namespace Microsoft.AspNetCore.Blazor.Build.Core.FileSystem
{
    internal class IndexHtmlFileProvider : InMemoryFileProvider
    {
        public IndexHtmlFileProvider(string htmlTemplate, string assemblyName, IEnumerable<IFileInfo> binFiles)
            : base(ComputeContents(htmlTemplate, assemblyName, binFiles))
        {
        }

        private static IEnumerable<(string, byte[])> ComputeContents(string htmlTemplate, string assemblyName, IEnumerable<IFileInfo> binFiles)
        {
            if (htmlTemplate != null)
            {
                var html = GetIndexHtmlContents(htmlTemplate, assemblyName, binFiles);
                var htmlBytes = Encoding.UTF8.GetBytes(html);
                yield return ("/index.html", htmlBytes);
            }
        }

        /// <summary>
        /// Injects the Blazor boot code and supporting config data at a user-designated
        /// script tag identified with a <c>type</c> of <c>blazor-boot</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If a matching script tag is found, then it will be adjusted to inject
        /// supporting configuration data, including a <c>src</c> attribute that
        /// will load the Blazor client-side library.  Any existing attribute
        /// names that match the boot config data will be overwritten, but other
        /// user-supplied attributes will be left intact.  This allows, for example,
        /// to designate asynchronous loading or deferred running of the script
        /// reference.
        /// </para><para>
        /// If no matching script tag is found, it is assumed that the user is
        /// responsible for completing the Blazor boot process.
        /// </para>
        /// </remarks>
        private static string GetIndexHtmlContents(string htmlTemplate, string assemblyName, IEnumerable<IFileInfo> binFiles)
        {
            var parser = new HtmlParser();
            var dom = parser.Parse(htmlTemplate);

            // First see if the user has declared a 'boot' script,
            // then it's their responsibility to load blazor.js
            var bootScript = dom.Body?.QuerySelectorAll("script")
                   .Where(x => x.Attributes["type"]?.Value == "blazor-boot").FirstOrDefault();

            // If we find a script tag that is decorated with a type="blazor-boot"
            // this will be the point at which we start the Blazor boot process
            if (bootScript != null)
            {
                // We need to remove the 'type="blazor-boot"' so that
                // it reverts to being processed as JS by the browser
                bootScript.RemoveAttribute("type");

                // Leave any user-specified attributes on the tag as-is
                // and add/overwrite the config data needed to boot Blazor
                InjectBootConfig(bootScript, assemblyName, binFiles);
            }

            // If no blazor-boot script tag was found, we skip it and
            // leave it up to the user to handle kicking off the boot

            using (var writer = new StringWriter())
            {
                dom.ToHtml(writer, new AutoSelectedMarkupFormatter());
                return writer.ToString();
            }
        }

        private static void InjectBootConfig(IElement script, string assemblyName, IEnumerable<IFileInfo> binFiles)
        {
            var assemblyNameWithExtension = $"{assemblyName}.dll";
            var referenceNames = binFiles
                .Where(file => !string.Equals(file.Name, assemblyNameWithExtension))
                .Select(file => file.Name);
            var referencesAttribute = string.Join(",", referenceNames.ToArray());

            script.SetAttribute("src", "/_framework/blazor.js");
            script.SetAttribute("main", assemblyNameWithExtension);
            script.SetAttribute("references", referencesAttribute);
        }
    }
}
