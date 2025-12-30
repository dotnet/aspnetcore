// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { MonoConfig } from '@microsoft/dotnet-runtime';
import { WebAssemblyStartOptions } from '../Platform/WebAssemblyStartOptions';
import { WebRendererId } from '../Rendering/WebRendererId';
import { JSAsset, JSInitializer } from './JSInitializers';

export async function fetchAndInvokeInitializers(options: Partial<WebAssemblyStartOptions>, loadedConfig: MonoConfig): Promise<JSInitializer> {
  if (options.initializers) {
    // Initializers were already resolved, so we don't have to fetch them, we just invoke the beforeStart ones
    // and return the list of afterStarted ones so they get processed in the same way as in traditional Blazor Server.
    await Promise.all(options.initializers.beforeStart.map(i => i(options)));
    return new JSInitializer(/* singleRuntime: */ false, undefined, options.initializers.afterStarted, WebRendererId.WebAssembly);
  } else {
    const initializerArguments = [options, loadedConfig.resources?.extensions ?? {}];
    const jsInitializer = new JSInitializer(
      /* singleRuntime: */ true,
      undefined,
      undefined,
      WebRendererId.WebAssembly
    );

    const configInitializers = loadedConfig?.resources?.['libraryInitializers'];
    let initializers : JSAsset[];
    if (!configInitializers) {
      initializers = [];
    }
    else if ("length" in configInitializers) {
      // New boot config schema.
      initializers = configInitializers;
    } else {
      // Old boot config schema.
      initializers = Object.keys(configInitializers).map(name => ({
        name,
      })) as JSAsset[];
    }

    await jsInitializer.importInitializersAsync(initializers, initializerArguments);
    return jsInitializer;
  }
}
