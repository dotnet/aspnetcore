// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Represents the value of a <c>Content-Disposition</c> header.
/// </summary>
/// <remarks>
/// Note this is for use both in HTTP (<see href="https://tools.ietf.org/html/rfc6266"/>) and MIME (<see href="https://tools.ietf.org/html/rfc2183"/>).
/// </remarks>
public class ContentDispositionHeaderValue
{
    private const string FileNameString = "filename";
    private const string NameString = "name";
    private const string FileNameStarString = "filename*";
    private const string CreationDateString = "creation-date";
    private const string ModificationDateString = "modification-date";
    private const string ReadDateString = "read-date";
    private const string SizeString = "size";
    private const int MaxStackAllocSizeBytes = 256;
    private static readonly char[] QuestionMark = new char[] { '?' };
    private static readonly char[] SingleQuote = new char[] { '\'' };
    private static readonly char[] EscapeChars = new char[] { '\\', '"' };
    private static ReadOnlySpan<byte> MimePrefix => "\"=?utf-8?B?"u8;
    private static ReadOnlySpan<byte> MimeSuffix => "?=\""u8;

    // attr-char definition from RFC5987
    // Same as token except ( "*" / "'" / "%" )
    private static readonly SearchValues<char> Rfc5987AttrChar =
        SearchValues.Create("!#$&+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~");

    private static readonly HttpHeaderParser<ContentDispositionHeaderValue> Parser
        = new GenericHeaderParser<ContentDispositionHeaderValue>(false, GetDispositionTypeLength);

    // Use list instead of dictionary since we may have multiple parameters with the same name.
    private ObjectCollection<NameValueHeaderValue>? _parameters;
    private StringSegment _dispositionType;

    private ContentDispositionHeaderValue()
    {
        // Used by the parser to create a new instance of this type.
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ContentDispositionHeaderValue"/>.
    /// </summary>
    /// <param name="dispositionType">A <see cref="StringSegment"/> that represents a content disposition type.</param>
    public ContentDispositionHeaderValue(StringSegment dispositionType)
    {
        CheckDispositionTypeFormat(dispositionType, "dispositionType");
        _dispositionType = dispositionType;
    }

    /// <summary>
    /// Gets or sets a content disposition type.
    /// </summary>
    public StringSegment DispositionType
    {
        get { return _dispositionType; }
        set
        {
            CheckDispositionTypeFormat(value, "value");
            _dispositionType = value;
        }
    }

    /// <summary>
    /// Gets a collection of parameters included the <c>Content-Disposition</c> header.
    /// </summary>
    public IList<NameValueHeaderValue> Parameters
    {
        get
        {
            if (_parameters == null)
            {
                _parameters = new ObjectCollection<NameValueHeaderValue>();
            }
            return _parameters;
        }
    }

    // Helpers to access specific parameters in the list

    /// <summary>
    /// Gets or sets the name of the content body part.
    /// </summary>
    public StringSegment Name
    {
        get { return GetName(NameString); }
        set { SetName(NameString, value); }
    }

    /// <summary>
    /// Gets or sets a value that suggests how to construct a filename for storing the message payload
    /// to be used if the entity is detached and stored in a separate file.
    /// </summary>
    public StringSegment FileName
    {
        get { return GetName(FileNameString); }
        set { SetName(FileNameString, value); }
    }

    /// <summary>
    /// Gets or sets a value that suggests how to construct filenames for storing message payloads
    /// to be used if the entities are detached and stored in a separate files.
    /// </summary>
    public StringSegment FileNameStar
    {
        get { return GetName(FileNameStarString); }
        set { SetName(FileNameStarString, value); }
    }

    /// <summary>
    /// Gets or sets the <see cref="DateTimeOffset"/> at which the file was created.
    /// </summary>
    public DateTimeOffset? CreationDate
    {
        get { return GetDate(CreationDateString); }
        set { SetDate(CreationDateString, value); }
    }

    /// <summary>
    /// Gets or sets the <see cref="DateTimeOffset"/> at which the file was last modified.
    /// </summary>
    public DateTimeOffset? ModificationDate
    {
        get { return GetDate(ModificationDateString); }
        set { SetDate(ModificationDateString, value); }
    }

    /// <summary>
    /// Gets or sets the <see cref="DateTimeOffset"/> at which the file was last read.
    /// </summary>
    public DateTimeOffset? ReadDate
    {
        get { return GetDate(ReadDateString); }
        set { SetDate(ReadDateString, value); }
    }

    /// <summary>
    /// Gets or sets the approximate size, in bytes, of the file.
    /// </summary>
    public long? Size
    {
        get
        {
            var sizeParameter = NameValueHeaderValue.Find(_parameters, SizeString);
            if (sizeParameter != null)
            {
                var sizeString = sizeParameter.Value;
                if (HeaderUtilities.TryParseNonNegativeInt64(sizeString, out var value))
                {
                    return value;
                }
            }
            return null;
        }
        set
        {
            var sizeParameter = NameValueHeaderValue.Find(_parameters, SizeString);
            if (value == null)
            {
                // Remove parameter
                if (sizeParameter != null)
                {
                    _parameters!.Remove(sizeParameter);
                }
            }
            else if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            else if (sizeParameter != null)
            {
                sizeParameter.Value = value.GetValueOrDefault().ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                var sizeString = value.GetValueOrDefault().ToString(CultureInfo.InvariantCulture);
                Parameters.Add(new NameValueHeaderValue(SizeString, sizeString));
            }
        }
    }

    /// <summary>
    /// Sets both FileName and FileNameStar using encodings appropriate for HTTP headers.
    /// </summary>
    /// <param name="fileName"></param>
    public void SetHttpFileName(StringSegment fileName)
    {
        if (!StringSegment.IsNullOrEmpty(fileName))
        {
            FileName = Sanitize(fileName);
        }
        else
        {
            FileName = fileName;
        }
        FileNameStar = fileName;
    }

    /// <summary>
    /// Sets the FileName parameter using encodings appropriate for MIME headers.
    /// The FileNameStar parameter is removed.
    /// </summary>
    /// <param name="fileName"></param>
    public void SetMimeFileName(StringSegment fileName)
    {
        FileNameStar = null;
        FileName = fileName;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _dispositionType + NameValueHeaderValue.ToString(_parameters, ';', true);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as ContentDispositionHeaderValue;

        if (other == null)
        {
            return false;
        }

        return _dispositionType.Equals(other._dispositionType, StringComparison.OrdinalIgnoreCase) &&
            HeaderUtilities.AreEqualCollections(_parameters, other._parameters);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // The dispositionType string is case-insensitive.
        return StringSegmentComparer.OrdinalIgnoreCase.GetHashCode(_dispositionType) ^ NameValueHeaderValue.GetHashCode(_parameters);
    }

    /// <summary>
    /// Parses <paramref name="input"/> as a <see cref="ContentDispositionHeaderValue"/> value.
    /// </summary>
    /// <param name="input">The values to parse.</param>
    /// <returns>The parsed values.</returns>
    public static ContentDispositionHeaderValue Parse(StringSegment input)
    {
        var index = 0;
        return Parser.ParseValue(input, ref index)!;
    }

    /// <summary>
    /// Attempts to parse the specified <paramref name="input"/> as a <see cref="ContentDispositionHeaderValue"/>.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="parsedValue">The parsed value.</param>
    /// <returns><see langword="true"/> if input is a valid <see cref="ContentDispositionHeaderValue"/>, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(StringSegment input, [NotNullWhen(true)] out ContentDispositionHeaderValue? parsedValue)
    {
        var index = 0;
        return Parser.TryParseValue(input, ref index, out parsedValue!);
    }

    private static int GetDispositionTypeLength(StringSegment input, int startIndex, out ContentDispositionHeaderValue? parsedValue)
    {
        Contract.Requires(startIndex >= 0);

        parsedValue = null;

        if (StringSegment.IsNullOrEmpty(input) || (startIndex >= input.Length))
        {
            return 0;
        }

        // Caller must remove leading whitespaces. If not, we'll return 0.
        var dispositionTypeLength = GetDispositionTypeExpressionLength(input, startIndex, out var dispositionType);

        if (dispositionTypeLength == 0)
        {
            return 0;
        }

        var current = startIndex + dispositionTypeLength;
        current = current + HttpRuleParser.GetWhitespaceLength(input, current);
        var contentDispositionHeader = new ContentDispositionHeaderValue();
        contentDispositionHeader._dispositionType = dispositionType;

        // If we're not done and we have a parameter delimiter, then we have a list of parameters.
        if ((current < input.Length) && (input[current] == ';'))
        {
            current++; // skip delimiter.
            int parameterLength = NameValueHeaderValue.GetNameValueListLength(input, current, ';',
                contentDispositionHeader.Parameters);

            parsedValue = contentDispositionHeader;
            return current + parameterLength - startIndex;
        }

        // We have a ContentDisposition header without parameters.
        parsedValue = contentDispositionHeader;
        return current - startIndex;
    }

    private static int GetDispositionTypeExpressionLength(StringSegment input, int startIndex, out StringSegment dispositionType)
    {
        Contract.Requires((input.Length > 0) && (startIndex < input.Length));

        // This method just parses the disposition type string, it does not parse parameters.
        dispositionType = null;

        // Parse the disposition type, i.e. <dispositiontype> in content-disposition string
        // "<dispositiontype>; param1=value1; param2=value2"
        var typeLength = HttpRuleParser.GetTokenLength(input, startIndex);

        if (typeLength == 0)
        {
            return 0;
        }

        dispositionType = input.Subsegment(startIndex, typeLength);
        return typeLength;
    }

    private static void CheckDispositionTypeFormat(StringSegment dispositionType, string parameterName)
    {
        if (StringSegment.IsNullOrEmpty(dispositionType))
        {
            throw new ArgumentException("An empty string is not allowed.", parameterName);
        }

        // When adding values using strongly typed objects, no leading/trailing LWS (whitespaces) are allowed.
        var dispositionTypeLength = GetDispositionTypeExpressionLength(dispositionType, 0, out var tempDispositionType);
        if ((dispositionTypeLength == 0) || (tempDispositionType.Length != dispositionType.Length))
        {
            throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                "Invalid disposition type '{0}'.", dispositionType));
        }
    }

    // Gets a parameter of the given name and attempts to extract a date.
    // Returns null if the parameter is not present or the format is incorrect.
    private DateTimeOffset? GetDate(string parameter)
    {
        var dateParameter = NameValueHeaderValue.Find(_parameters, parameter);
        if (dateParameter != null)
        {
            var dateString = dateParameter.Value;
            // Should have quotes, remove them.
            if (IsQuoted(dateString))
            {
                dateString = dateString.Subsegment(1, dateString.Length - 2);
            }
            DateTimeOffset date;
            if (HttpRuleParser.TryStringToDate(dateString, out date))
            {
                return date;
            }
        }
        return null;
    }

    // Add the given parameter to the list. Remove if date is null.
    private void SetDate(string parameter, DateTimeOffset? date)
    {
        var dateParameter = NameValueHeaderValue.Find(_parameters, parameter);
        if (date == null)
        {
            // Remove parameter
            if (dateParameter != null)
            {
                _parameters!.Remove(dateParameter);
            }
        }
        else
        {
            // Must always be quoted
            var dateString = HeaderUtilities.FormatDate(date.GetValueOrDefault(), quoted: true);
            if (dateParameter != null)
            {
                dateParameter.Value = dateString;
            }
            else
            {
                Parameters.Add(new NameValueHeaderValue(parameter, dateString));
            }
        }
    }

    // Gets a parameter of the given name and attempts to decode it if necessary.
    // Returns null if the parameter is not present or the raw value if the encoding is incorrect.
    private StringSegment GetName(string parameter)
    {
        var nameParameter = NameValueHeaderValue.Find(_parameters, parameter);
        if (nameParameter != null)
        {
            string? result;
            // filename*=utf-8'lang'%7FMyString
            if (parameter.EndsWith('*'))
            {
                if (TryDecode5987(nameParameter.Value, out result))
                {
                    return result;
                }
                return null; // Unrecognized encoding
            }

            // filename="=?utf-8?B?BDFSDFasdfasdc==?="
            if (TryDecodeMime(nameParameter.Value, out result))
            {
                return result;
            }
            // May not have been encoded
            return HeaderUtilities.RemoveQuotes(nameParameter.Value);
        }
        return null;
    }

    // Add/update the given parameter in the list, encoding if necessary.
    // Remove if value is null/Empty
    private void SetName(StringSegment parameter, StringSegment value)
    {
        var nameParameter = NameValueHeaderValue.Find(_parameters, parameter);
        if (StringSegment.IsNullOrEmpty(value))
        {
            // Remove parameter
            if (nameParameter != null)
            {
                _parameters!.Remove(nameParameter);
            }
        }
        else
        {
            StringSegment processedValue;
            if (parameter.EndsWith("*", StringComparison.Ordinal))
            {
                processedValue = Encode5987(value);
            }
            else
            {
                processedValue = EncodeAndQuoteMime(value);
            }

            if (nameParameter != null)
            {
                nameParameter.Value = processedValue;
            }
            else
            {
                Parameters.Add(new NameValueHeaderValue(parameter, processedValue));
            }
        }
    }

    // Returns input for decoding failures, as the content might not be encoded
    private StringSegment EncodeAndQuoteMime(StringSegment input)
    {
        var result = input;
        var needsQuotes = false;
        // Remove bounding quotes, they'll get re-added later
        if (IsQuoted(result))
        {
            result = result.Subsegment(1, result.Length - 2);
            needsQuotes = true;
        }

        if (RequiresEncoding(result))
        {
            // EncodeMimeWithQuotes will Base64 encode any quotes in the input, and surround the payload in quotes
            // so there is no need to add quotes
            needsQuotes = false;
            result = EncodeMimeWithQuotes(result); // "=?utf-8?B?asdfasdfaesdf?="
        }
        else if (!needsQuotes && HttpRuleParser.GetTokenLength(result, 0) != result.Length)
        {
            needsQuotes = true;
        }

        if (needsQuotes)
        {
            if (result.IndexOfAny(EscapeChars) != -1)
            {
                // '\' and '"' must be escaped in a quoted string
                result = result.ToString().Replace(@"\", @"\\").Replace(@"""", @"\""");
            }
            // Re-add quotes "value"
            result = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", result);
        }
        return result;
    }

    // Replaces characters not suitable for HTTP headers with '_' rather than MIME encoding them.
    private static StringSegment Sanitize(StringSegment input)
    {
        var result = input;

        if (RequiresEncoding(result))
        {
            var builder = new StringBuilder(result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                var c = result[i];
                if ((int)c >= 0x7f || (int)c < 0x20)
                {
                    c = '_'; // Replace out-of-range characters
                }
                builder.Append(c);
            }
            result = builder.ToString();
        }

        return result;
    }

    // Returns true if the value starts and ends with a quote
    private static bool IsQuoted(StringSegment value)
    {
        Contract.Assert(value != null);

        return value.Length > 1 && value.StartsWith("\"", StringComparison.Ordinal)
            && value.EndsWith("\"", StringComparison.Ordinal);
    }

    // tspecials are required to be in a quoted string.  Only non-ascii and control characters need to be encoded.
    private static bool RequiresEncoding(StringSegment input)
    {
        Contract.Assert(input != null);

        return input.AsSpan().IndexOfAnyExceptInRange((char)0x20, (char)0x7e) >= 0;
    }

    // Encode using MIME encoding
    // And adds surrounding quotes, Encoded data must always be quoted, the equals signs are invalid in tokens
    [SkipLocalsInit]
    private string EncodeMimeWithQuotes(StringSegment input)
    {
        var requiredLength = MimePrefix.Length +
            Base64.GetMaxEncodedToUtf8Length(Encoding.UTF8.GetByteCount(input.AsSpan())) +
            MimeSuffix.Length;
        byte[]? bufferFromPool = null;
        Span<byte> buffer = requiredLength <= MaxStackAllocSizeBytes
            ? stackalloc byte[MaxStackAllocSizeBytes]
            : bufferFromPool = ArrayPool<byte>.Shared.Rent(requiredLength);
        buffer = buffer[..requiredLength];

        MimePrefix.CopyTo(buffer);
        var bufferContent = buffer.Slice(MimePrefix.Length);
        var contentLength = Encoding.UTF8.GetBytes(input.AsSpan(), bufferContent);

        Base64.EncodeToUtf8InPlace(bufferContent, contentLength, out var base64ContentLength);

        MimeSuffix.CopyTo(bufferContent.Slice(base64ContentLength));

        var result = Encoding.UTF8.GetString(buffer.Slice(0, MimePrefix.Length + base64ContentLength + MimeSuffix.Length));

        if (bufferFromPool is not null)
        {
            ArrayPool<byte>.Shared.Return(bufferFromPool);
        }

        return result;
    }

    // Attempt to decode MIME encoded strings
    private static bool TryDecodeMime(StringSegment input, [NotNullWhen(true)] out string? output)
    {
        Contract.Assert(input != null);

        output = null;
        var processedInput = input;
        // Require quotes, min of "=?e?b??="
        if (!IsQuoted(processedInput) || processedInput.Length < 10)
        {
            return false;
        }

        var parts = processedInput.Split(QuestionMark).ToArray();
        // "=, encodingName, encodingType, encodedData, ="
        if (parts.Length != 5 || parts[0] != "\"=" || parts[4] != "=\""
            || !parts[2].Equals("b", StringComparison.OrdinalIgnoreCase))
        {
            // Not encoded.
            // This does not support multi-line encoding.
            // Only base64 encoding is supported, not quoted printable
            return false;
        }

        try
        {
            var encoding = Encoding.GetEncoding(parts[1].ToString());
            var bytes = Convert.FromBase64String(parts[3].ToString());
            output = encoding.GetString(bytes, 0, bytes.Length);
            return true;
        }
        catch (ArgumentException)
        {
            // Unknown encoding or bad characters
        }
        catch (FormatException)
        {
            // Bad base64 decoding
        }
        return false;
    }

    // Encode a string using RFC 5987 encoding
    // encoding'lang'PercentEncodedSpecials
    [SkipLocalsInit]
    private static string Encode5987(StringSegment input)
    {
        var builder = new StringBuilder("UTF-8\'\'");
        var remaining = input.AsSpan();
        while (remaining.Length > 0)
        {
            var length = remaining.IndexOfAnyExcept(Rfc5987AttrChar);
            if (length < 0)
            {
                length = remaining.Length;
            }
            builder.Append(remaining[..length]);

            remaining = remaining.Slice(length);
            if (remaining.Length == 0)
            {
                break;
            }

            length = remaining.IndexOfAny(Rfc5987AttrChar);
            if (length < 0)
            {
                length = remaining.Length;
            }

            for (var i = 0; i < length;)
            {
                Rune.DecodeFromUtf16(remaining.Slice(i), out Rune rune, out var runeLength);
                EncodeToUtf8Hex(rune, builder);
                i += runeLength;
            }

            remaining = remaining.Slice(length);
        }

        return builder.ToString();
    }

    private static readonly char[] HexUpperChars = {
                                   '0', '1', '2', '3', '4', '5', '6', '7',
                                   '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

    private static void EncodeToUtf8Hex(Rune rune, StringBuilder builder)
    {
        // Inspired by https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Text/Rune.cs TryEncodeToUtf8
        var value = (uint)rune.Value;
        if (rune.IsAscii)
        {
            var byteValue = (byte)value;
            builder.Append(CultureInfo.InvariantCulture, $"%{HexUpperChars[(byteValue & 0xf0) >> 4]}{HexUpperChars[byteValue & 0xf]}");
        }
        else if (rune.Value <= 0x7FFu)
        {
            // Scalar 00000yyy yyxxxxxx -> bytes [ 110yyyyy 10xxxxxx ]
            var byteValue = (byte)((value + (0b110u << 11)) >> 6);
            builder.Append(CultureInfo.InvariantCulture, $"%{HexUpperChars[(byteValue & 0xf0) >> 4]}{HexUpperChars[byteValue & 0xf]}");
            byteValue = (byte)((value & 0x3Fu) + 0x80u);
            builder.Append(CultureInfo.InvariantCulture, $"%{HexUpperChars[(byteValue & 0xf0) >> 4]}{HexUpperChars[byteValue & 0xf]}");
        }
        else if (rune.Value <= 0xFFFFu)
        {
            // Scalar zzzzyyyy yyxxxxxx -> bytes [ 1110zzzz 10yyyyyy 10xxxxxx ]
            var byteValue = (byte)((value + (0b1110 << 16)) >> 12);
            builder.Append(CultureInfo.InvariantCulture, $"%{HexUpperChars[(byteValue & 0xf0) >> 4]}{HexUpperChars[byteValue & 0xf]}");
            byteValue = (byte)(((value & (0x3Fu << 6)) >> 6) + 0x80u);
            builder.Append(CultureInfo.InvariantCulture, $"%{HexUpperChars[(byteValue & 0xf0) >> 4]}{HexUpperChars[byteValue & 0xf]}");
            byteValue = (byte)((value & 0x3Fu) + 0x80u);
            builder.Append(CultureInfo.InvariantCulture, $"%{HexUpperChars[(byteValue & 0xf0) >> 4]}{HexUpperChars[byteValue & 0xf]}");
        }
        else
        {
            // Scalar 000uuuuu zzzzyyyy yyxxxxxx -> bytes [ 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx ]
            var byteValue = (byte)((value + (0b11110 << 21)) >> 18);
            builder.Append(CultureInfo.InvariantCulture, $"%{HexUpperChars[(byteValue & 0xf0) >> 4]}{HexUpperChars[byteValue & 0xf]}");
            byteValue = (byte)(((value & (0x3Fu << 12)) >> 12) + 0x80u);
            builder.Append(CultureInfo.InvariantCulture, $"%{HexUpperChars[(byteValue & 0xf0) >> 4]}{HexUpperChars[byteValue & 0xf]}");
            byteValue = (byte)(((value & (0x3Fu << 6)) >> 6) + 0x80u);
            builder.Append(CultureInfo.InvariantCulture, $"%{HexUpperChars[(byteValue & 0xf0) >> 4]}{HexUpperChars[byteValue & 0xf]}");
            byteValue = (byte)((value & 0x3Fu) + 0x80u);
            builder.Append(CultureInfo.InvariantCulture, $"%{HexUpperChars[(byteValue & 0xf0) >> 4]}{HexUpperChars[byteValue & 0xf]}");
        }
    }

    // Attempt to decode using RFC 5987 encoding.
    // encoding'language'my%20string
    private static bool TryDecode5987(StringSegment input, [NotNullWhen(true)] out string? output)
    {
        output = null;

        var parts = input.Split(SingleQuote).ToArray();
        if (parts.Length != 3)
        {
            return false;
        }

        var decoded = new StringBuilder();
        byte[]? unescapedBytes = null;
        try
        {
            var encoding = Encoding.GetEncoding(parts[0].ToString());

            var dataString = parts[2];
            unescapedBytes = ArrayPool<byte>.Shared.Rent(dataString.Length);
            var unescapedBytesCount = 0;
            for (var index = 0; index < dataString.Length; index++)
            {
                if (IsHexEncoding(dataString, index)) // %FF
                {
                    // Unescape and cache bytes, multi-byte characters must be decoded all at once
                    unescapedBytes[unescapedBytesCount++] = HexUnescape(dataString, ref index);
                    index--; // HexUnescape did +=3; Offset the for loop's ++
                }
                else
                {
                    if (unescapedBytesCount > 0)
                    {
                        // Decode any previously cached bytes
                        decoded.Append(encoding.GetString(unescapedBytes, 0, unescapedBytesCount));
                        unescapedBytesCount = 0;
                    }
                    decoded.Append(dataString[index]); // Normal safe character
                }
            }

            if (unescapedBytesCount > 0)
            {
                // Decode any previously cached bytes
                decoded.Append(encoding.GetString(unescapedBytes, 0, unescapedBytesCount));
            }
        }
        catch (ArgumentException)
        {
            return false; // Unknown encoding or bad characters
        }
        finally
        {
            if (unescapedBytes != null)
            {
                ArrayPool<byte>.Shared.Return(unescapedBytes);
            }
        }

        output = decoded.ToString();
        return true;
    }

    private static bool IsHexEncoding(StringSegment pattern, int index)
    {
        if ((pattern.Length - index) < 3)
        {
            return false;
        }
        if ((pattern[index] == '%') && IsEscapedAscii(pattern[index + 1], pattern[index + 2]))
        {
            return true;
        }
        return false;
    }

    private static bool IsEscapedAscii(char digit, char next)
    {
        if (!(((digit >= '0') && (digit <= '9'))
            || ((digit >= 'A') && (digit <= 'F'))
            || ((digit >= 'a') && (digit <= 'f'))))
        {
            return false;
        }

        if (!(((next >= '0') && (next <= '9'))
            || ((next >= 'A') && (next <= 'F'))
            || ((next >= 'a') && (next <= 'f'))))
        {
            return false;
        }

        return true;
    }

    private static byte HexUnescape(StringSegment pattern, ref int index)
    {
        if ((index < 0) || (index >= pattern.Length))
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        if ((pattern[index] == '%')
            && (pattern.Length - index >= 3))
        {
            var ret = UnEscapeAscii(pattern[index + 1], pattern[index + 2]);
            index += 3;
            return ret;
        }
        return (byte)pattern[index++];
    }

    internal static byte UnEscapeAscii(char digit, char next)
    {
        if (!(((digit >= '0') && (digit <= '9'))
            || ((digit >= 'A') && (digit <= 'F'))
            || ((digit >= 'a') && (digit <= 'f'))))
        {
            throw new ArgumentOutOfRangeException(nameof(digit));
        }

        var res = (digit <= '9')
            ? ((int)digit - (int)'0')
            : (((digit <= 'F')
            ? ((int)digit - (int)'A')
            : ((int)digit - (int)'a'))
               + 10);

        if (!(((next >= '0') && (next <= '9'))
            || ((next >= 'A') && (next <= 'F'))
            || ((next >= 'a') && (next <= 'f'))))
        {
            throw new ArgumentOutOfRangeException(nameof(next));
        }

        return (byte)((res << 4) + ((next <= '9')
                ? ((int)next - (int)'0')
                : (((next <= 'F')
                    ? ((int)next - (int)'A')
                    : ((int)next - (int)'a'))
                   + 10)));
    }
}
