// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

// https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1
// chunk-ext      = *( BWS ";" BWS chunk-ext-name
//                     [BWS "=" BWS chunk-ext-val] )
//
// chunk-ext-name = token
//
// chunk-ext-val  = token / quoted-string
//
// https://www.rfc-editor.org/info/rfc9110#section-5.6.4
// quoted-string  = DQUOTE *( qdtext / quoted-pair ) DQUOTE
//
// qdtext         = HTAB / SP / %x21 / %x23-5B / %x5D-7E / obs-text
//
// quoted-pair    = "\" ( HTAB / SP / VCHAR / obs-text )
//
// obs-text = %x80-FF
//
// https://www.rfc-editor.org/info/rfc9110/#section-5.6.2
// token          = 1*tchar
//
// https://www.rfc-editor.org/info/rfc9110#section-5.6.3
// BWS            = OWS
//                ; "bad" whitespace
//
// OWS = *(SP / HTAB)
//     ; optional whitespace
//
// https://www.rfc-editor.org/info/rfc9110/#section-5.6.2
// tchar          = "!" / "#" / "$" / "%" / "&" / "'" / "*"
//                / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
//                / DIGIT / ALPHA
//                ; any VCHAR, except delimiters
//
// https://www.rfc-editor.org/info/rfc5234/#appendix-B.1
// HTAB           =  %x09
// SP             =  %x20
// VCHAR          =  %x21-7E
internal struct ChunkedExtensionParser
{
    private const byte ByteCR = (byte)'\r';
    private const byte ByteLF = (byte)'\n';
    private const byte ByteSemicolon = (byte)';';
    private const byte ByteEqual = (byte)'=';
    private const byte ByteDQuote = (byte)'"';
    private const byte ByteBackslash = (byte)'\\';

    private State _state;

    public ChunkedExtensionParser()
        => _state = State.StartOfExtension;

    public bool Consume(ref SequenceReader<byte> reader, out SequencePosition consumed, out SequencePosition examined)
    {
        while (reader.TryRead(out var b))
        {
            switch (_state)
            {
                case State.StartOfExtension:
                    if (IsBadWhitespace(b))
                    {
                        continue;
                    }

                    if (b == ByteSemicolon)
                    {
                        _state = State.BeforeExtensionName;
                        continue;
                    }

                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
                    break;
                case State.BeforeExtensionName:
                    if (IsBadWhitespace(b))
                    {
                        continue;
                    }

                    if (HttpCharacters.IsValidTokenByte(b))
                    {
                        _state = State.InExtensionName;
                        continue;
                    }

                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
                    break;
                case State.InExtensionName:
                    if (HttpCharacters.IsValidTokenByte(b))
                    {
                        _state = State.InExtensionName;
                        continue;
                    }

                    if (IsBadWhitespace(b))
                    {
                        _state = State.BadWhitespaceAfterExtensionName;
                        continue;
                    }

                    if (b == ByteEqual)
                    {
                        _state = State.BeforeExtensionValue;
                        continue;
                    }

                    if (b == ByteSemicolon)
                    {
                        _state = State.BeforeExtensionName;
                        continue;
                    }

                    if (b == ByteCR)
                    {
                        _state = State.WaitTerminatingLF;
                        continue;
                    }

                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
                    break;
                case State.BadWhitespaceAfterExtensionName:
                    if (IsBadWhitespace(b))
                    {
                        continue;
                    }

                    if (b == ByteEqual)
                    {
                        _state = State.BeforeExtensionValue;
                        continue;
                    }

                    if (b == ByteSemicolon)
                    {
                        _state = State.BeforeExtensionName;
                        continue;
                    }

                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
                    break;
                case State.BeforeExtensionValue:
                    if (IsBadWhitespace(b))
                    {
                        continue;
                    }

                    if (HttpCharacters.IsValidTokenByte(b))
                    {
                        _state = State.ExtensionValueToken;
                        continue;
                    }

                    if (b == ByteDQuote)
                    {
                        _state = State.ExtensionValueQuotedString;
                        continue;
                    }

                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
                    break;
                case State.ExtensionValueToken:
                    if (HttpCharacters.IsValidTokenByte(b))
                    {
                        _state = State.ExtensionValueToken;
                        continue;
                    }

                    if (b == ByteCR)
                    {
                        _state = State.WaitTerminatingLF;
                        continue;
                    }

                    _state = State.StartOfExtension;
                    goto case State.StartOfExtension;
                case State.ExtensionValueQuotedString:
                    if (b == ByteBackslash)
                    {
                        _state = State.ExtensionValueQuotedPair;
                        continue;
                    }

                    // qdtext
                    if (b == 0x09 || b == 0x20 || b == 0x21 || (b >= 0x23 && b <= 0x5B) || (b >= 0x5D && b <= 0x7E) || (b >= 0x80 && b <= 0xFF))
                    {
                        continue;
                    }

                    if (b == ByteDQuote)
                    {
                        _state = State.ExtensionValueQuotedStringEnd;
                        continue;
                    }

                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
                    break;
                case State.ExtensionValueQuotedPair:

                    // quoted-pair    = "\" ( HTAB / SP / VCHAR / obs-text )
                    if (b == 0x09 || b == 0x20 || (b >= 0x21 && b <= 0x7E) || (b >= 0x80 && b <= 0xFF))
                    {
                        _state = State.ExtensionValueQuotedString;
                        continue;
                    }

                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
                    break;
                case State.ExtensionValueQuotedStringEnd:

                    if (b == ByteCR)
                    {
                        _state = State.WaitTerminatingLF;
                        continue;
                    }

                    _state = State.StartOfExtension;
                    goto case State.StartOfExtension;

                case State.WaitTerminatingLF:
                    if (b == ByteLF)
                    {
                        _state = State.Completed;
                        consumed = reader.Position;
                        examined = reader.Position;
                        return true;
                    }

                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
                    break;
                default:
                    throw new UnreachableException();
            }
        }

        consumed = reader.Position;
        examined = reader.Position;
        return false;
    }

    private static bool IsBadWhitespace(byte b)
    {
        return b is 0x20 or 0x09;
    }

    private enum State
    {
        /// <summary>
        /// In this state, we wait for BWS followed by a semicolon.
        /// </summary>
        StartOfExtension,

        /// <summary>
        /// In this state, we received the semicolon and are waiting for BWS followed by the extension name.
        /// </summary>
        BeforeExtensionName,

        /// <summary>
        /// In this state, we started processing the extension name.
        /// We consume token (one or more tchar) until we reach a BWS, equal sign, or semicolon.
        /// </summary>
        InExtensionName,

        /// <summary>
        /// In this state, we started receiving BWS after the extension name.
        /// In this case, we can either receive an equal sign or a semicolon after consuming BWS.
        /// </summary>
        BadWhitespaceAfterExtensionName,

        /// <summary>
        /// In this state, we received the equal sign and are waiting for BWS followed by the extension value.
        /// </summary>
        BeforeExtensionValue,

        /// <summary>
        /// In this state, we are processing the extension value as a token (one or more tchar).
        /// </summary>
        ExtensionValueToken,

        /// <summary>
        /// In this state, we are processing the extension value as a quoted string.
        /// We have already consumed the opening double quote, and are processing zero
        /// or more of either qdtext or quoted-pair
        /// </summary>
        ExtensionValueQuotedString,

        /// <summary>
        /// In this state, we received the "\" of a quoted pair and are waiting for the next character.
        /// </summary>
        ExtensionValueQuotedPair,

        /// <summary>
        /// In this state, we received the closing double quote of a quoted string value.
        /// We are waiting for either CRLF or start of new extension.
        /// </summary>
        ExtensionValueQuotedStringEnd,

        /// <summary>
        /// In this state, we received CR, and are waiting for LF.
        /// </summary>
        WaitTerminatingLF,

        /// <summary>
        /// In this state, we completed processing and we should never be called again.
        /// </summary>
        Completed,
    }
}
