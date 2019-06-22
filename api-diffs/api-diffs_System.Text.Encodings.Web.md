# System.Text.Encodings.Web

``` diff
-namespace System.Text.Encodings.Web {
 {
-    public abstract class HtmlEncoder : TextEncoder {
 {
-        protected HtmlEncoder();

-        public static HtmlEncoder Default { get; }

-        public static HtmlEncoder Create(TextEncoderSettings settings);

-        public static HtmlEncoder Create(params UnicodeRange[] allowedRanges);

-    }
-    public abstract class JavaScriptEncoder : TextEncoder {
 {
-        protected JavaScriptEncoder();

-        public static JavaScriptEncoder Default { get; }

-        public static JavaScriptEncoder Create(TextEncoderSettings settings);

-        public static JavaScriptEncoder Create(params UnicodeRange[] allowedRanges);

-    }
-    public abstract class TextEncoder {
 {
-        protected TextEncoder();

-        public abstract int MaxOutputCharactersPerInputCharacter { get; }

-        public virtual void Encode(TextWriter output, char[] value, int startIndex, int characterCount);

-        public void Encode(TextWriter output, string value);

-        public virtual void Encode(TextWriter output, string value, int startIndex, int characterCount);

-        public virtual string Encode(string value);

-        public unsafe abstract int FindFirstCharacterToEncode(char* text, int textLength);

-        public unsafe abstract bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten);

-        public abstract bool WillEncode(int unicodeScalar);

-    }
-    public class TextEncoderSettings {
 {
-        public TextEncoderSettings();

-        public TextEncoderSettings(TextEncoderSettings other);

-        public TextEncoderSettings(params UnicodeRange[] allowedRanges);

-        public virtual void AllowCharacter(char character);

-        public virtual void AllowCharacters(params char[] characters);

-        public virtual void AllowCodePoints(IEnumerable<int> codePoints);

-        public virtual void AllowRange(UnicodeRange range);

-        public virtual void AllowRanges(params UnicodeRange[] ranges);

-        public virtual void Clear();

-        public virtual void ForbidCharacter(char character);

-        public virtual void ForbidCharacters(params char[] characters);

-        public virtual void ForbidRange(UnicodeRange range);

-        public virtual void ForbidRanges(params UnicodeRange[] ranges);

-        public virtual IEnumerable<int> GetAllowedCodePoints();

-    }
-    public abstract class UrlEncoder : TextEncoder {
 {
-        protected UrlEncoder();

-        public static UrlEncoder Default { get; }

-        public static UrlEncoder Create(TextEncoderSettings settings);

-        public static UrlEncoder Create(params UnicodeRange[] allowedRanges);

-    }
-}
```

