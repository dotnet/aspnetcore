// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
namespace Microsoft.AspNetCore.Razor.Tokenizer
{
    internal static class XmlHelpers
    {
        public static bool IsXmlNameStartChar(char chr)
        {
            // [4] NameStartChar    ::= ":" | [A-Z] | "_" | [a-z] | [#xC0-#xD6] | [#xD8-#xF6] | [#xF8-#x2FF] | [#x370-#x37D] |
            //                          [#x37F-#x1FFF] | [#x200C-#x200D] | [#x2070-#x218F] | [#x2C00-#x2FEF] | [#x3001-#xD7FF] |
            //                          [#xF900-#xFDCF] | [#xFDF0-#xFFFD] | [#x10000-#xEFFFF]
            // http://www.w3.org/TR/REC-xml/#NT-Name

            return Char.IsLetter(chr) ||
                   chr == ':' ||
                   chr == '_' ||
                   IsInRange(chr, 0xC0, 0xD6) ||
                   IsInRange(chr, 0xD8, 0xF6) ||
                   IsInRange(chr, 0xF8, 0x2FF) ||
                   IsInRange(chr, 0x370, 0x37D) ||
                   IsInRange(chr, 0x37F, 0x1FFF) ||
                   IsInRange(chr, 0x200C, 0x200D) ||
                   IsInRange(chr, 0x2070, 0x218F) ||
                   IsInRange(chr, 0x2C00, 0x2FEF) ||
                   IsInRange(chr, 0x3001, 0xD7FF) ||
                   IsInRange(chr, 0xF900, 0xFDCF) ||
                   IsInRange(chr, 0xFDF0, 0xFFFD) ||
                   IsInRange(chr, 0x10000, 0xEFFFF);
        }

        public static bool IsXmlNameChar(char chr)
        {
            // [4a] NameChar     ::= NameStartChar | "-" | "." | [0-9] | #xB7 | [#x0300-#x036F] | [#x203F-#x2040]
            // http://www.w3.org/TR/REC-xml/#NT-Name
            return Char.IsDigit(chr) ||
                   IsXmlNameStartChar(chr) ||
                   chr == '-' ||
                   chr == '.' ||
                   chr == '·' || // (U+00B7 is middle dot: ·)
                   IsInRange(chr, 0x0300, 0x036F) ||
                   IsInRange(chr, 0x203F, 0x2040);
        }

        public static bool IsInRange(char chr, int low, int high)
        {
            return ((int)chr >= low) && ((int)chr <= high);
        }
    }
}
