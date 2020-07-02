using System;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal static class RazorCSharpUtilities
    {
        public static bool TrySplitNamespaceAndType(string fullTypeName, out TextSpan @namespace, out TextSpan typeName)
        {
            @namespace = default;
            typeName = default;

            if (string.IsNullOrEmpty(fullTypeName))
            {
                return false;
            }

            var nestingLevel = 0;
            var splitLocation = -1;
            for (var i = fullTypeName.Length - 1; i >= 0; i--)
            {
                var c = fullTypeName[i];
                if (c == Type.Delimiter && nestingLevel == 0)
                {
                    splitLocation = i;
                    break;
                }
                else if (c == '>')
                {
                    nestingLevel++;
                }
                else if (c == '<')
                {
                    nestingLevel--;
                }
            }

            if (splitLocation == -1)
            {
                typeName = new TextSpan(0, fullTypeName.Length);
                return true;
            }

            @namespace = new TextSpan(0, splitLocation);

            var typeNameStartLocation = splitLocation + 1;
            if (typeNameStartLocation < fullTypeName.Length)
            {
                typeName = new TextSpan(typeNameStartLocation, fullTypeName.Length - typeNameStartLocation);
            }

            return true;
        }
    }
}
