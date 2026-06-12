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

    const initializers = resolveLibraryInitializers(loadedConfig);
    await jsInitializer.importInitializersAsync(initializers, initializerArguments);
    return jsInitializer;
  }
}

type WasmResourcesWithInitializers = {
  modulesAfterConfigLoaded?: JSAsset[];
  modulesAfterRuntimeReady?: JSAsset[];
  // Legacy boot config entry, only populated for pre-.NET 8 boot configs.
  libraryInitializers?: JSAsset[] | { [name: string]: string | null };
};

// Resolves the set of Blazor JS library initializer modules ('*.lib.module.js') that Blazor is
// responsible for importing and invoking ('beforeStart'/'afterStarted'/'before|afterWebAssembly*').
function resolveLibraryInitializers(loadedConfig: MonoConfig): JSAsset[] {
  const resources = loadedConfig?.resources as unknown as WasmResourcesWithInitializers | undefined;

  // .NET 8+ boot config: both the Mono and CoreCLR runtime loaders expose library initializers under
  // 'modulesAfterConfigLoaded' (modules exporting beforeStart/afterStarted) and 'modulesAfterRuntimeReady'.
  // Their names are resolved by the runtime relative to the '_framework' folder (hence the leading '../'),
  // whereas Blazor resolves initializer paths relative to 'document.baseURI', so strip a single leading '../'.
  const modules = [
    ...(resources?.modulesAfterConfigLoaded ?? []),
    ...(resources?.modulesAfterRuntimeReady ?? []),
  ].filter(asset => !!asset?.name);
  if (modules.length > 0) {
    return modules.map(asset => ({ ...asset, name: asset.name!.replace(/^\.\.\//, '') }));
  }

  // Fallback for older boot config schemas (< .NET 8) that only populate 'libraryInitializers'.
  const configInitializers = resources?.libraryInitializers;
  if (configInitializers) {
    if (Array.isArray(configInitializers)) {
      // Array form.
      return configInitializers;
    }

    // Dictionary form (name -> hash).
    return Object.keys(configInitializers).map(name => ({ name }));
  }

  return [];
}
