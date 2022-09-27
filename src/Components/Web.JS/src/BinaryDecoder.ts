// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const uint64HighPartShift = Math.pow(2, 32);
const maxSafeNumberHighPart = Math.pow(2, 21) - 1; // The high-order int32 from Number.MAX_SAFE_INTEGER

export function readInt32LE(buffer: Uint8Array, position: number): any {
  return (buffer[position])
        | (buffer[position + 1] << 8)
        | (buffer[position + 2] << 16)
        | (buffer[position + 3] << 24);
}

export function readUint32LE(buffer: Uint8Array, position: number): any {
  return (buffer[position])
        + (buffer[position + 1] << 8)
        + (buffer[position + 2] << 16)
        + ((buffer[position + 3] << 24) >>> 0); // The >>> 0 coerces the value to unsigned
}

export function readUint64LE(buffer: Uint8Array, position: number): any {
  // This cannot be done using bit-shift operators in JavaScript, because
  // those all implicitly convert to int32
  const highPart = readUint32LE(buffer, position + 4);
  if (highPart > maxSafeNumberHighPart) {
    throw new Error(`Cannot read uint64 with high order part ${highPart}, because the result would exceed Number.MAX_SAFE_INTEGER.`);
  }

  return (highPart * uint64HighPartShift) + readUint32LE(buffer, position);
}

export function readLEB128(buffer: Uint8Array, position: number): number {
  let result = 0;
  let shift = 0;
  for (let index = 0; index < 4; index++) {
    const byte = buffer[position + index];
    result |= (byte & 127) << shift;
    if (byte < 128) {
      break;
    }
    shift += 7;
  }
  return result;
}

export function numLEB128Bytes(value: number): 1 | 2 | 3 | 4 {
  return value < 128 ? 1
    : value < 16384 ? 2
      : value < 2097152 ? 3 : 4;
}
