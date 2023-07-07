// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These IDs need to be kept in sync with .NET code.
// See classes deriving from 'WebRenderer'.
export enum RendererId {
  Default = 0,
  Server = 1,
  WebAssembly = 2,
  WebView = 3,
}
