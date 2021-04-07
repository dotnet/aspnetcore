// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    internal static class HtmlContentIntermediateNodeExtensions
    {
        private static readonly string HasEncodedContent = "HasEncodedContent";

        public static bool IsEncoded(this HtmlContentIntermediateNode node)
        {
            return ReferenceEquals(node.Annotations[HasEncodedContent], HasEncodedContent);
        }

        public static void SetEncoded(this HtmlContentIntermediateNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            node.Annotations[HasEncodedContent] = HasEncodedContent;
        }
    }
}