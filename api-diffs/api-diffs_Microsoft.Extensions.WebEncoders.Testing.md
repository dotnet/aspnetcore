# Microsoft.Extensions.WebEncoders.Testing

``` diff
 namespace Microsoft.Extensions.WebEncoders.Testing {
     public sealed class HtmlTestEncoder : HtmlEncoder {
         public HtmlTestEncoder();
         public override int MaxOutputCharactersPerInputCharacter { get; }
         public override void Encode(TextWriter output, char[] value, int startIndex, int characterCount);
         public override void Encode(TextWriter output, string value, int startIndex, int characterCount);
         public override string Encode(string value);
         public unsafe override int FindFirstCharacterToEncode(char* text, int textLength);
         public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten);
         public override bool WillEncode(int unicodeScalar);
     }
     public class JavaScriptTestEncoder : JavaScriptEncoder {
         public JavaScriptTestEncoder();
         public override int MaxOutputCharactersPerInputCharacter { get; }
         public override void Encode(TextWriter output, char[] value, int startIndex, int characterCount);
         public override void Encode(TextWriter output, string value, int startIndex, int characterCount);
         public override string Encode(string value);
         public unsafe override int FindFirstCharacterToEncode(char* text, int textLength);
         public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten);
         public override bool WillEncode(int unicodeScalar);
     }
     public class UrlTestEncoder : UrlEncoder {
         public UrlTestEncoder();
         public override int MaxOutputCharactersPerInputCharacter { get; }
         public override void Encode(TextWriter output, char[] value, int startIndex, int characterCount);
         public override void Encode(TextWriter output, string value, int startIndex, int characterCount);
         public override string Encode(string value);
         public unsafe override int FindFirstCharacterToEncode(char* text, int textLength);
         public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten);
         public override bool WillEncode(int unicodeScalar);
     }
 }
```

