// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { BootConfigResult } from './BootConfig';
import { Blazor } from '../GlobalExports';

export class WebAssemblyConfigLoader {
  static async initAsync(bootConfigResult: BootConfigResult): Promise<void> {
    Blazor._internal.getApplicationEnvironment = () => bootConfigResult.applicationEnvironment;

    const configFiles = await Promise.all((bootConfigResult.bootConfig.config || [])
      .filter(name => name === 'appsettings.json' || name === `appsettings.${bootConfigResult.applicationEnvironment}.json`)
      .map(async name => ({ name, content: await getConfigBytes(name) })));

    Blazor._internal.getConfig = (fileName: string): Uint8Array | undefined => {
      const resolvedFile = configFiles.find(f => f.name === fileName);
      return resolvedFile ? resolvedFile.content : undefined;
    };

    async function getConfigBytes(file: string): Promise<Uint8Array> {
      const response = await fetch(file, {
        method: 'GET',
        credentials: 'include',
        cache: 'no-cache',
      });

      return new Uint8Array(await response.arrayBuffer());
    }
  }
}
