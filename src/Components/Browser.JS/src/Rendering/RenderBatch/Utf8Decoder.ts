const nativeDecoder = typeof TextDecoder === 'function'
  ? new TextDecoder('utf-8')
  : null;

export const decodeUtf8: (bytes: Uint8Array) => string
  = nativeDecoder ? nativeDecoder.decode.bind(nativeDecoder) : decodeImpl;

/* !
Logic in decodeImpl is derived from fast-text-encoding
https://github.com/samthor/fast-text-encoding

License for fast-text-encoding: Apache 2.0
https://github.com/samthor/fast-text-encoding/blob/master/LICENSE
*/

function decodeImpl(bytes: Uint8Array): string {
  let pos = 0;
  const len = bytes.length;
  const out: number[] = [];
  const substrings: string[] = [];

  while (pos < len) {
    const byte1 = bytes[pos++];
    if (byte1 === 0) {
      break; // NULL
    }

    if ((byte1 & 0x80) === 0) { // 1-byte
      out.push(byte1);
    } else if ((byte1 & 0xe0) === 0xc0) { // 2-byte
      const byte2 = bytes[pos++] & 0x3f;
      out.push(((byte1 & 0x1f) << 6) | byte2);
    } else if ((byte1 & 0xf0) === 0xe0) {
      const byte2 = bytes[pos++] & 0x3f;
      const byte3 = bytes[pos++] & 0x3f;
      out.push(((byte1 & 0x1f) << 12) | (byte2 << 6) | byte3);
    } else if ((byte1 & 0xf8) === 0xf0) {
      const byte2 = bytes[pos++] & 0x3f;
      const byte3 = bytes[pos++] & 0x3f;
      const byte4 = bytes[pos++] & 0x3f;

      // this can be > 0xffff, so possibly generate surrogates
      let codepoint = ((byte1 & 0x07) << 0x12) | (byte2 << 0x0c) | (byte3 << 0x06) | byte4;
      if (codepoint > 0xffff) {
        // codepoint &= ~0x10000;
        codepoint -= 0x10000;
        out.push((codepoint >>> 10) & 0x3ff | 0xd800);
        codepoint = 0xdc00 | codepoint & 0x3ff;
      }
      out.push(codepoint);
    } else {
      // FIXME: we're ignoring this
    }

    // As a workaround for https://github.com/samthor/fast-text-encoding/issues/1,
    // make sure the 'out' array never gets too long. When it reaches a limit, we
    // stringify what we have so far and append to a list of outputs.
    if (out.length > 1024) {
      substrings.push(String.fromCharCode.apply(null, out));
      out.length = 0;
    }
  }

  substrings.push(String.fromCharCode.apply(null, out));
  return substrings.join('');
}
