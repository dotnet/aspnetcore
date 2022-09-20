// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Swaggatherer;

internal sealed class SwaggathererApplication : CommandLineApplication
{
    public SwaggathererApplication()
    {
        Invoke = InvokeCore;

        HttpMethods = Option("-m|--method", "allow multiple endpoints with different http method", CommandOptionType.NoValue);
        Input = Option("-i", "input swagger 2.0 JSON file", CommandOptionType.MultipleValue);
        InputDirectory = Option("-d", "input directory", CommandOptionType.SingleValue);
        Output = Option("-o", "output", CommandOptionType.SingleValue);

        HelpOption("-h|--help");
    }

    public CommandOption Input { get; }

    public CommandOption InputDirectory { get; }

    // Support multiple endpoints that are distinguished only by http method.
    public CommandOption HttpMethods { get; }

    public CommandOption Output { get; }

    private int InvokeCore()
    {
        if (!Input.HasValue() && !InputDirectory.HasValue())
        {
            ShowHelp();
            return 1;
        }

        if (Input.HasValue() && InputDirectory.HasValue())
        {
            ShowHelp();
            return 1;
        }

        if (!Output.HasValue())
        {
            Output.Values.Add("Out.generated.cs");
        }

        if (InputDirectory.HasValue())
        {
            Input.Values.AddRange(Directory.EnumerateFiles(InputDirectory.Value(), "*.json", SearchOption.AllDirectories));
        }

        Console.WriteLine($"Processing {Input.Values.Count} files...");
        var entries = new List<RouteEntry>();
        for (var i = 0; i < Input.Values.Count; i++)
        {
            var input = ReadInput(Input.Values[i]);
            ParseEntries(input, entries);
        }

        // We don't yet want to support complex segments.
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (HasComplexSegment(entries[i]))
            {
                Out.WriteLine("Skipping route with complex segment: " + entries[i].Template.TemplateText);
                entries.RemoveAt(i);
            }
        }

        // The data that we're provided by might be unambiguous.
        // Remove any routes that would be ambiguous in our system.
        var routesByPrecedence = new Dictionary<decimal, List<RouteEntry>>();
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            var precedence = RoutePrecedence.ComputeInbound(entries[i].Template);

            if (!routesByPrecedence.TryGetValue(precedence, out var matches))
            {
                matches = new List<RouteEntry>();
                routesByPrecedence.Add(precedence, matches);
            }

            if (IsDuplicateTemplate(entry, matches))
            {
                Out.WriteLine("Duplicate route template: " + entries[i].Template.TemplateText);
                entries.RemoveAt(i);
                continue;
            }

            matches.Add(entry);
        }

        // We're not too sophisticated with how we generate parameter values, just hoping for
        // the best. For parameters we generate a segment that is the same length as the parameter name
        // but with a minimum of 5 characters to avoid collisions.
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            entries[i].RequestUrl = GenerateRequestUrl(entries[i].Template);
            if (entries[i].RequestUrl == null)
            {
                Out.WriteLine("Failed to create a request for: " + entries[i].Template.TemplateText);
                entries.RemoveAt(i);
                continue;
            }
        }

        Sort(entries);

        var text = Template.Execute(entries);
        File.WriteAllText(Output.Value(), text);
        return 0;
    }

    private JObject ReadInput(string input)
    {
        using (var reader = File.OpenText(input))
        {
            try
            {
                return JObject.Load(new JsonTextReader(reader));
            }
            catch (JsonReaderException ex)
            {
                Out.WriteLine($"Error reading: {input}");
                Out.WriteLine(ex);
                return new JObject();
            }
        }
    }

    private void ParseEntries(JObject input, List<RouteEntry> entries)
    {
        var basePath = "";
        if (input["basePath"] is JProperty basePathProperty)
        {
            basePath = basePathProperty.Value<string>();
        }

        if (input["paths"] is JObject paths)
        {
            foreach (var path in paths.Properties())
            {
                foreach (var method in ((JObject)path.Value).Properties())
                {
                    var template = basePath + path.Name;
                    var parsed = TemplateParser.Parse(template);
                    entries.Add(new RouteEntry()
                    {
                        Method = HttpMethods.HasValue() ? method.Name.ToString() : null,
                        Template = parsed,
                        Precedence = RoutePrecedence.ComputeInbound(parsed),
                    });
                }
            }
        }
    }

    private static bool HasComplexSegment(RouteEntry entry)
    {
        for (var i = 0; i < entry.Template.Segments.Count; i++)
        {
            if (!entry.Template.Segments[i].IsSimple)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDuplicateTemplate(RouteEntry entry, List<RouteEntry> others)
    {
        for (var j = 0; j < others.Count; j++)
        {
            // This is another route with the same precedence. It is guaranteed to have the same number of segments
            // of the same kinds and in the same order. We just need to check the literals.
            var other = others[j];

            var isSame = true;
            for (var k = 0; k < entry.Template.Segments.Count; k++)
            {
                if (!string.Equals(
                    entry.Template.Segments[k].Parts[0].Text,
                    other.Template.Segments[k].Parts[0].Text,
                    StringComparison.OrdinalIgnoreCase))
                {
                    isSame = false;
                    break;
                }

                if (HttpMethods.HasValue() &&
                    !string.Equals(entry.Method, other.Method, StringComparison.OrdinalIgnoreCase))
                {
                    isSame = false;
                    break;
                }
            }

            if (isSame)
            {
                return true;
            }
        }

        return false;
    }

    private static void Sort(List<RouteEntry> entries)
    {
        // We need to sort these in precedence order for the linear matchers.
        entries.Sort((x, y) =>
        {
            var comparison = RoutePrecedence.ComputeInbound(x.Template).CompareTo(RoutePrecedence.ComputeInbound(y.Template));
            if (comparison != 0)
            {
                return comparison;
            }

            return string.Compare(x.Template.TemplateText, y.Template.TemplateText, StringComparison.Ordinal);
        });
    }

    private static string GenerateRequestUrl(RouteTemplate template)
    {
        if (template.Segments.Count == 0)
        {
            return "/";
        }

        var url = new StringBuilder();
        for (var i = 0; i < template.Segments.Count; i++)
        {
            // We don't yet handle complex segments
            var part = template.Segments[i].Parts[0];

            url.Append('/');
            url.Append(part.IsLiteral ? part.Text : GenerateParameterValue(part));
        }

        return url.ToString();
    }

    private static string GenerateParameterValue(TemplatePart part)
    {
        var text = Guid.NewGuid().ToString();
        var length = Math.Min(text.Length, Math.Max(5, part.Name.Length));
        return text.Substring(0, length);
    }
}
