// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { BootConfigResult } from './BootConfig';
import { WebAssemblyStartOptions } from './WebAssemblyStartOptions';
import { Blazor } from '../GlobalExports';

export class WebAssemblyConfigLoader {
  static async initAsync(bootConfigResult: BootConfigResult, startOptions: Partial<WebAssemblyStartOptions>): Promise<void> {
    Blazor._internal.getApplicationEnvironment = () => bootConfigResult.applicationEnvironment;

    const configFiles = await Promise.all((bootConfigResult.bootConfig.config || [])
      .filter(name => name === 'appsettings.json' || name === `appsettings.${bootConfigResult.applicationEnvironment}.json`)
      .map(async name => ({ name, content: await getConfigBytes(name) })));

    Blazor._internal.getConfig = (fileName: string) : Uint8Array | undefined => {
      const resolvedFile = configFiles.find(f => f.name === fileName);
      return resolvedFile ? resolvedFile.content : undefined;
    };

    async function getConfigBytes(file: string): Promise<Uint8Array> {
      // Allow developers to override how the config is loaded
      if (startOptions.loadBootResource) {
        const customLoadResult = startOptions.loadBootResource('configuration', file, file, '');
        if (customLoadResult instanceof Promise) {
          // They are supplying an entire custom response, so just use that
          return new Uint8Array(await (await customLoadResult).arrayBuffer());
        } else if (typeof customLoadResult === 'string') {
          // They are supplying a custom URL, so use that with the default fetch behavior
          file = customLoadResult;
        }
      }

      const response = await fetch(file, {
        method: 'GET',
        credentials: 'include',
        cache: 'no-cache',
      });

      return new Uint8Array(await response.arrayBuffer());
    }
  }
}
