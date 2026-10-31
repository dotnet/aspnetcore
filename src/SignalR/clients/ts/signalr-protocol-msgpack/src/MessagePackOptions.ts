// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * MessagePack Options per:
 * {@link https://github.com/msgpack/msgpack-javascript#api msgpack-javascript Options}
 */
export interface MessagePackOptions {

  /**
   * @name extensionCodec encoding, decoding extensions: default ExtensionCodec.defaultCodec
   */
  extensionCodec?: any;

  /**
   * @name context user-defined context
   */
  context?: any;

  // encode options

  /**
   * @name maxDepth maximum object depth for encoding
   */
  maxDepth?: number;

  /**
   * @name initialBufferSize starting encode buffer size
   */
  initialBufferSize?: number;

  /**
   * @name sortKeys Force a determinate key order for encoding
   */
  sortKeys?: boolean;

  /**
   * @name forceFloat32 Force floats to be encoded as 32-bit floats
   */
  forceFloat32?: boolean;

  /**
   * @name forceIntegerToFloat Force integers to be encoded as floats
   */
  forceIntegerToFloat?: boolean;

  /**
   * @name ignoreUndefined ignore undefined values when encoding
   */
  ignoreUndefined?: boolean;

  // decode options

  /**
   * @name maxStrLength maximum string decoding length
   */
  maxStrLength?: number;

  /**
   * @name maxBinLength maximum binary decoding length
   */
  maxBinLength?: number;

  /**
   * @name maxArrayLength maximum array decoding length
   */
  maxArrayLength?: number;

  /**
   * @name maxMapLength maximum map decoding length
   */
  maxMapLength?: number;

  /**
   * @name maxExtLength maximum decoding length
   */
  maxExtLength?: number;
}
