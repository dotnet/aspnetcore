// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.BuildTools.Core.ILWipe
{
    class SpecListEntry
    {
        public bool Negated { get; }
        public string TypeName { get; }
        public string MethodName { get; }
        public string Args { get; }

        public SpecListEntry(string parseSpecLine)
        {
            parseSpecLine = parseSpecLine.Trim();

            if (parseSpecLine.StartsWith('!'))
            {
                Negated = true;
                parseSpecLine = parseSpecLine.Substring(1);
            }

            var colonsPos = parseSpecLine.IndexOf("::");
            if (colonsPos < 0)
            {
                TypeName = parseSpecLine;
            }
            else
            {
                TypeName = parseSpecLine.Substring(0, colonsPos);
                parseSpecLine = parseSpecLine.Substring(colonsPos + 2);

                var bracketPos = parseSpecLine.IndexOf('(');
                if (bracketPos < 0)
                {
                    MethodName = parseSpecLine;
                }
                else
                {
                    MethodName = parseSpecLine.Substring(0, bracketPos);
                    Args = parseSpecLine.Substring(bracketPos + 1, parseSpecLine.Length - bracketPos - 2);
                }
            }
        }

        public bool IsMatch(AssemblyItem item)
        {
            return MatchesType(item)
                && MatchesMethod(item)
                && MatchesArgs(item);
        }

        private bool MatchesArgs(AssemblyItem item)
        {
            if (Args == null)
            {
                return true;
            }
            else
            {
                var methodString = item.Method.ToString();
                var bracketPos = methodString.IndexOf('(');
                var argsString = methodString.Substring(bracketPos + 1, methodString.Length - bracketPos - 2);
                return string.Equals(argsString, Args, StringComparison.Ordinal);
            }
        }

        private bool MatchesMethod(AssemblyItem item)
        {
            if (MethodName == null)
            {
                return true;
            }
            else if (MethodName.EndsWith('*'))
            {
                return item.Method.Name.StartsWith(
                    MethodName.Substring(0, MethodName.Length - 1),
                    StringComparison.Ordinal);
            }
            else
            {
                return string.Equals(item.Method.Name, MethodName, StringComparison.Ordinal);
            }
        }

        private bool MatchesType(AssemblyItem item)
        {
            var declaringTypeFullName = item.Method.DeclaringType.FullName;
            if (TypeName.EndsWith('*'))
            {
                // Wildcard match
                return declaringTypeFullName.StartsWith(
                    TypeName.Substring(0, TypeName.Length - 1),
                    StringComparison.Ordinal);
            }
            else
            {
                // Exact match
                if (string.Equals(
                    item.Method.DeclaringType.FullName,
                    TypeName,
                    StringComparison.Ordinal))
                {
                    return true;
                }

                // If we're matching all members of the type, include nested types
                if (MethodName == null && declaringTypeFullName.StartsWith(
                    $"{TypeName}/",
                    StringComparison.Ordinal))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
