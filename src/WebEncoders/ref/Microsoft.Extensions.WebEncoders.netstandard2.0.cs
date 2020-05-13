// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class EncoderServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddWebEncoders(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddWebEncoders(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.WebEncoders.WebEncoderOptions> setupAction) { throw null; }
    }
}
namespace Microsoft.Extensions.WebEncoders
{
    public sealed partial class WebEncoderOptions
    {
        public WebEncoderOptions() { }
        public System.Text.Encodings.Web.TextEncoderSettings TextEncoderSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.Extensions.WebEncoders.Testing
{
    public sealed partial class HtmlTestEncoder : System.Text.Encodings.Web.HtmlEncoder
    {
        public HtmlTestEncoder() { }
        public override int MaxOutputCharactersPerInputCharacter { get { throw null; } }
        public override void Encode(System.IO.TextWriter output, char[] value, int startIndex, int characterCount) { }
        public override void Encode(System.IO.TextWriter output, string value, int startIndex, int characterCount) { }
        public override string Encode(string value) { throw null; }
        public unsafe override int FindFirstCharacterToEncode(char* text, int textLength) { throw null; }
        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten) { throw null; }
        public override bool WillEncode(int unicodeScalar) { throw null; }
    }
    public partial class JavaScriptTestEncoder : System.Text.Encodings.Web.JavaScriptEncoder
    {
        public JavaScriptTestEncoder() { }
        public override int MaxOutputCharactersPerInputCharacter { get { throw null; } }
        public override void Encode(System.IO.TextWriter output, char[] value, int startIndex, int characterCount) { }
        public override void Encode(System.IO.TextWriter output, string value, int startIndex, int characterCount) { }
        public override string Encode(string value) { throw null; }
        public unsafe override int FindFirstCharacterToEncode(char* text, int textLength) { throw null; }
        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten) { throw null; }
        public override bool WillEncode(int unicodeScalar) { throw null; }
    }
    public partial class UrlTestEncoder : System.Text.Encodings.Web.UrlEncoder
    {
        public UrlTestEncoder() { }
        public override int MaxOutputCharactersPerInputCharacter { get { throw null; } }
        public override void Encode(System.IO.TextWriter output, char[] value, int startIndex, int characterCount) { }
        public override void Encode(System.IO.TextWriter output, string value, int startIndex, int characterCount) { }
        public override string Encode(string value) { throw null; }
        public unsafe override int FindFirstCharacterToEncode(char* text, int textLength) { throw null; }
        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten) { throw null; }
        public override bool WillEncode(int unicodeScalar) { throw null; }
    }
}
