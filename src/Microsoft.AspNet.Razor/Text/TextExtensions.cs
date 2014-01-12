// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.AspNet.Razor.Text
{
    internal static class TextExtensions
    {
        public static void Seek(this ITextBuffer self, int characters)
        {
            self.Position += characters;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The consumer is expected to dispose this object")]
        public static ITextDocument ToDocument(this ITextBuffer self)
        {
            ITextDocument ret = self as ITextDocument;
            if (ret == null)
            {
                ret = new SeekableTextReader(self);
            }
            return ret;
        }

        public static LookaheadToken BeginLookahead(this ITextBuffer self)
        {
            int start = self.Position;
            return new LookaheadToken(() =>
            {
                self.Position = start;
            });
        }

        public static string ReadToEnd(this ITextBuffer self)
        {
            StringBuilder builder = new StringBuilder();
            int read;
            while ((read = self.Read()) != -1)
            {
                builder.Append((char)read);
            }
            return builder.ToString();
        }
    }
}
