// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.StackTrace.Sources;

namespace Microsoft.AspNetCore.Hosting
{
    internal static class StartupExceptionPage
    {
        private static readonly string _errorPageFormatString = GetResourceString("GenericError.html", escapeBraces: true);
        private static readonly string _errorMessageFormatString = GetResourceString("GenericError_Message.html");
        private static readonly string _errorExceptionFormatString = GetResourceString("GenericError_Exception.html");
        private static readonly string _errorFooterFormatString = GetResourceString("GenericError_Footer.html");

        public static byte[] GenerateErrorHtml(bool showDetails, Exception exception)
        {
            // Build the message for each error
            var builder = new StringBuilder();
            var rawExceptionDetails = new StringBuilder();

            if (!showDetails)
            {
                WriteMessage("An error occurred while starting the application.", builder);
            }
            else
            {
                Debug.Assert(exception != null);
                var wasSourceCodeWrittenOntoPage = false;
                var flattenedExceptions = FlattenAndReverseExceptionTree(exception);
                foreach (var innerEx in flattenedExceptions)
                {
                    WriteException(innerEx, builder, ref wasSourceCodeWrittenOntoPage);
                }

                WriteRawExceptionDetails("Show raw exception details", exception.ToString(), rawExceptionDetails);
            }

            // Generate the footer
            var footer = showDetails ? GenerateFooterEncoded() : null;

            // And generate the full markup
            return Encoding.UTF8.GetBytes(string.Format(CultureInfo.InvariantCulture, _errorPageFormatString, builder, rawExceptionDetails, footer));
        }

        private static string BuildCodeSnippetDiv(StackFrameInfo frameInfo)
        {
            var filename = frameInfo.FilePath;
            if (!string.IsNullOrEmpty(filename))
            {
                int failingLineNumber = frameInfo.LineNumber;
                if (failingLineNumber >= 1)
                {
                    var lines = GetFailingCallSiteInFile(filename, failingLineNumber);
                    if (lines != null)
                    {
                        return @"<div class=""codeSnippet"">"
                            + @"<div class=""filename""><code>" + HtmlEncodeAndReplaceLineBreaks(filename) + "</code></div>" + Environment.NewLine
                            + string.Join(Environment.NewLine, lines) + "</div>" + Environment.NewLine;
                    }
                }
            }

            // fallback
            return null;
        }

        private static string BuildLineForStackFrame(StackFrameInfo frameInfo)
        {
            var builder = new StringBuilder("<pre>");
            var stackFrame = frameInfo.StackFrame;
            var method = stackFrame.GetMethod();
            var displayInfo = frameInfo.MethodDisplayInfo;

            // Special case: no method available
            if (method == null)
            {
                return null;
            }

            // First, write the type name
            var type = method.DeclaringType;
            if (type != null)
            {
                // Special-case ExceptionDispatchInfo.Throw()
                if (type == typeof(ExceptionDispatchInfo) && method.Name == "Throw")
                {
                    return @"<pre><span class=""faded"">--- exception rethrown ---</span></pre>";
                }

                var typeName = displayInfo.DeclaringTypeName;
                // Separate the namespace component from the type name so that we can format it differently.
                if (!string.IsNullOrEmpty(typeName) && !string.IsNullOrEmpty(type.Namespace))
                {
                    builder.Append($@"<span class=""faded"">at {HtmlEncodeAndReplaceLineBreaks(type.Namespace)}</span>");
                    typeName = typeName.Substring(type.Namespace.Length + 1);
                }

                builder.Append(HtmlEncodeAndReplaceLineBreaks(typeName));
            }

            // Next, write the method signature
            builder.Append(displayInfo.Name);

            // Build method parameters
            var methodParameters = "(" + string.Join(", ", displayInfo.Parameters) + ")";
            builder.Append($@"<span class=""faded"">{HtmlEncodeAndReplaceLineBreaks(methodParameters)}</span>");

            // Do we have source information for this frame?
            if (stackFrame.GetILOffset() != -1)
            {
                var filename = frameInfo.FilePath;
                if (!string.IsNullOrEmpty(filename))
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, " in {0}:line {1:D}", HtmlEncodeAndReplaceLineBreaks(filename), frameInfo.LineNumber);
                }
            }

            // Finish
            builder.Append("</pre>");
            return builder.ToString();
        }

        private static string GetResourceString(string name, bool escapeBraces = false)
        {
            // '{' and '}' are special in CSS, so we use "[[[0]]]" instead for {0} (and so on).
            var assembly = typeof(StartupExceptionPage).GetTypeInfo().Assembly;
            var resourceName = assembly.GetName().Name + ".compiler.resources." + name;
            var manifestStream = assembly.GetManifestResourceStream(resourceName);
            var formatString = new StreamReader(manifestStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false).ReadToEnd();
            if (escapeBraces)
            {
                formatString = formatString.Replace("{", "{{").Replace("}", "}}").Replace("[[[", "{").Replace("]]]", "}");
            }

            return formatString;
        }

        private static List<string> GetFailingCallSiteInFile(string filename, int failedLineNumber)
        {
            // We figure out the [first, last] range of lines to read from the file.
            var firstLineNumber = failedLineNumber - 2;
            firstLineNumber = Math.Max(1, firstLineNumber);
            var lastLineNumber = failedLineNumber + 2;
            lastLineNumber = Math.Max(lastLineNumber, failedLineNumber);

            // Figure out how many characters lastLineNumber will take to print.
            var lastLineNumberCharLength = lastLineNumber.ToString("D", CultureInfo.InvariantCulture).Length;

            var errorSubContents = new List<string>();
            var didReadFailingLine = false;

            try
            {
                var thisLineNum = 0;
                foreach (var line in File.ReadLines(filename))
                {
                    thisLineNum++;

                    // Are we within the correct range?
                    if (thisLineNum < firstLineNumber)
                    {
                        continue;
                    }
                    if (thisLineNum > lastLineNumber)
                    {
                        break;
                    }

                    var encodedLine = HtmlEncodeAndReplaceLineBreaks("Line "
                        + thisLineNum.ToString("D", CultureInfo.InvariantCulture).PadLeft(lastLineNumberCharLength)
                        + ":  "
                        + line);

                    if (thisLineNum == failedLineNumber)
                    {
                        didReadFailingLine = true;
                        errorSubContents.Add(@"<div class=""line error""><code>" + encodedLine + "</code></div>");
                    }
                    else
                    {
                        errorSubContents.Add(@"<div class=""line""><code>" + encodedLine + "</code></div>");
                    }
                }
            }
            catch
            {
                // If there's an error for any reason, don't show source.
                return null;
            }

            return (didReadFailingLine) ? errorSubContents : null;
        }

        private static string GenerateFooterEncoded()
        {
            var runtimeType = HtmlEncodeAndReplaceLineBreaks(Microsoft.Extensions.Internal.RuntimeEnvironment.RuntimeType);
            var runtimeDisplayName = runtimeType == "CoreCLR" ? ".NET Core" : runtimeType == "CLR" ? ".NET Framework" : "Mono";
#if NETSTANDARD1_3
            var systemRuntimeAssembly = typeof(System.ComponentModel.DefaultValueAttribute).GetTypeInfo().Assembly;
            var assemblyVersion = new AssemblyName(systemRuntimeAssembly.FullName).Version.ToString();
            var clrVersion = HtmlEncodeAndReplaceLineBreaks(assemblyVersion);
#else
            var clrVersion = HtmlEncodeAndReplaceLineBreaks(Environment.Version.ToString());
#endif
            var runtimeArch = HtmlEncodeAndReplaceLineBreaks(RuntimeInformation.ProcessArchitecture.ToString());
            var currentAssembly = typeof(StartupExceptionPage).GetTypeInfo().Assembly;
            var currentAssemblyVersion = currentAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            currentAssemblyVersion = HtmlEncodeAndReplaceLineBreaks(currentAssemblyVersion);

            var osDescription = HtmlEncodeAndReplaceLineBreaks(RuntimeInformation.OSDescription);

            return string.Format(CultureInfo.InvariantCulture, _errorFooterFormatString, runtimeDisplayName, runtimeArch, clrVersion,
                currentAssemblyVersion, osDescription);
        }

        private static string HtmlEncodeAndReplaceLineBreaks(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Split on line breaks before passing it through the encoder.
            // We use the static default encoder since we can't depend on DI in the error handling logic.
            return string.Join("<br />" + Environment.NewLine,
                input.Split(new[] { "\r\n" }, StringSplitOptions.None)
                .SelectMany(s => s.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
                .Select(HtmlEncoder.Default.Encode));
        }

        private static void WriteException(Exception ex, StringBuilder builder, ref bool wasFailingCallSiteSourceWritten)
        {
            string inlineSourceDiv = null;

            // First, build the stack trace
            var firstStackFrame = true;
            var stackTraceBuilder = new StringBuilder();
            foreach (var frameInfo in StackTraceHelper.GetFrames(ex))
            {
                if (!firstStackFrame)
                {
                    stackTraceBuilder.Append("<br />");
                }
                firstStackFrame = false;
                var thisFrameLine = BuildLineForStackFrame(frameInfo);
                stackTraceBuilder.AppendLine(thisFrameLine);

                // Try to include the source code in the error page if we can.
                if (!wasFailingCallSiteSourceWritten && inlineSourceDiv == null)
                {
                    inlineSourceDiv = BuildCodeSnippetDiv(frameInfo);
                    if (inlineSourceDiv != null)
                    {
                        wasFailingCallSiteSourceWritten = true;
                    }
                }
            }

            // Finally, build the rest of the <div>
            builder.AppendFormat(CultureInfo.InvariantCulture, _errorExceptionFormatString,
                HtmlEncodeAndReplaceLineBreaks(ex.GetType().FullName),
                HtmlEncodeAndReplaceLineBreaks(ex.Message),
                inlineSourceDiv,
                stackTraceBuilder);
        }

        private static void WriteRawExceptionDetails(string linkText, string line, StringBuilder rawExceptionDetails)
        {
            rawExceptionDetails
                .AppendLine("<div class=\"rawExceptionBlock\">")
                .AppendFormat($"    <div><a href=\"#\" onclick=\"javascript: showRawException(); return false;\">{linkText}</a></div>")
                .AppendLine()
                .AppendLine("    <div id=\"rawException\">")
                .Append("        <pre>");

            rawExceptionDetails.AppendLine(line);

            rawExceptionDetails
                .AppendLine("</pre>")
                .AppendLine("    </div>")
                .AppendLine("</div>");
        }

        private static void WriteMessage(string message, StringBuilder builder)
        {
            // Build the <div>
            builder.AppendFormat(CultureInfo.InvariantCulture, _errorMessageFormatString,
                HtmlEncodeAndReplaceLineBreaks(message));
        }

        private static IEnumerable<Exception> FlattenAndReverseExceptionTree(Exception ex)
        {
            // ReflectionTypeLoadException is special because the details are in 
            // a the LoaderExceptions property
            var typeLoadException = ex as ReflectionTypeLoadException;
            if (typeLoadException != null)
            {
                var typeLoadExceptions = new List<Exception>();
                foreach (Exception loadException in typeLoadException.LoaderExceptions)
                {
                    typeLoadExceptions.AddRange(FlattenAndReverseExceptionTree(loadException));
                }

                typeLoadExceptions.Add(ex);
                return typeLoadExceptions;
            }

            var list = new List<Exception>();
            while (ex != null)
            {
                list.Add(ex);
                ex = ex.InnerException;
            }
            list.Reverse();
            return list;
        }
    }
}
