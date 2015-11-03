// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Compilation;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Hosting.Startup
{
    internal static class StartupExceptionPage
    {
        private const int MaxCompilationErrorsToShow = 20;

        private static readonly string _errorPageFormatString = GetResourceString("GenericError.html", escapeBraces: true);
        private static readonly string _errorMessageFormatString = GetResourceString("GenericError_Message.html");
        private static readonly string _errorExceptionFormatString = GetResourceString("GenericError_Exception.html");
        private static readonly string _errorFooterFormatString = GetResourceString("GenericError_Footer.html");
        private static readonly string _compilationExceptionFormatString = GetResourceString("Compilation_Exception.html");

        public static byte[] GenerateErrorHtml(bool showDetails, IRuntimeEnvironment runtimeEnvironment, params object[] errorDetails)
        {
            if (!showDetails)
            {
                errorDetails = new[] { "An error occurred while starting the application." };
            }

            // Build the message for each error
            var wasSourceCodeWrittenOntoPage = false;
            var builder = new StringBuilder();
            var rawExceptionDetails = new StringBuilder();

            foreach (object error in errorDetails ?? new object[0])
            {
                var ex = error as Exception;
                if (ex == null && error is ExceptionDispatchInfo)
                {
                    ex = ((ExceptionDispatchInfo)error).SourceException;
                }

                if (ex != null)
                {
                    var flattenedExceptions = FlattenAndReverseExceptionTree(ex);

                    var compilationException = flattenedExceptions.OfType<ICompilationException>()
                                                                  .FirstOrDefault();
                    if (compilationException != null)
                    {
                        WriteException(compilationException, builder, ref wasSourceCodeWrittenOntoPage);

                        var compilationErrorMessages = compilationException.CompilationFailures
                            .SelectMany(f => f.Messages.Select(m => m.FormattedMessage))
                            .Take(MaxCompilationErrorsToShow);

                        WriteRawExceptionDetails("Show raw compilation error details", compilationErrorMessages, rawExceptionDetails);
                    }
                    else
                    {
                        foreach (var innerEx in flattenedExceptions)
                        {
                            WriteException(innerEx, builder, ref wasSourceCodeWrittenOntoPage);
                        }

                        WriteRawExceptionDetails("Show raw exception details", new[] { ex.ToString() }, rawExceptionDetails);
                    }
                }
                else
                {
                    var message = Convert.ToString(error, CultureInfo.InvariantCulture);
                    WriteMessage(message, builder);
                }
            }

            // Generate the footer
            var footer = showDetails ? GenerateFooterEncoded(runtimeEnvironment) : null;

            // And generate the full markup
            return Encoding.UTF8.GetBytes(string.Format(CultureInfo.InvariantCulture, _errorPageFormatString, builder, rawExceptionDetails, footer));
        }

        private static string BuildCodeSnippetDiv(StackFrame frame)
        {
            var filename = frame.GetFileName();
            if (!string.IsNullOrEmpty(filename))
            {
                int failingLineNumber = frame.GetFileLineNumber();
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

        private static string BuildLineForStackFrame(StackFrame frame)
        {
            var builder = new StringBuilder("<pre>");
            var method = frame.GetMethod();

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

                string prefix, friendlyName;
                SplitTypeIntoPrefixAndFriendlyName(type, out prefix, out friendlyName);
                builder.AppendFormat(CultureInfo.InvariantCulture, @"<span class=""faded"">at {0}</span>", HtmlEncodeAndReplaceLineBreaks(prefix));
                builder.Append(HtmlEncodeAndReplaceLineBreaks(friendlyName));
            }

            // Next, write the method signature
            builder.Append(HtmlEncodeAndReplaceLineBreaks("." + method.Name));

            // Is this method generic?
            if (method.IsGenericMethod)
            {
                builder.Append(HtmlEncodeAndReplaceLineBreaks(BuildMethodGenericParametersUnescaped(method)));
            }

            // Build method parameters
            builder.AppendFormat(CultureInfo.InvariantCulture, @"<span class=""faded"">{0}</span>", HtmlEncodeAndReplaceLineBreaks(BuildMethodParametersUnescaped(method)));

            // Do we have source information for this frame?
            if (frame.GetILOffset() != -1)
            {
                var filename = frame.GetFileName();
                if (!string.IsNullOrEmpty(filename))
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, " in {0}:line {1:D}", HtmlEncodeAndReplaceLineBreaks(filename), frame.GetFileLineNumber());
                }
            }

            // Finish
            builder.Append("</pre>");
            return builder.ToString();
        }

        private static string BuildMethodGenericParametersUnescaped(MethodBase method)
        {
            Debug.Assert(method.IsGenericMethod);
            return "<" + string.Join(", ", method.GetGenericArguments().Select(PrettyPrintTypeName)) + ">";
        }

        private static string BuildMethodParametersUnescaped(MethodBase method)
        {
            return "(" + string.Join(", ", method.GetParameters().Select(p => {
                Type parameterType = p.ParameterType;
                return ((parameterType != null) ? PrettyPrintTypeName(parameterType) : "?") + " " + p.Name;
            })) + ")";
        }

        private static void BuildCodeSnippetDiv(CompilationFailure failure,
                                               StringBuilder builder,
                                               ref int totalErrorsShown)
        {
            const int NumContextLines = 3;
            var fileName = failure.SourceFilePath;
            if (totalErrorsShown < MaxCompilationErrorsToShow &&
                !string.IsNullOrEmpty(fileName))
            {
                builder.Append(@"<div class=""codeSnippet"">")
                       .AppendFormat(@"<div class=""filename""><code>{0}</code></div>", HtmlEncodeAndReplaceLineBreaks(fileName))
                       .AppendLine();

                IEnumerable<string> fileContent;
                if (string.IsNullOrEmpty(failure.SourceFileContent))
                {
                    fileContent = File.ReadLines(fileName);
                }
                else
                {
                    fileContent = failure.SourceFileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                }
                foreach (var message in failure.Messages)
                {
                    if (totalErrorsShown++ > MaxCompilationErrorsToShow)
                    {
                        break;
                    }

                    if (totalErrorsShown > 1)
                    {
                        builder.AppendLine("<br />");
                    }

                    builder.Append(@"<div class=""error-message"">")
                           .Append(HtmlEncodeAndReplaceLineBreaks(message.Message))
                           .Append("</div>");

                    // StartLine and EndLine are 1-based
                    var startLine = message.StartLine - 1;
                    var endLine = message.EndLine - 1;
                    var preContextIndex = Math.Max(startLine - NumContextLines, 0);
                    var index = preContextIndex + 1;
                    foreach (var line in fileContent.Skip(preContextIndex).Take(startLine - preContextIndex))
                    {
                        builder.Append(@"<div class=""line faded"">")
                               .AppendFormat(@"<span class=""line-number"">{0}</span><code>", index++)
                               .Append(HtmlEncodeAndReplaceLineBreaks(line))
                               .AppendLine("</code></div>");
                    }

                    var numErrorLines = 1 + Math.Max(0, endLine - startLine);
                    foreach (var line in fileContent.Skip(startLine).Take(numErrorLines))
                    {
                        builder.Append(@"<div class=""line error"">")
                               .AppendFormat(@"<span class=""line-number"">{0}</span><code>", index++)
                               .Append(HtmlEncodeAndReplaceLineBreaks(line))
                               .AppendLine("</code></div>");
                    }
                    foreach (var line in fileContent.Skip(message.EndLine).Take(NumContextLines))
                    {
                        builder.Append(@"<div class=""line faded"">")
                               .AppendFormat(@"<span class=""line-number"">{0}</span><code>", index++)
                               .Append(HtmlEncodeAndReplaceLineBreaks(line))
                               .AppendLine("</code></div>");
                    }
                }

                builder.AppendLine("</div>"); // Close codeSnippet div
            }
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

        private static string PrettyPrintTypeName(Type t)
        {
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();

                var name = t.Name;

                // Degenerate case
                if (string.IsNullOrEmpty(name))
                {
                    name = "?";
                }

                // Handle generic types
                if (t.GetTypeInfo().IsGenericType)
                {
                    // strip off the CLR generic type marker if it exists
                    var indexOfGenericTypeMarker = name.LastIndexOf('`');
                    if (indexOfGenericTypeMarker >= 0)
                    {
                        name = name.Substring(0, indexOfGenericTypeMarker);
                        name += "<" + string.Join(", ", t.GetGenericArguments().Select(PrettyPrintTypeName)) + ">";
                    }
                }

                // Handle nested types
                if (!t.IsGenericParameter)
                {
                    var containerType = t.DeclaringType;
                    if (containerType != null)
                    {
                        name = PrettyPrintTypeName(containerType) + "." + name;
                    }
                }

                return name;
            }
            catch
            {
                // If anything at all goes wrong, fall back to the full type name so that we don't crash the server.
                return t.FullName;
            }
        }

        private static void SplitTypeIntoPrefixAndFriendlyName(Type type, out string prefix, out string friendlyName)
        {
            prefix = type.Namespace;
            friendlyName = PrettyPrintTypeName(type);

            if (!string.IsNullOrEmpty(friendlyName) && !string.IsNullOrEmpty(prefix))
            {
                prefix += ".";
            }
        }

        private static string GenerateFooterEncoded(IRuntimeEnvironment environment)
        {
            var runtimeType = HtmlEncodeAndReplaceLineBreaks(environment.RuntimeType);
#if DNXCORE50 || DOTNET5_4
            var systemRuntimeAssembly = typeof(System.ComponentModel.DefaultValueAttribute).GetTypeInfo().Assembly;
            var assemblyVersion = new AssemblyName(systemRuntimeAssembly.FullName).Version.ToString();
            var clrVersion = HtmlEncodeAndReplaceLineBreaks(assemblyVersion);
#else
            var clrVersion = HtmlEncodeAndReplaceLineBreaks(Environment.Version.ToString());
#endif
            var runtimeArch = HtmlEncodeAndReplaceLineBreaks(environment.RuntimeArchitecture);
            var dnxVersion = HtmlEncodeAndReplaceLineBreaks(environment.RuntimeVersion);
            var currentAssembly = typeof(StartupExceptionPage).GetTypeInfo().Assembly;
            var currentAssemblyVersion = currentAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            currentAssemblyVersion = HtmlEncodeAndReplaceLineBreaks(currentAssemblyVersion);

            var os = HtmlEncodeAndReplaceLineBreaks(environment.OperatingSystem);
            var osVersion = HtmlEncodeAndReplaceLineBreaks(environment.OperatingSystemVersion);

            return string.Format(CultureInfo.InvariantCulture, _errorFooterFormatString, runtimeType, clrVersion,
                runtimeArch, dnxVersion, currentAssemblyVersion, os, osVersion);
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
            var needFileInfo = true;
            foreach (var frame in new StackTrace(ex, needFileInfo).GetFrames() ?? Enumerable.Empty<StackFrame>())
            {
                if (!firstStackFrame)
                {
                    stackTraceBuilder.Append("<br />");
                }
                firstStackFrame = false;
                var thisFrameLine = BuildLineForStackFrame(frame);
                stackTraceBuilder.AppendLine(thisFrameLine);

                // Try to include the source code in the error page if we can.
                if (!wasFailingCallSiteSourceWritten && inlineSourceDiv == null)
                {
                    inlineSourceDiv = BuildCodeSnippetDiv(frame);
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

        private static void WriteRawExceptionDetails(string linkText, IEnumerable<string> lines, StringBuilder rawExceptionDetails)
        {
            rawExceptionDetails
                .AppendLine("<div class=\"rawExceptionBlock\">")
                .AppendFormat($"    <div><a href=\"#\" onclick=\"javascript: showRawException(); return false;\">{linkText}</a></div>")
                .AppendLine()
                .AppendLine("    <div id=\"rawException\">")
                .Append("        <pre>");

            foreach (var line in lines)
            {
                rawExceptionDetails.AppendLine(line);
            }

            rawExceptionDetails
                .AppendLine("</pre>")
                .AppendLine("    </div>")
                .AppendLine("</div>");
        }

        private static void WriteException(ICompilationException compilationException,
                                           StringBuilder builder,
                                           ref bool wasSourceCodeWrittenOntoPage)
        {
            var totalErrorsShown = 0;
            var inlineSourceDiv = new StringBuilder();
            var firstStackFrame = true;
            foreach (var failure in compilationException.CompilationFailures)
            {
                if (firstStackFrame)
                {
                    firstStackFrame = false;
                }
                else
                {
                    inlineSourceDiv.AppendLine("<br/>");
                }

                BuildCodeSnippetDiv(failure, inlineSourceDiv, ref totalErrorsShown);
            }

            wasSourceCodeWrittenOntoPage = totalErrorsShown > 0;

            builder.AppendFormat(CultureInfo.InvariantCulture,
                                 _compilationExceptionFormatString,
                                 inlineSourceDiv);
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
