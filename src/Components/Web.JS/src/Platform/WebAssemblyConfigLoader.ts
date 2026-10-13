// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { MonoConfig } from '@microsoft/dotnet-runtime';
import { WebAssemblyStartOptions } from './WebAssemblyStartOptions';
import { Blazor } from '../GlobalExports';

export class WebAssemblyConfigLoader {
  static async initAsync(loadedConfig: MonoConfig, startOptions: Partial<WebAssemblyStartOptions>): Promise<void> {
    const appsettingsToLoad = new Set<string>(loadedConfig.appsettings ?? []);
    appsettingsToLoad.add('appsettings.json');
    if (loadedConfig.applicationEnvironment) {
      appsettingsToLoad.add(`appsettings.${loadedConfig.applicationEnvironment}.json`);
    }

    // Delete loadedConfig.appsettings so dotnet.js will not fetch them into the VFS.
    delete loadedConfig.appsettings;

    const configFiles = new Map<string, Uint8Array>();

    await Promise.all(Array.from(appsettingsToLoad).map(async name => {
      const content = await getConfigBytes(name);
      if (content) {
        configFiles.set(name, content);
      }
    }));

    Blazor._internal.getConfig = (fileName: string): Uint8Array | undefined => {
      return configFiles.get(fileName);
    };

    async function getConfigBytes(file: string): Promise<Uint8Array | undefined> {
      let url = file;
      if (startOptions.loadBootResource) {
        const customLoadResult = startOptions.loadBootResource('configuration', file, file, '', 'vfs');
        if (customLoadResult instanceof Promise) {
          try {
            const response = await customLoadResult;
            if (response.ok) {
              return new Uint8Array(await response.arrayBuffer());
            }
          } catch {
            return undefined;
          }
          return undefined;
        } else if (typeof customLoadResult === 'string') {
          url = customLoadResult;
        }
      }

      if (typeof document !== 'undefined' && document.baseURI) {
        url = new URL(url, document.baseURI).href;
      }

      try {
        const response = await fetch(url, {
          method: 'GET',
          credentials: 'include',
          cache: 'no-cache',
        });

        if (!response.ok) {
          return undefined;
        }

        return new Uint8Array(await response.arrayBuffer());
      } catch {
        return undefined;
      }
    }
  }
}
