// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorUnitedApp.Validation;

/// <summary>
/// Bidirectional mapping between <see cref="FieldIdentifier"/>s and dotted property paths
/// (e.g. <c>"Address.Street"</c>, <c>"Items[2].Quantity"</c>) relative to a root model.
/// <para>
/// Validator libraries that work in flat-string property keys (FluentValidation, JSON Schema,
/// server-side ModelState round-tripping) need both directions:
/// <list type="bullet">
/// <item><see cref="TryDecode"/> — given a path emitted by the validator, find the leaf object
/// and field name so a <see cref="FieldIdentifier"/> can be constructed for message attribution.</item>
/// <item><see cref="TryEncode"/> — given the <see cref="FieldIdentifier"/> from <c>OnFieldChanged</c>,
/// find the dotted path so the validator can scope its work to the touched property.</item>
/// </list>
/// </para>
/// <para>
/// Encoding is a graph walk over the root model and uses reference equality to locate the leaf.
/// Cycles are broken via a visited set; ambiguity (the same instance referenced from multiple
/// places) returns the first match found in property declaration order.
/// </para>
/// </summary>
internal static class FieldPath
{
    public static bool TryDecode(object root, string path, out FieldIdentifier fieldIdentifier)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(path);

        fieldIdentifier = default;
        if (path.Length == 0)
        {
            fieldIdentifier = new FieldIdentifier(root, string.Empty);
            return true;
        }

        var segments = TokenizePath(path);
        if (segments.Count == 0)
        {
            return false;
        }

        var current = root;
        for (var i = 0; i < segments.Count - 1; i++)
        {
            if (!TryNavigate(current, segments[i], out var next) || next is null)
            {
                return false;
            }
            current = next;
        }

        var last = segments[^1];
        if (last.IsIndex)
        {
            // Trailing indexer with no member name doesn't have a clean FieldIdentifier mapping.
            return false;
        }

        if (current is null || current.GetType().IsValueType)
        {
            return false;
        }

        fieldIdentifier = new FieldIdentifier(current, last.Name);
        return true;
    }

    public static bool TryEncode(object root, FieldIdentifier fieldIdentifier, [NotNullWhen(true)] out string? path)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (ReferenceEquals(fieldIdentifier.Model, root))
        {
            path = fieldIdentifier.FieldName;
            return true;
        }

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (TryFindPathToInstance(root, fieldIdentifier.Model, visited, out var prefix))
        {
            path = prefix.Length == 0
                ? fieldIdentifier.FieldName
                : prefix + "." + fieldIdentifier.FieldName;
            return true;
        }

        path = null;
        return false;
    }

    private static bool TryFindPathToInstance(object current, object target, HashSet<object> visited, out string path)
    {
        if (!visited.Add(current))
        {
            path = string.Empty;
            return false;
        }

        var type = current.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            object? value;
            try
            {
                value = property.GetValue(current);
            }
            catch
            {
                continue;
            }

            if (value is null)
            {
                continue;
            }

            if (ReferenceEquals(value, target))
            {
                path = property.Name;
                return true;
            }

            if (value is string)
            {
                continue;
            }

            if (value is IList list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item is null)
                    {
                        continue;
                    }

                    if (ReferenceEquals(item, target))
                    {
                        path = $"{property.Name}[{i}]";
                        return true;
                    }

                    if (item.GetType().IsValueType || item is string)
                    {
                        continue;
                    }

                    if (TryFindPathToInstance(item, target, visited, out var nested))
                    {
                        path = $"{property.Name}[{i}].{nested}";
                        return true;
                    }
                }

                continue;
            }

            if (value.GetType().IsValueType)
            {
                continue;
            }

            if (TryFindPathToInstance(value, target, visited, out var sub))
            {
                path = sub.Length == 0 ? property.Name : property.Name + "." + sub;
                return true;
            }
        }

        path = string.Empty;
        return false;
    }

    private static bool TryNavigate(object current, PathSegment segment, out object? next)
    {
        if (segment.IsIndex)
        {
            if (current is IList list && segment.Index >= 0 && segment.Index < list.Count)
            {
                next = list[segment.Index];
                return true;
            }
            next = null;
            return false;
        }

        var property = current.GetType().GetProperty(segment.Name, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
        {
            next = null;
            return false;
        }

        try
        {
            next = property.GetValue(current);
            return true;
        }
        catch
        {
            next = null;
            return false;
        }
    }

    private static List<PathSegment> TokenizePath(string path)
    {
        var segments = new List<PathSegment>();
        var i = 0;
        while (i < path.Length)
        {
            if (path[i] == '[')
            {
                var end = path.IndexOf(']', i);
                if (end < 0 || !int.TryParse(path.AsSpan(i + 1, end - i - 1), out var index))
                {
                    return new List<PathSegment>();
                }

                segments.Add(PathSegment.ForIndex(index));
                i = end + 1;
                if (i < path.Length && path[i] == '.')
                {
                    i++;
                }
                continue;
            }

            var nameEnd = path.IndexOfAny(new[] { '.', '[' }, i);
            if (nameEnd < 0)
            {
                segments.Add(PathSegment.ForName(path[i..]));
                break;
            }

            segments.Add(PathSegment.ForName(path[i..nameEnd]));
            i = path[nameEnd] == '.' ? nameEnd + 1 : nameEnd;
        }

        return segments;
    }

    private readonly record struct PathSegment(string Name, int Index, bool IsIndex)
    {
        public static PathSegment ForName(string name) => new(name, -1, false);
        public static PathSegment ForIndex(int index) => new(string.Empty, index, true);
    }
}
