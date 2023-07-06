// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function createSymbolOrFallback(fallback: string): symbol | string {
  return typeof Symbol === 'function' ? Symbol() : fallback;
}
