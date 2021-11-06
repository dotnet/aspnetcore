// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal class IntermediateNodeFormatterBase : IntermediateNodeFormatter
{
    private string _content;
    private readonly Dictionary<string, string> _properties = new Dictionary<string, string>(StringComparer.Ordinal);

    protected FormatterContentMode ContentMode { get; set; }

    protected bool IncludeSource { get; set; }

    protected TextWriter Writer { get; set; }

    public override void WriteChildren(IntermediateNodeCollection children)
    {
        if (children == null)
        {
            throw new ArgumentNullException(nameof(children));
        }

        Writer.Write(" ");
        Writer.Write("\"");
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i] as IntermediateToken;
            if (child != null)
            {
                Writer.Write(EscapeNewlines(child.Content));
            }
        }
        Writer.Write("\"");
    }

    public override void WriteContent(string content)
    {
        if (content == null)
        {
            return;
        }

        _content = EscapeNewlines(content);
    }

    public override void WriteProperty(string key, string value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            return;
        }

        _properties.Add(key, EscapeNewlines(value));
    }

    public void FormatNode(IntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        BeginNode(node);
        node.FormatNode(this);
        EndNode(node);
    }

    public void FormatTree(IntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var visitor = new FormatterVisitor(this);
        visitor.Visit(node);
    }

    private void BeginNode(IntermediateNode node)
    {
        Writer.Write(GetShortName(node));

        if (IncludeSource)
        {
            Writer.Write(" ");
            Writer.Write(node.Source?.ToString() ?? "(n/a)");
        }
    }

    private void EndNode(IntermediateNode node)
    {
        if (_content != null && (_properties.Count == 0 || ContentMode == FormatterContentMode.PreferContent))
        {
            Writer.Write(" ");
            Writer.Write("\"");
            Writer.Write(EscapeNewlines(_content));
            Writer.Write("\"");
        }

        if (_properties.Count > 0 && (_content == null || ContentMode == FormatterContentMode.PreferProperties))
        {
            Writer.Write(" ");
            Writer.Write("{ ");
            Writer.Write(string.Join(", ", _properties.Select(kvp => $"{kvp.Key}: \"{kvp.Value}\"")));
            Writer.Write(" }");
        }

        _content = null;
        _properties.Clear();
    }

    private StringSegment GetShortName(IntermediateNode node)
    {
        var typeName = node.GetType().Name;
        return
            typeName.EndsWith(nameof(IntermediateNode), StringComparison.Ordinal) ?
            new StringSegment(typeName, 0, typeName.Length - nameof(IntermediateNode).Length) :
            typeName;
    }

    private string EscapeNewlines(string content)
    {
        return content.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }

    // Depending on the usage of the formatter we might prefer thoroughness (properties)
    // or brevity (content). Generally if a node has a single string that provides value
    // it has content.
    //
    // Some nodes have neither: TagHelperBody
    // Some nodes have content: HtmlContent
    // Some nodes have properties: Document
    // Some nodes have both: TagHelperProperty
    protected enum FormatterContentMode
    {
        PreferContent,
        PreferProperties,
    }

    protected class FormatterVisitor : IntermediateNodeWalker
    {
        private const int IndentSize = 2;

        private readonly IntermediateNodeFormatterBase _formatter;
        private int _indent;

        public FormatterVisitor(IntermediateNodeFormatterBase formatter)
        {
            _formatter = formatter;
        }

        public override void VisitDefault(IntermediateNode node)
        {
            // Indent
            for (var i = 0; i < _indent; i++)
            {
                _formatter.Writer.Write(' ');
            }
            _formatter.FormatNode(node);
            _formatter.Writer.WriteLine();

            // Process children
            _indent += IndentSize;
            base.VisitDefault(node);
            _indent -= IndentSize;
        }
    }
}
