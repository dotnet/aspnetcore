using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Components.WebView
{
    internal class StaticContentProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly Uri _appBaseUri;
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        public StaticContentProvider(IFileProvider fileProvider, Uri appBaseUri)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            _appBaseUri = appBaseUri ?? throw new ArgumentNullException(nameof(appBaseUri));
        }

        public bool TryGetResponseContent(string requestUri, out int statusCode, out string statusMessage, out Stream content, out string headers)
        {
            var fileUri = new Uri(requestUri);
            if (_appBaseUri.IsBaseOf(fileUri))
            {
                var relativePath = _appBaseUri.MakeRelativeUri(fileUri).ToString();
                if (relativePath.Equals(string.Empty, StringComparison.Ordinal))
                {
                    relativePath = "index.html";
                }

                // Content in the file provider takes first priority
                var fileInfo = _fileProvider.GetFileInfo(relativePath);
                if (fileInfo.Exists)
                {
                    content = fileInfo.CreateReadStream();
                }
                else
                {
                    // If there's no match, fall back on serving embedded framework content
                    TryGetFrameworkFile(relativePath, out content);
                }

                if (content != null)
                {
                    statusCode = 200;
                    statusMessage = "OK";
                    headers = GetResponseHeaders(GetResponseContentTypeOrDefault(relativePath));
                }
                else
                {
                    content = new MemoryStream(Encoding.UTF8.GetBytes($"There is no content at {relativePath}"));
                    statusCode = 404;
                    statusMessage = "Not found";
                    headers = GetResponseHeaders("text/plain");
                }

                // Always respond to requests within the base URI, even if there's no matching file
                return true;
            }

            statusCode = default;
            statusMessage = default;
            headers = default;
            content = default;
            return false;
        }

        private static bool TryGetFrameworkFile(string relativePath, out Stream content)
        {
            // We're not trying to simulate everything a real webserver does. We don't need to
            // support querystring parameters, for example. It's enough to require an exact match.
            const string frameworkPrefix = "_framework/";
            if (relativePath.StartsWith(frameworkPrefix, StringComparison.Ordinal))
            {
                var pathWithinFramework = relativePath.Substring(frameworkPrefix.Length);
                switch (pathWithinFramework)
                {
                    case "blazor.webview.js":
                    case "blazor.webview.js.map":
                        var assembly = typeof(StaticContentProvider).Assembly;
                        content = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.{pathWithinFramework}");
                        return content != null;
                }
            }

            content = default;
            return false;
        }

        private static string GetResponseContentTypeOrDefault(string path)
            => ContentTypeProvider.TryGetContentType(path, out var matchedContentType)
            ? matchedContentType
            : "application/octet-stream";

        private static string GetResponseHeaders(string contentType)
            => $"Content-Type: {contentType}{Environment.NewLine}Cache-Control: no-cache, max-age=0, must-revalidate, no-store";
    }
}
