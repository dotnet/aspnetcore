// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.HttpRepl.OpenApi;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Parsing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.HttpRepl.Commands
{
    public class SetSwaggerCommand : ICommand<HttpState, ICoreParseResult>
    {
        private static readonly string Name = "set";
        private static readonly string SubCommand = "swagger";

        public string Description => "Sets the swagger document to use for information about the current server";

        private static void FillDirectoryInfo(DirectoryStructure parent, EndpointMetadata entry)
        {
            string[] parts = entry.Path.Split('/');

            foreach (string part in parts)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    parent = parent.DeclareDirectory(part);
                }
            }

            RequestInfo dirRequestInfo = new RequestInfo();

            foreach (KeyValuePair<string, IReadOnlyDictionary<string, IReadOnlyList<Parameter>>> requestInfo in entry.AvailableRequests)
            {
                string method = requestInfo.Key;

                foreach (KeyValuePair<string, IReadOnlyList<Parameter>> parameterSetsByContentType in requestInfo.Value)
                {
                    if (string.IsNullOrEmpty(parameterSetsByContentType.Key))
                    {
                        dirRequestInfo.SetFallbackRequestBody(method, parameterSetsByContentType.Key, GetBodyString(null, parameterSetsByContentType.Value));
                    }

                    dirRequestInfo.SetRequestBody(method, parameterSetsByContentType.Key, GetBodyString(parameterSetsByContentType.Key, parameterSetsByContentType.Value));
                }

                dirRequestInfo.AddMethod(method);
            }

            if (dirRequestInfo.Methods.Count > 0)
            {
                parent.RequestInfo = dirRequestInfo;
            }
        }

        private static string GetBodyString(string contentType, IEnumerable<Parameter> operation)
        {
            Parameter body = operation.FirstOrDefault(x => string.Equals(x.Location, "body", StringComparison.OrdinalIgnoreCase));

            if (body != null)
            {
                JToken result = GenerateData(body.Schema);
                return result?.ToString() ?? "{\n}";
            }

            return null;
        }

        private static JToken GenerateData(Schema schema)
        {
            if (schema == null)
            {
                return null;
            }

            if (schema.Example != null)
            {
                return JToken.FromObject(schema.Example);
            }

            if (schema.Default != null)
            {
                return JToken.FromObject(schema.Default);
            }

            if (schema.Type is null)
            {
                if (schema.Properties != null || schema.AdditionalProperties != null || schema.MinProperties.HasValue || schema.MaxProperties.HasValue)
                {
                    schema.Type = "OBJECT";
                }
                else if (schema.Items != null || schema.MinItems.HasValue || schema.MaxItems.HasValue)
                {
                    schema.Type = "ARRAY";
                }
                else if (schema.Minimum.HasValue || schema.Maximum.HasValue || schema.MultipleOf.HasValue)
                {
                    schema.Type = "INTEGER";
                }
            }

            switch (schema.Type?.ToUpperInvariant())
            {
                case null:
                case "STRING":
                    return "";
                case "NUMBER":
                    if (schema.Minimum.HasValue)
                    {
                        if (schema.Maximum.HasValue)
                        {
                            return (schema.Maximum.Value + schema.Minimum.Value) / 2;
                        }

                        if (schema.ExclusiveMinimum)
                        {
                            return schema.Minimum.Value + 1;
                        }

                        return schema.Minimum.Value;
                    }
                    return 1.1;
                case "INTEGER":
                    if (schema.Minimum.HasValue)
                    {
                        if (schema.Maximum.HasValue)
                        {
                            return (int)((schema.Maximum.Value + schema.Minimum.Value) / 2);
                        }

                        if (schema.ExclusiveMinimum)
                        {
                            return schema.Minimum.Value + 1;
                        }

                        return schema.Minimum.Value;
                    }
                    return 0;
                case "BOOLEAN":
                    return true;
                case "ARRAY":
                    JArray container = new JArray();
                    JToken item = GenerateData(schema.Items) ?? "";

                    int count = schema.MinItems.GetValueOrDefault();
                    count = Math.Max(1, count);

                    for (int i = 0; i < count; ++i)
                    {
                        container.Add(item.DeepClone());
                    }

                    return container;
                case "OBJECT":
                    JObject obj = new JObject();
                    foreach (KeyValuePair<string, Schema> property in schema.Properties)
                    {
                        JToken data = GenerateData(property.Value) ?? "";
                        obj[property.Key] = data;
                    }
                    return obj;
            }

            return null;
        }

        private static async Task<IEnumerable<EndpointMetadata>> GetSwaggerDocAsync(HttpClient client, Uri uri)
        {
            var resp = await client.GetAsync(uri).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            string responseString = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            JsonSerializer serializer = new JsonSerializer{ PreserveReferencesHandling = PreserveReferencesHandling.All };
            JObject responseObject = (JObject)serializer.Deserialize(new StringReader(responseString), typeof(JObject));
            EndpointMetadataReader reader = new EndpointMetadataReader();
            responseObject = await PointerUtil.ResolvePointersAsync(uri, responseObject, client).ConfigureAwait(false) as JObject;

            if (responseObject is null)
            {
                return new EndpointMetadata[0];
            }

            return reader.Read(responseObject);
        }

        public string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return Description;
        }

        public string GetHelpDetails(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.Sections.Count > 1 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase) && string.Equals(parseResult.Sections[1], SubCommand, StringComparison.OrdinalIgnoreCase))
            {
                return Description;
            }

            return null;
        }

        public IEnumerable<string> Suggest(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.Sections.Count == 0)
            {
                return new[] { Name };
            }

            if (parseResult.Sections.Count > 0 && parseResult.SelectedSection == 0 && Name.StartsWith(parseResult.Sections[0].Substring(0, parseResult.CaretPositionWithinSelectedSection), StringComparison.OrdinalIgnoreCase))
            {
                return new[] { Name };
            }

            if (string.Equals(Name, parseResult.Sections[0], StringComparison.OrdinalIgnoreCase) && parseResult.SelectedSection == 1 && (parseResult.Sections.Count < 2 || SubCommand.StartsWith(parseResult.Sections[1].Substring(0, parseResult.CaretPositionWithinSelectedSection), StringComparison.OrdinalIgnoreCase)))
            {
                return new[] { SubCommand };
            }

            return null;
        }

        public bool? CanHandle(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            return parseResult.Sections.Count > 1 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase) && string.Equals(parseResult.Sections[1], SubCommand, StringComparison.OrdinalIgnoreCase)
                ? (bool?)true
                : null;
        }

        public async Task ExecuteAsync(IShellState shellState, HttpState programState, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (parseResult.Sections.Count == 2)
            {
                programState.SwaggerStructure = null;
                return;
            }

            if (parseResult.Sections.Count != 3 || string.IsNullOrEmpty(parseResult.Sections[2]) || !Uri.TryCreate(parseResult.Sections[2], UriKind.Absolute, out Uri serverUri))
            {
                shellState.ConsoleManager.Error.WriteLine("Must specify a swagger document".SetColor(programState.ErrorColor));
            }
            else
            {
                await CreateDirectoryStructureForSwaggerEndpointAsync(shellState, programState, serverUri, cancellationToken).ConfigureAwait(false);
            }
        }

        internal static async Task CreateDirectoryStructureForSwaggerEndpointAsync(IShellState shellState, HttpState programState, Uri serverUri, CancellationToken cancellationToken)
        {
            programState.SwaggerEndpoint = serverUri;

            try
            {
                IEnumerable<EndpointMetadata> doc = await GetSwaggerDocAsync(programState.Client, serverUri).ConfigureAwait(false);

                DirectoryStructure d = new DirectoryStructure(null);

                foreach (EndpointMetadata entry in doc)
                {
                    FillDirectoryInfo(d, entry);
                }

                programState.SwaggerStructure = !cancellationToken.IsCancellationRequested ? d : null;
            }
            catch
            {
                programState.SwaggerStructure = null;
            }
        }
    }
}
