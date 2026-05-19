// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers;

[HtmlTargetElement("pre")]
public class CustomEncoderTagHelper : TagHelper
{
    public override int Order => 1;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var encoder = new CustomEncoder();
        var customContent = await output.GetChildContentAsync(encoder);

        // Note this is very unsafe. Should always post-process content that may not be fully HTML encoded before
        // writing it into a response. Here for example, could pass SetContent() a string and that would be
        // HTML encoded later.
        output.PreContent
            .SetHtmlContent("Custom encoder: ")
            .AppendHtml(customContent)
            .AppendHtml("<br />");
    }

    // Simple encoder that just wraps "string" as "Custom[[string]]". Note: Lacks all parameter checks.
    private class CustomEncoder : HtmlEncoder
    {
        public CustomEncoder()
        {
        }

        public override int MaxOutputCharactersPerInputCharacter => 1;

        public override string Encode(string value) => $"Custom[[{ value }]]";

        public override void Encode(TextWriter output, char[] value, int startIndex, int characterCount)
        {
            if (characterCount == 0)
            {
                return;
            }

            output.Write("Custom[[");
            output.Write(value, startIndex, characterCount);
            output.Write("]]");
        }

        public override void Encode(TextWriter output, string value, int startIndex, int characterCount)
        {
            if (characterCount == 0)
            {
                return;
            }

            output.Write("Custom[[");
            output.Write(value.Substring(startIndex, characterCount));
            output.Write("]]");
        }

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength) => -1;

        public override unsafe bool TryEncodeUnicodeScalar(
            int unicodeScalar,
            char* buffer,
            int bufferLength,
            out int numberOfCharactersWritten)
        {
            numberOfCharactersWritten = 0;

            return false;
        }

        public override bool WillEncode(int unicodeScalar) => false;
    }
}
