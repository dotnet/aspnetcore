// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.AspNet.Razor.Chunks
{
    public class ParentLiteralChunk : ParentChunk
    {
        public string GetText()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < Children.Count; i++)
            {
                builder.Append(((LiteralChunk)Children[i]).Text);
            }

            return builder.ToString();
        }
    }
}
