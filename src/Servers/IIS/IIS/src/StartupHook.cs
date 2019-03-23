// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.StackTrace.Sources;

internal class StartupHook
{
    public static void Initialize()
    {
        Console.WriteLine("test");
        AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
        {
            var builder = new StringBuilder();
            var exception = eventArgs.Exception;
            // Pinvoke for content root.
            var exceptionDetailProvider = new ExceptionDetailsProvider(
               new PhysicalFileProvider(), // TODO get content root here
               sourceCodeLineCount: 6);

            var details = exceptionDetailProvider.GetDetails(exception);
            var detail = details.First();

            builder.Append("<h1>An error occurred while starting the application.</h1></br>");
            builder.Append($"<div class=\"titleerror\">{HtmlEncoder.Default.Encode(exception.GetType().ToString())}: {HtmlEncodeAndReplaceLineBreaks(exception.Message)}");

            var firstFrame = detail.StackFrames.FirstOrDefault();
            var location = string.Empty;
            if (firstFrame != null)
            {
                location = firstFrame.Function;
            }

            if (!string.IsNullOrEmpty(location) && firstFrame != null && !string.IsNullOrEmpty(firstFrame.File))
            {
                builder.Append($"<p class=\"location\">{HtmlEncoder.Default.Encode(location)} in <code title=\"{HtmlEncoder.Default.Encode(firstFrame.File)}\">{HtmlEncoder.Default.Encode(Path.GetFileName(firstFrame.File))}</code>, line {firstFrame.Line}</p>");
            }
            else if (!string.IsNullOrEmpty(location))
            {
                builder.Append($"<p class=\"location\">{HtmlEncoder.Default.Encode(location)}</p>");
            }
            else
            {
                builder.Append($"<p class=\"location\">@Resources.ErrorPageHtml_UnknownLocation</p>");
            }

            var reflectionTypeLoadException = detail.Error as ReflectionTypeLoadException;
            if (reflectionTypeLoadException != null)
            {
                if (reflectionTypeLoadException.LoaderExceptions.Length > 0)
                {
                    builder.Append("<h3>Loader Exceptions:</h3></br>");
                    builder.Append("<ul>");

                    foreach (var ex in reflectionTypeLoadException.LoaderExceptions)
                    {
                        builder.Append($"<li>{HtmlEncoder.Default.Encode(ex.Message)}</li>");
                    }
                    builder.Append("</ul>");
                }
            }

            builder.Append("<div id=\"stackpage\" class=\"page\">");
            builder.Append("<ul>");

            var exceptionCount = 0;
            var stackFrameCount = 0;
            var exceptionDetailId = "";
            var frameId = "";
            foreach (var errorDetail in details)
            {
                exceptionCount++;
                exceptionDetailId = "exceptionDetail" + exceptionCount;
                builder.Append("<li>");
                builder.Append($"<h2 class=\"stackerror\">{HtmlEncoder.Default.Encode(errorDetail.Error.GetType().Name)}: {HtmlEncoder.Default.Encode(errorDetail.Error.Message)}</h2>");
                builder.Append("<ul>");
                foreach (var frame in errorDetail.StackFrames)
                {
                    stackFrameCount++;
                    frameId = "frame" + stackFrameCount;
                    builder.Append($"<li class=\"frame\" id=\"{frameId}\">");
                    if (string.IsNullOrEmpty(frame.File))
                    {
                        builder.Append($"<h3>{HtmlEncoder.Default.Encode(frame.Function)}</h3>");
                    }
                    else
                    {
                        builder.Append($"<h3>{HtmlEncoder.Default.Encode(frame.Function)} in <code title=\"{HtmlEncoder.Default.Encode(frame.File)}\">{HtmlEncoder.Default.Encode(Path.GetFileName(frame.File))}</code></h3>");
                    }

                    if (frame.Line != 0 && frame.ContextCode.Any())
                    {
                        builder.Append($"<button class=\"expandCollapseButton\" data-frameId=\"{HtmlEncoder.Default.Encode(frameId)}\">+</button>");
                        builder.Append($"<div class=\"source\">");
                        if (frame.PreContextCode.Any())
                        {
                            builder.Append($"<ol start=\"{frame.PreContextLine}\" class=\"collapsible\">");
                            foreach (var line in frame.PreContextCode)
                            {
                                builder.Append($"<li><span>{HtmlEncoder.Default.Encode(line)}</span></li>");
                            }
                            builder.Append("</ol>");
                        }
                        builder.Append($"<ol start=\"{frame.Line}\" class=\"highlight\">");
                        foreach(var line in frame.ContextCode)
                        {
                            builder.Append($"<li><span>{HtmlEncoder.Default.Encode(line)}</span></li>");
                        }
                        builder.Append("</ol>");

                        if (frame.PostContextCode.Any())
                        {
                            builder.Append($"<ol start=\"{(frame.Line + 1)}\" class=\"collapsible\">");
                            foreach (var line in frame.PostContextCode)
                            {
                                builder.Append($"<li><span>{HtmlEncoder.Default.Encode(line)}</span></li>");
                            }
                            builder.Append("</ol>");
                        }
                        builder.Append($"</div>");
                    }

                    builder.Append($"</li>");
                }
                builder.Append($"</ul>");
                builder.Append($"</li>");
                builder.Append($"<li>");
                builder.Append("<br/>");
                builder.Append($"<div class=\"rawExceptionBlock\">");
                builder.Append($" <div class=\"showRawExceptionContainer\">");
                builder.Append($"<button class=\"showRawException\" data-exceptionDetailId=\"{HtmlEncoder.Default.Encode(exceptionDetailId)}\">Show raw exception details</button>");
                builder.Append($"</div>");
                builder.Append($"<div id=\"{HtmlEncoder.Default.Encode(exceptionDetailId)}\" class=\"rawExceptionDetails\">");
                builder.Append($"<pre class=\"rawExceptionStackTrace\">{HtmlEncoder.Default.Encode(errorDetail.Error.ToString())}</pre>");
                builder.Append($"</div>");
                builder.Append($"</div>");
                builder.Append($"</li>");
            }
            builder.Append("</ul>");
            builder.Append($"</div>");


            builder.Append("<footer>");
            builder.Append("</footer>");
            NativeMethods.HttpSetStartupErrorPageContent(builder.ToString());
        };
    }

    private static string HtmlEncodeAndReplaceLineBreaks(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // This is horrible, but I don't want to call linq
        var list = new List<string>();
        var arr = input.Split(new[] { "\r\n" }, StringSplitOptions.None);
        foreach (var a in arr)
        {
            var anotherStringArr = a.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            list.AddRange(anotherStringArr);
        }

        return string.Join("<br />" + Environment.NewLine, list);
    }

    public class PhysicalFileProvider : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }

        public IFileInfo GetFileInfo(string contentRoot)
        {
            var fileInfo = new FileInfo(contentRoot);
            return new PhysicalFileInfo(fileInfo);
        }


        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }

        private class PhysicalFileInfo : IFileInfo
        {
            private readonly FileInfo _info;

            public PhysicalFileInfo(FileInfo info)
            {
                _info = info;
            }

            /// <inheritdoc />
            public bool Exists => _info.Exists;

            /// <inheritdoc />
            public long Length => _info.Length;

            /// <inheritdoc />
            public string PhysicalPath => _info.FullName;

            /// <inheritdoc />
            public string Name => _info.Name;

            /// <inheritdoc />
            public DateTimeOffset LastModified => _info.LastWriteTimeUtc;

            /// <summary>
            /// Always false.
            /// </summary>
            public bool IsDirectory => false;
            public Stream CreateReadStream()
            {
                // We are setting buffer size to 1 to prevent FileStream from allocating it's internal buffer
                // 0 causes constructor to throw
                var bufferSize = 1;
                return new FileStream(
                    PhysicalPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
            }
        }
    }
}
