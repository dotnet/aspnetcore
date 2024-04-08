// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// This implementation is based off the `TypeNameBuilder` used by the runtime
/// to generate `Type.FullName`s. It's been modified slightly to satisfy our
/// OpenAPI requirements. Original implementation can be seen here:
/// https://github.com/dotnet/runtime/blob/d88c7ba88627b4b68ad523ba27cb354809eb7e67/src/libraries/System.Private.CoreLib/src/System/Reflection/Emit/TypeNameBuilder.cs
/// </summary>
internal sealed class TypeNameBuilder
{
    private readonly StringBuilder _str = new();
    private int _instNesting;
    private bool _firstInstArg;
    private bool _nestedName;
    private bool _hasAssemblySpec;
    private readonly List<int> _stack = [];
    private int _stackIdx;

    internal TypeNameBuilder()
    {
    }

    private void OpenGenericArguments()
    {
        _instNesting++;
        _firstInstArg = true;
    }

    private void CloseGenericArguments()
    {
        Debug.Assert(_instNesting != 0);

        _instNesting--;

        if (_firstInstArg)
        {
            _str.Remove(_str.Length - 1, 1);
        }
    }

    private void OpenGenericArgument()
    {
        Debug.Assert(_instNesting != 0);

        _nestedName = false;

        if (!_firstInstArg)
        {
            Append("And");
        }

        _firstInstArg = false;

        Append("Of");

        PushOpenGenericArgument();
    }

    private void CloseGenericArgument()
    {
        Debug.Assert(_instNesting != 0);

        if (_hasAssemblySpec)
        {
            Append(']');
        }
    }

    private void AddName(string name)
    {
        Debug.Assert(name != null);

        if (_nestedName)
        {
            Append('+');
        }

        _nestedName = true;

        EscapeName(name);
    }

    private void AddArray(int rank)
    {
        Debug.Assert(rank > 0);

        if (rank == 1)
        {
            Append("[*]");
        }
        else if (rank > 64)
        {
            // Only taken in an error path, runtime will not load arrays of more than 32 dimensions
            _str.Append('[').Append(rank).Append(']');
        }
        else
        {
            Append('[');
            for (int i = 1; i < rank; i++)
            {
                Append(',');
            }

            Append(']');
        }
    }

    private void AddAssemblySpec(string assemblySpec)
    {
        if (assemblySpec != null && !assemblySpec.Equals(""))
        {
            Append(", ");

            if (_instNesting > 0)
            {
                EscapeEmbeddedAssemblyName(assemblySpec);
            }
            else
            {
                EscapeAssemblyName(assemblySpec);
            }

            _hasAssemblySpec = true;
        }
    }

    public override string ToString()
    {
        Debug.Assert(_instNesting == 0);

        return _str.ToString();
    }

    private static bool ContainsReservedChar(string name)
    {
        foreach (char c in name)
        {
            if (c == '\0')
            {
                break;
            }

            if (IsTypeNameReservedChar(c))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsTypeNameReservedChar(char ch)
    {
        return ch switch
        {
            ',' or '[' or ']' or '&' or '*' or '+' or '\\' or '`' or '<' or '>' => true,
            _ => false,
        };
    }

    private void EscapeName(string name)
    {
        if (ContainsReservedChar(name))
        {
            foreach (char c in name)
            {
                if (c == '\0')
                {
                    break;
                }

                if (char.IsDigit(c))
                {
                    continue;
                }

                if (IsTypeNameReservedChar(c))
                {
                    continue;
                }

                _str.Append(c);
            }
        }
        else
        {
            Append(name);
        }
    }

    private void EscapeAssemblyName(string name)
    {
        Append(name);
    }

    private void EscapeEmbeddedAssemblyName(string name)
    {
        if (name.Contains(']'))
        {
            foreach (char c in name)
            {
                if (c == ']')
                {
                    Append('\\');
                }

                Append(c);
            }
        }
        else
        {
            Append(name);
        }
    }

    private void PushOpenGenericArgument()
    {
        _stack.Add(_str.Length);
        _stackIdx++;
    }

    private void Append(string pStr)
    {
        int i = pStr.IndexOf('\0');
        if (i < 0)
        {
            _str.Append(pStr);
        }
        else if (i > 0)
        {
            _str.Append(pStr.AsSpan(0, i));
        }
    }

    private void Append(char c)
    {
        _str.Append(c);
    }

    internal enum Format
    {
        ToString,
        FullName,
        AssemblyQualifiedName,
    }

    internal static string? ToString(Type type, Format format)
    {
        if (format == Format.FullName || format == Format.AssemblyQualifiedName)
        {
            if (!type.IsGenericTypeDefinition && type.ContainsGenericParameters)
            {
                return null;
            }
        }

        var tnb = new TypeNameBuilder();
        tnb.AddAssemblyQualifiedName(type, format);
        return tnb.ToString();
    }

    private void AddElementType(Type type)
    {
        if (!type.HasElementType)
        {
            return;
        }

        AddElementType(type.GetElementType()!);

        if (type.IsPointer)
        {
            Append('*');
        }
        else if (type.IsByRef)
        {
            Append('&');
        }
        else if (type.IsSZArray)
        {
            Append("[]");
        }
        else if (type.IsArray)
        {
            AddArray(type.GetArrayRank());
        }
    }

    internal void AddAssemblyQualifiedName(Type type, Format format)
    {
        // Append just the type name to the start because
        // we don't want to include the fully qualified name
        // in the OpenAPI document.
        AddName(type.Name);

        // Append generic arguments
        if (type.IsGenericType && (!type.IsGenericTypeDefinition || format == Format.ToString))
        {
            Type[] genericArguments = type.GetGenericArguments();

            OpenGenericArguments();
            for (int i = 0; i < genericArguments.Length; i++)
            {
                Format genericArgumentsFormat = format == Format.FullName ? Format.AssemblyQualifiedName : format;

                OpenGenericArgument();
                AddAssemblyQualifiedName(genericArguments[i], genericArgumentsFormat);
                CloseGenericArgument();
            }
            CloseGenericArguments();
        }

        // Append pointer, byRef and array qualifiers
        AddElementType(type);

        if (format == Format.AssemblyQualifiedName)
        {
            AddAssemblySpec(type.Module.Assembly.FullName!);
        }
    }
}
