// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

[DebuggerDisplay("{DebuggerToString(),nq}")]
public abstract class IntermediateNode
{
    private ItemCollection _annotations;
    private RazorDiagnosticCollection _diagnostics;

    public ItemCollection Annotations
    {
        get
        {
            if (_annotations == null)
            {
                _annotations = new ItemCollection();
            }

            return _annotations;
        }
    }

    public abstract IntermediateNodeCollection Children { get; }

    public RazorDiagnosticCollection Diagnostics
    {
        get
        {
            if (_diagnostics == null)
            {
                _diagnostics = new RazorDiagnosticCollection();
            }

            return _diagnostics;
        }
    }

    public bool HasDiagnostics => _diagnostics != null && _diagnostics.Count > 0;

    public SourceSpan? Source { get; set; }

    public abstract void Accept(IntermediateNodeVisitor visitor);

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    private string Tree
    {
        get
        {
            var formatter = new DebuggerDisplayFormatter();
            formatter.FormatTree(this);
            return formatter.ToString();
        }
    }

    private string DebuggerToString()
    {
        var formatter = new DebuggerDisplayFormatter();
        formatter.FormatNode(this);
        return formatter.ToString();
    }


    public virtual void FormatNode(IntermediateNodeFormatter formatter)
    {
    }
}
