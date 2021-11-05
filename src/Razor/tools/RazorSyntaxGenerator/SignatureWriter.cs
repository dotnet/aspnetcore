// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RazorSyntaxGenerator;

internal class SignatureWriter
{
    private readonly TextWriter _writer;
    private readonly Tree _tree;
    private readonly Dictionary<string, string> _typeMap;

    private SignatureWriter(TextWriter writer, Tree tree)
    {
        _writer = writer;
        _tree = tree;
        _typeMap = tree.Types.ToDictionary(n => n.Name, n => n.Base);
        _typeMap.Add(tree.Root, null);
    }

    public static void Write(TextWriter writer, Tree tree)
    {
        new SignatureWriter(writer, tree).WriteFile();
    }

    private void WriteFile()
    {
        _writer.WriteLine("using System;");
        _writer.WriteLine("using System.Collections;");
        _writer.WriteLine("using System.Collections.Generic;");
        _writer.WriteLine("using System.Linq;");
        _writer.WriteLine("using System.Threading;");
        _writer.WriteLine();
        _writer.WriteLine("namespace Microsoft.AspNetCore.Razor.Language.Syntax");
        _writer.WriteLine("{");

        this.WriteTypes();

        _writer.WriteLine("}");
    }

    private void WriteTypes()
    {
        var nodes = _tree.Types.Where(n => !(n is PredefinedNode)).ToList();
        for (int i = 0, n = nodes.Count; i < n; i++)
        {
            var node = nodes[i];
            _writer.WriteLine();
            WriteType(node);
        }
    }

    private void WriteType(TreeType node)
    {
        if (node is AbstractNode abstractNode)
        {
            _writer.WriteLine("  public abstract partial class {0} : {1}", node.Name, node.Base);
            _writer.WriteLine("  {");
            for (int i = 0, n = abstractNode.Fields.Count; i < n; i++)
            {
                var field = abstractNode.Fields[i];
                if (IsNodeOrNodeList(field.Type))
                {
                    _writer.WriteLine("    public abstract {0}{1} {2} {{ get; }}", "", field.Type, field.Name);
                }
            }
            _writer.WriteLine("  }");
        }
        else if (node is Node nd)
        {
            _writer.WriteLine("  public partial class {0} : {1}", node.Name, node.Base);
            _writer.WriteLine("  {");

            WriteKinds(nd.Kinds);

            var valueFields = nd.Fields.Where(n => !IsNodeOrNodeList(n.Type)).ToList();
            var nodeFields = nd.Fields.Where(n => IsNodeOrNodeList(n.Type)).ToList();

            for (int i = 0, n = nodeFields.Count; i < n; i++)
            {
                var field = nodeFields[i];
                _writer.WriteLine("    public {0}{1}{2} {3} {{ get; }}", "", "", field.Type, field.Name);
            }

            for (int i = 0, n = valueFields.Count; i < n; i++)
            {
                var field = valueFields[i];
                _writer.WriteLine("    public {0}{1}{2} {3} {{ get; }}", "", "", field.Type, field.Name);
            }

            _writer.WriteLine("  }");
        }
    }

    private void WriteKinds(List<Kind> kinds)
    {
        if (kinds.Count > 1)
        {
            foreach (var kind in kinds)
            {
                _writer.WriteLine("    // {0}", kind.Name);
            }
        }
    }

    private bool IsSeparatedNodeList(string typeName)
    {
        return typeName.StartsWith("SeparatedSyntaxList<", StringComparison.Ordinal);
    }

    private bool IsNodeList(string typeName)
    {
        return typeName.StartsWith("SyntaxList<", StringComparison.Ordinal);
    }

    public bool IsNodeOrNodeList(string typeName)
    {
        return IsNode(typeName) || IsNodeList(typeName) || IsSeparatedNodeList(typeName);
    }

    private bool IsNode(string typeName)
    {
        return _typeMap.ContainsKey(typeName);
    }
}
