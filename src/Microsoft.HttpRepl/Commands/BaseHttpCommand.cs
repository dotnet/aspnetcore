// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.HttpRepl.Formatting;
using Microsoft.HttpRepl.Preferences;
using Microsoft.HttpRepl.Suggestions;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Parsing;
using Microsoft.Repl.Suggestions;
using Newtonsoft.Json.Linq;

namespace Microsoft.HttpRepl.Commands
{
    public abstract class BaseHttpCommand : CommandWithStructuredInputBase<HttpState, ICoreParseResult>
    {
        private const string HeaderOption = nameof(HeaderOption);
        private const string ResponseHeadersFileOption = nameof(ResponseHeadersFileOption);
        private const string ResponseBodyFileOption = nameof(ResponseBodyFileOption);
        private const string ResponseFileOption = nameof(ResponseFileOption);
        private const string BodyFileOption = nameof(BodyFileOption);
        private const string NoBodyOption = nameof(NoBodyOption);
        private const string NoFormattingOption = nameof(NoFormattingOption);
        private const string StreamingOption = nameof(StreamingOption);
        private const string BodyContentOption = nameof(BodyContentOption);
        private static readonly char[] HeaderSeparatorChars = new[] { '=', ':' };

        private CommandInputSpecification _inputSpec;

        protected abstract string Verb { get; }

        protected abstract bool RequiresBody { get; }

        public override CommandInputSpecification InputSpec
        {
            get
            {
                if (_inputSpec != null)
                {
                    return _inputSpec;
                }

                CommandInputSpecificationBuilder builder = CommandInputSpecification.Create(Verb)
                    .MaximumArgCount(1)
                    .WithOption(new CommandOptionSpecification(HeaderOption, requiresValue: true, forms: new[] {"--header", "-h"}))
                    .WithOption(new CommandOptionSpecification(ResponseFileOption, requiresValue: true, maximumOccurrences: 1, forms: new[] { "--response", }))
                    .WithOption(new CommandOptionSpecification(ResponseHeadersFileOption, requiresValue: true, maximumOccurrences: 1, forms: new[] { "--response:headers", }))
                    .WithOption(new CommandOptionSpecification(ResponseBodyFileOption, requiresValue: true, maximumOccurrences: 1, forms: new[] { "--response:body", }))
                    .WithOption(new CommandOptionSpecification(NoFormattingOption, maximumOccurrences: 1, forms: new[] { "--no-formatting", "-F" }))
                    .WithOption(new CommandOptionSpecification(StreamingOption, maximumOccurrences: 1, forms: new[] { "--streaming", "-s" }));

                if (RequiresBody)
                {
                    builder = builder.WithOption(new CommandOptionSpecification(NoBodyOption, maximumOccurrences: 1, forms: "--no-body"))
                        .WithOption(new CommandOptionSpecification(BodyFileOption, requiresValue: true, maximumOccurrences: 1, forms: new[] {"--file", "-f"}))
                        .WithOption(new CommandOptionSpecification(BodyContentOption, requiresValue: true, maximumOccurrences: 1, forms: new[] {"--content", "-c"}));
                }

                _inputSpec = builder.Finish();
                return _inputSpec;
            }
        }

        protected override async Task ExecuteAsync(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (programState.BaseAddress == null && (commandInput.Arguments.Count == 0 || !Uri.TryCreate(commandInput.Arguments[0].Text, UriKind.Absolute, out Uri _)))
            {
                shellState.ConsoleManager.Error.WriteLine("'set base {url}' must be called before issuing requests to a relative path".SetColor(programState.ErrorColor));
                return;
            }

            if (programState.SwaggerEndpoint != null)
            {
                string swaggerRequeryBehaviorSetting = programState.GetStringPreference(WellKnownPreference.SwaggerRequeryBehavior, "auto");

                if (swaggerRequeryBehaviorSetting.StartsWith("auto", StringComparison.OrdinalIgnoreCase))
                {
                    await SetSwaggerCommand.CreateDirectoryStructureForSwaggerEndpointAsync(shellState, programState, programState.SwaggerEndpoint, cancellationToken).ConfigureAwait(false);
                }
            }

            Dictionary<string, string> thisRequestHeaders = new Dictionary<string, string>();

            foreach (InputElement header in commandInput.Options[HeaderOption])
            {
                int equalsIndex = header.Text.IndexOfAny(HeaderSeparatorChars);

                if (equalsIndex < 0)
                {
                    shellState.ConsoleManager.Error.WriteLine("Headers must be formatted as {header}={value} or {header}:{value}".SetColor(programState.ErrorColor));
                    return;
                }

                thisRequestHeaders[header.Text.Substring(0, equalsIndex)] = header.Text.Substring(equalsIndex + 1);
            }

            Uri effectivePath = programState.GetEffectivePath(commandInput.Arguments.Count > 0 ? commandInput.Arguments[0].Text : string.Empty);
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(Verb.ToUpperInvariant()), effectivePath);
            bool noBody = false;

            if (RequiresBody)
            {
                string filePath = null;
                string bodyContent = null;
                bool deleteFile = false;
                noBody = commandInput.Options[NoBodyOption].Count > 0;

                if (!thisRequestHeaders.TryGetValue("content-type", out string contentType) && programState.Headers.TryGetValue("content-type", out IEnumerable<string> contentTypes))
                {
                    contentType = contentTypes.FirstOrDefault();
                }

                if (!noBody)
                {
                    if (string.IsNullOrEmpty(contentType))
                    {
                        contentType = "application/json";
                    }

                    if (commandInput.Options[BodyFileOption].Count > 0)
                    {
                        filePath = commandInput.Options[BodyFileOption][0].Text;

                        if (!File.Exists(filePath))
                        {
                            shellState.ConsoleManager.Error.WriteLine($"Content file {filePath} does not exist".SetColor(programState.ErrorColor));
                            return;
                        }
                    }
                    else if (commandInput.Options[BodyContentOption].Count > 0)
                    {
                        bodyContent = commandInput.Options[BodyContentOption][0].Text;
                    }
                    else
                    {
                        string defaultEditorCommand = programState.GetStringPreference(WellKnownPreference.DefaultEditorCommand);
                        if (defaultEditorCommand == null)
                        {
                            shellState.ConsoleManager.Error.WriteLine($"The default editor must be configured using the command `pref set {WellKnownPreference.DefaultEditorCommand} \"{{commandline}}\"`".SetColor(programState.ErrorColor));
                            return;
                        }

                        deleteFile = true;
                        filePath = Path.GetTempFileName();

                        string exampleBody = programState.GetExampleBody(commandInput.Arguments.Count > 0 ? commandInput.Arguments[0].Text : string.Empty, ref contentType, Verb);

                        if (!string.IsNullOrEmpty(exampleBody))
                        {
                            File.WriteAllText(filePath, exampleBody);
                        }

                        string defaultEditorArguments = programState.GetStringPreference(WellKnownPreference.DefaultEditorArguments) ?? "";
                        string original = defaultEditorArguments;
                        string pathString = $"\"{filePath}\"";

                        defaultEditorArguments = defaultEditorArguments.Replace("{filename}", pathString);

                        if (string.Equals(defaultEditorArguments, original, StringComparison.Ordinal))
                        {
                            defaultEditorArguments = (defaultEditorArguments + " " + pathString).Trim();
                        }

                        ProcessStartInfo info = new ProcessStartInfo(defaultEditorCommand, defaultEditorArguments);

                        Process.Start(info)?.WaitForExit();
                    }
                }

                if (string.IsNullOrEmpty(contentType))
                {
                    contentType = "application/json";
                }

                byte[] data = noBody 
                    ? new byte[0] 
                    : string.IsNullOrEmpty(bodyContent) 
                        ? File.ReadAllBytes(filePath) 
                        : Encoding.UTF8.GetBytes(bodyContent);

                HttpContent content = new ByteArrayContent(data);
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                request.Content = content;

                if (deleteFile)
                {
                    File.Delete(filePath);
                }

                foreach (KeyValuePair<string, IEnumerable<string>> header in programState.Headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                foreach (KeyValuePair<string, string> header in thisRequestHeaders)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in programState.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (KeyValuePair<string, string> header in thisRequestHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            string headersTarget = commandInput.Options[ResponseHeadersFileOption].FirstOrDefault()?.Text ?? commandInput.Options[ResponseFileOption].FirstOrDefault()?.Text;
            string bodyTarget = commandInput.Options[ResponseBodyFileOption].FirstOrDefault()?.Text ?? commandInput.Options[ResponseFileOption].FirstOrDefault()?.Text;

            HttpResponseMessage response = await programState.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            await HandleResponseAsync(programState, commandInput, shellState.ConsoleManager, response, programState.EchoRequest, headersTarget, bodyTarget, cancellationToken).ConfigureAwait(false);
        }

        private static async Task HandleResponseAsync(HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, IConsoleManager consoleManager, HttpResponseMessage response, bool echoRequest, string headersTargetFile, string bodyTargetFile, CancellationToken cancellationToken)
        {
            RequestConfig requestConfig = new RequestConfig(programState);
            ResponseConfig responseConfig = new ResponseConfig(programState);
            string protocolInfo;

            if (echoRequest)
            {
                string hostString = response.RequestMessage.RequestUri.Scheme + "://" + response.RequestMessage.RequestUri.Host + (!response.RequestMessage.RequestUri.IsDefaultPort ? ":" + response.RequestMessage.RequestUri.Port : "");
                consoleManager.WriteLine($"Request to {hostString}...".SetColor(requestConfig.AddressColor));
                consoleManager.WriteLine();

                string method = response.RequestMessage.Method.ToString().ToUpperInvariant().SetColor(requestConfig.MethodColor);
                string pathAndQuery = response.RequestMessage.RequestUri.PathAndQuery.SetColor(requestConfig.AddressColor);
                protocolInfo = $"{"HTTP".SetColor(requestConfig.ProtocolNameColor)}{"/".SetColor(requestConfig.ProtocolSeparatorColor)}{response.Version.ToString().SetColor(requestConfig.ProtocolVersionColor)}";

                consoleManager.WriteLine($"{method} {pathAndQuery} {protocolInfo}");
                IEnumerable<KeyValuePair<string, IEnumerable<string>>> requestHeaders = response.RequestMessage.Headers;

                if (response.RequestMessage.Content != null)
                {
                    requestHeaders = requestHeaders.Union(response.RequestMessage.Content.Headers);
                }

                foreach (KeyValuePair<string, IEnumerable<string>> header in requestHeaders.OrderBy(x => x.Key))
                {
                    string headerKey = header.Key.SetColor(requestConfig.HeaderKeyColor);
                    string headerSep = ":".SetColor(requestConfig.HeaderSeparatorColor);
                    string headerValue = string.Join(";".SetColor(requestConfig.HeaderValueSeparatorColor), header.Value.Select(x => x.Trim().SetColor(requestConfig.HeaderValueColor)));
                    consoleManager.WriteLine($"{headerKey}{headerSep} {headerValue}");
                }

                consoleManager.WriteLine();

                if (response.RequestMessage.Content != null)
                {
                    using (StreamWriter writer = new StreamWriter(new MemoryStream()))
                    {
                        await FormatBodyAsync(commandInput, programState, consoleManager, response.RequestMessage.Content, writer, cancellationToken).ConfigureAwait(false);
                    }
                }

                consoleManager.WriteLine();
                consoleManager.WriteLine($"Response from {hostString}...".SetColor(requestConfig.AddressColor));
                consoleManager.WriteLine();
            }

            protocolInfo = $"{"HTTP".SetColor(responseConfig.ProtocolNameColor)}{"/".SetColor(responseConfig.ProtocolSeparatorColor)}{response.Version.ToString().SetColor(responseConfig.ProtocolVersionColor)}";
            string status = ((int)response.StatusCode).ToString().SetColor(responseConfig.StatusCodeColor) + " " + response.ReasonPhrase.SetColor(responseConfig.StatusReasonPhraseColor);

            consoleManager.WriteLine($"{protocolInfo} {status}");

            IEnumerable<KeyValuePair<string, IEnumerable<string>>> responseHeaders = response.Headers;

            if (response.Content != null)
            {
                responseHeaders = responseHeaders.Union(response.Content.Headers);
            }

            StreamWriter headerFileWriter;

            if (headersTargetFile != null)
            {
                headerFileWriter = new StreamWriter(File.Create(headersTargetFile));
            }
            else
            {
                headerFileWriter = new StreamWriter(new MemoryStream());
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in responseHeaders.OrderBy(x => x.Key))
            {
                string headerKey = header.Key.SetColor(responseConfig.HeaderKeyColor);
                string headerSep = ":".SetColor(responseConfig.HeaderSeparatorColor);
                string headerValue = string.Join(";".SetColor(responseConfig.HeaderValueSeparatorColor), header.Value.Select(x => x.Trim().SetColor(responseConfig.HeaderValueColor)));
                consoleManager.WriteLine($"{headerKey}{headerSep} {headerValue}");
                headerFileWriter.WriteLine($"{header.Key}: {string.Join(";", header.Value.Select(x => x.Trim()))}");
            }

            StreamWriter bodyFileWriter;
            if (!string.Equals(headersTargetFile, bodyTargetFile, StringComparison.Ordinal))
            {
                headerFileWriter.Flush();
                headerFileWriter.Close();
                headerFileWriter.Dispose();

                if (bodyTargetFile != null)
                {
                    bodyFileWriter = new StreamWriter(File.Create(bodyTargetFile));
                }
                else
                {
                    bodyFileWriter = new StreamWriter(new MemoryStream());
                }
            }
            else
            {
                headerFileWriter.WriteLine();
                bodyFileWriter = headerFileWriter;
            }

            consoleManager.WriteLine();

            if (response.Content != null)
            {
                await FormatBodyAsync(commandInput, programState, consoleManager, response.Content, bodyFileWriter, cancellationToken).ConfigureAwait(false);
            }

            bodyFileWriter.Flush();
            bodyFileWriter.Close();
            bodyFileWriter.Dispose();

            consoleManager.WriteLine();
        }

        private static async Task FormatBodyAsync(DefaultCommandInput<ICoreParseResult> commandInput, HttpState programState, IConsoleManager consoleManager, HttpContent content, StreamWriter bodyFileWriter, CancellationToken cancellationToken)
        {
            if (commandInput.Options[StreamingOption].Count > 0)
            {
                Memory<char> buffer = new Memory<char>(new char[2048]);
                Stream s = await content.ReadAsStreamAsync().ConfigureAwait(false);
                StreamReader reader = new StreamReader(s);
                consoleManager.WriteLine("Streaming the response, press any key to stop...".SetColor(programState.WarningColor));

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        ValueTask<int> readTask = reader.ReadAsync(buffer, cancellationToken);
                        if (await WaitForCompletionAsync(readTask, cancellationToken).ConfigureAwait(false))
                        {
                            if (readTask.Result == 0)
                            {
                                break;
                            }

                            string str = new string(buffer.Span.Slice(0, readTask.Result));
                            consoleManager.Write(str);
                            bodyFileWriter.Write(str);
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                return;
            }

            string contentType = null;
            if (content.Headers.TryGetValues("Content-Type", out IEnumerable<string> contentTypeValues))
            {
                contentType = contentTypeValues.FirstOrDefault()?.Split(';').FirstOrDefault();
            }

            contentType = contentType?.ToUpperInvariant() ?? "text/plain";

            if (commandInput.Options[NoFormattingOption].Count == 0)
            {
                if (contentType.EndsWith("/JSON", StringComparison.OrdinalIgnoreCase)
                    || contentType.EndsWith("-JSON", StringComparison.OrdinalIgnoreCase)
                    || contentType.EndsWith("+JSON", StringComparison.OrdinalIgnoreCase)
                    || contentType.EndsWith("/JAVASCRIPT", StringComparison.OrdinalIgnoreCase)
                    || contentType.EndsWith("-JAVASCRIPT", StringComparison.OrdinalIgnoreCase)
                    || contentType.EndsWith("+JAVASCRIPT", StringComparison.OrdinalIgnoreCase))
                {
                    if (await FormatJsonAsync(programState, consoleManager, content, bodyFileWriter))
                    {
                        return;
                    }
                }
                else if (contentType.EndsWith("/HTML", StringComparison.OrdinalIgnoreCase)
                    || contentType.EndsWith("-HTML", StringComparison.OrdinalIgnoreCase)
                    || contentType.EndsWith("+HTML", StringComparison.OrdinalIgnoreCase)
                    || contentType.EndsWith("/XML", StringComparison.OrdinalIgnoreCase)
                    || contentType.EndsWith("-XML", StringComparison.OrdinalIgnoreCase)
                    || contentType.EndsWith("+XML", StringComparison.OrdinalIgnoreCase))
                {
                    if (await FormatXmlAsync(consoleManager, content, bodyFileWriter))
                    {
                        return;
                    }
                }
            }

            string responseContent = await content.ReadAsStringAsync().ConfigureAwait(false);
            bodyFileWriter.WriteLine(responseContent);
            consoleManager.WriteLine(responseContent);
        }

        private static async Task<bool> WaitForCompletionAsync(ValueTask<int> readTask, CancellationToken cancellationToken)
        {
            while (!readTask.IsCompleted && !cancellationToken.IsCancellationRequested && !Console.KeyAvailable)
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }

            if (Console.KeyAvailable)
            {
                Console.ReadKey(false);
                return false;
            }

            return readTask.IsCompleted;
        }

        private static async Task<bool> FormatXmlAsync(IWritable consoleManager, HttpContent content, StreamWriter bodyFileWriter)
        {
            string responseContent = await content.ReadAsStringAsync().ConfigureAwait(false);
            try
            {
                XDocument body = XDocument.Parse(responseContent);
                consoleManager.WriteLine(body.ToString());
                bodyFileWriter.WriteLine(body.ToString());
                return true;
            }
            catch
            {
            }

            return false;
        }

        private static async Task<bool> FormatJsonAsync(HttpState programState, IWritable outputSink, HttpContent content, StreamWriter bodyFileWriter)
        {
            string responseContent = await content.ReadAsStringAsync().ConfigureAwait(false);

            try
            {
                JsonConfig config = new JsonConfig(programState);
                string formatted = JsonVisitor.FormatAndColorize(config, responseContent);
                outputSink.WriteLine(formatted);
                bodyFileWriter.WriteLine(JToken.Parse(responseContent).ToString());
                return true;
            }
            catch
            {
            }

            return false;
        }

        protected override string GetHelpDetails(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult)
        {
            var helpText = new StringBuilder();
            helpText.Append("Usage: ".Bold());
            helpText.AppendLine($"{Verb.ToUpperInvariant()} [Options]");
            helpText.AppendLine();
            helpText.AppendLine($"Issues a {Verb.ToUpperInvariant()} request.");

            if (RequiresBody)
            {
                helpText.AppendLine("Your default editor will be opened with a sample body if no options are provided.");
            }

            return helpText.ToString();
        }

        public override string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return $"{Verb.ToLowerInvariant()} - Issues a {Verb.ToUpperInvariant()} request";
        }

        protected override IEnumerable<string> GetArgumentSuggestionsForText(IShellState shellState, HttpState programState, ICoreParseResult parseResult, DefaultCommandInput<ICoreParseResult> commandInput, string normalCompletionString)
        {
            List<string> results = new List<string>();

            if (programState.Structure != null && programState.BaseAddress != null)
            {
                //If it's an absolute URI, nothing to suggest
                if (Uri.TryCreate(parseResult.Sections[1], UriKind.Absolute, out Uri _))
                {
                    return null;
                }

                string path = normalCompletionString.Replace('\\', '/');
                int searchFrom = normalCompletionString.Length - 1;
                int lastSlash = path.LastIndexOf('/', searchFrom);
                string prefix;

                if (lastSlash < 0)
                {
                    path = string.Empty;
                    prefix = normalCompletionString;
                }
                else
                {
                    path = path.Substring(0, lastSlash + 1);
                    prefix = normalCompletionString.Substring(lastSlash + 1);
                }

                IDirectoryStructure s = programState.Structure.TraverseTo(programState.PathSections.Reverse()).TraverseTo(path);

                foreach (string child in s.DirectoryNames)
                {
                    if (child.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(path + child);
                    }
                }
            }

            return results;
        }

        protected override IEnumerable<string> GetOptionValueCompletions(IShellState shellState, HttpState programState, string optionId, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult, string normalizedCompletionText)
        {
            if (string.Equals(optionId, BodyFileOption, StringComparison.Ordinal) || string.Equals(optionId, ResponseFileOption, StringComparison.OrdinalIgnoreCase) || string.Equals(optionId, ResponseBodyFileOption, StringComparison.OrdinalIgnoreCase) || string.Equals(optionId, ResponseHeadersFileOption, StringComparison.OrdinalIgnoreCase))
            {
                return FileSystemCompletion.GetCompletions(normalizedCompletionText);
            }

            if (string.Equals(optionId, HeaderOption, StringComparison.Ordinal))
            {
                HashSet<string> alreadySpecifiedHeaders = new HashSet<string>(StringComparer.Ordinal);
                IReadOnlyList<InputElement> options = commandInput.Options[HeaderOption];
                for (int i = 0; i < options.Count; ++i)
                {
                    if (options[i] == commandInput.SelectedElement)
                    {
                        continue;
                    }

                    string elementText = options[i].Text;
                    string existingHeaderName = elementText.Split(HeaderSeparatorChars)[0];
                    alreadySpecifiedHeaders.Add(existingHeaderName);
                }

                //Check to see if the selected element is in a header name or value
                int equalsIndex = normalizedCompletionText.IndexOfAny(HeaderSeparatorChars);
                string path = commandInput.Arguments.Count > 0 ? commandInput.Arguments[0].Text : string.Empty;

                if (equalsIndex < 0)
                {
                    IEnumerable<string> headerNameOptions = HeaderCompletion.GetCompletions(alreadySpecifiedHeaders, normalizedCompletionText);

                    if (headerNameOptions == null)
                    {
                        return null;
                    }

                    List<string> allSuggestions = new List<string>();
                    foreach (string suggestion in headerNameOptions.Select(x => x))
                    {
                        allSuggestions.Add(suggestion + ":");

                        IEnumerable<string> suggestions = HeaderCompletion.GetValueCompletions(Verb, path, suggestion, string.Empty, programState);

                        if (suggestions != null)
                        {
                            foreach (string valueSuggestion in suggestions)
                            {
                                allSuggestions.Add(suggestion + ":" + valueSuggestion);
                            }
                        }
                    }

                    return allSuggestions;
                }
                else
                {
                    //Didn't exit from the header name check, so must be a value
                    string headerName = normalizedCompletionText.Substring(0, equalsIndex);
                    IEnumerable<string> suggestions = HeaderCompletion.GetValueCompletions(Verb, path, headerName, normalizedCompletionText.Substring(equalsIndex + 1), programState);

                    if (suggestions == null)
                    {
                        return null;
                    }

                    return suggestions.Select(x => normalizedCompletionText.Substring(0, equalsIndex + 1) + x);
                }
            }

            return null;
        }
    }
}
