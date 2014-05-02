// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
