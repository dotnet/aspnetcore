// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { BootJsonData } from '../Platform/BootConfig';
import { WebAssemblyStartOptions } from '../Platform/WebAssemblyStartOptions';
import { JSInitializer } from './JSInitializers';

export async function fetchAndInvokeInitializers(bootConfig: BootJsonData, options: Partial<WebAssemblyStartOptions>) : Promise<JSInitializer> {
  const initializers = bootConfig.resources.libraryInitializers;
  const jsInitializer = new JSInitializer();
  if (initializers) {
    await jsInitializer.importInitializersAsync(
      Object.keys(initializers),
      [options, bootConfig.resources.extensions]
    );
  }

  return jsInitializer;
}
