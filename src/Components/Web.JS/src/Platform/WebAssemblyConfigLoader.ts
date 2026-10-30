// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { MonoConfig } from '@microsoft/dotnet-runtime';
import { WebAssemblyStartOptions } from './WebAssemblyStartOptions';
import { Blazor } from '../GlobalExports';

export class WebAssemblyConfigLoader {
  static async initAsync(loadedConfig: MonoConfig, startOptions: Partial<WebAssemblyStartOptions>): Promise<void> {
    const configFiles = new Map<string, Uint8Array>();

    Blazor._internal.getConfig = (fileName: string): Uint8Array | undefined => {
      return configFiles.get(fileName);
    };

    if (!loadedConfig.appsettings || loadedConfig.appsettings.length === 0) {
      return;
    }

    const appsettingsToLoad = Array.from(new Set(loadedConfig.appsettings)).filter(appsetting => {
      const fileName = getFileName(appsetting);
      return shouldLoad(fileName, loadedConfig.applicationEnvironment);
    });

    // Delete loadedConfig.appsettings so dotnet.js will not fetch them into the VFS.
    delete loadedConfig.appsettings;

    await Promise.all(appsettingsToLoad.map(async name => {
      const fileName = getFileName(name);
      const content = await getConfigBytes(name);
      if (content) {
        configFiles.set(fileName, content);
        if (fileName !== name) {
          configFiles.set(name, content);
        }
      }
    }));

    async function getConfigBytes(file: string): Promise<Uint8Array | undefined> {
      let defaultUri = file;
      if (typeof document !== 'undefined' && document.baseURI) {
        const frameworkBase = new URL('_framework/', document.baseURI);
        defaultUri = new URL(file, frameworkBase).href;
      }

      let url = defaultUri;
      if (startOptions.loadBootResource) {
        const customLoadResult = startOptions.loadBootResource('configuration', file, defaultUri, '', 'vfs');
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
          url = typeof document !== 'undefined' && document.baseURI
            ? new URL(customLoadResult, document.baseURI).href
            : customLoadResult;
        }
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

function getFileName(path: string): string {
  const lastSlash = Math.max(path.lastIndexOf('/'), path.lastIndexOf('\\'));
  return lastSlash >= 0 ? path.substring(lastSlash + 1) : path;
}

function shouldLoad(fileName: string, environment?: string): boolean {
  return fileName === 'appsettings.json' || (Boolean(environment) && fileName === `appsettings.${environment}.json`);
}
