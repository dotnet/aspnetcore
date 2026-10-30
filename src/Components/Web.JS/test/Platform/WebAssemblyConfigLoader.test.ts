// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach, afterEach, jest } from '@jest/globals';
import { WebAssemblyConfigLoader } from '../../src/Platform/WebAssemblyConfigLoader';
import { Blazor } from '../../src/GlobalExports';
import type { MonoConfig } from '@microsoft/dotnet-runtime';

describe('WebAssemblyConfigLoader', () => {
  const originalFetch = global.fetch;

  beforeEach(() => {
    delete (Blazor._internal as any).getConfig;
  });

  afterEach(() => {
    global.fetch = originalFetch;
  });

  test('returns undefined when appsettings is not defined', async () => {
    const fetchMock = jest.fn<typeof fetch>();
    global.fetch = fetchMock;

    const config: MonoConfig = {};
    await WebAssemblyConfigLoader.initAsync(config, {});

    expect(fetchMock).not.toHaveBeenCalled();
    expect(Blazor._internal.getConfig!('appsettings.json')).toBeUndefined();
    expect(config.appsettings).toBeUndefined();
  });

  test('only loads appsettings.json and appsettings.{environment}.json, ignoring other environments', async () => {
    const fetchedUrls: string[] = [];
    global.fetch = jest.fn<typeof fetch>(async (input) => {
      const url = typeof input === 'string' ? input : (input as Request).url;
      fetchedUrls.push(url);
      const buffer = new Uint8Array([1, 2, 3]).buffer;
      return {
        ok: true,
        arrayBuffer: async () => buffer,
      } as any;
    });

    const config: MonoConfig = {
      applicationEnvironment: 'Development',
      appsettings: [
        '../appsettings.Development.json',
        '../appsettings.json',
        '../appsettings.Production.json',
        '../appsettings.Staging.json',
      ],
    };

    await WebAssemblyConfigLoader.initAsync(config, {});

    expect(config.appsettings).toBeUndefined();
    expect(fetchedUrls.length).toBe(2);
    expect(fetchedUrls.some(u => u.includes('appsettings.Development.json'))).toBe(true);
    expect(fetchedUrls.some(u => u.includes('appsettings.json'))).toBe(true);
    expect(fetchedUrls.some(u => u.includes('appsettings.Production.json'))).toBe(false);

    // Verify keying works by simple filename and by original entry
    const devBytes = Blazor._internal.getConfig!('appsettings.Development.json');
    expect(devBytes).toBeDefined();
    expect(Blazor._internal.getConfig!('../appsettings.Development.json')).toBeDefined();

    const baseBytes = Blazor._internal.getConfig!('appsettings.json');
    expect(baseBytes).toBeDefined();
    expect(Blazor._internal.getConfig!('../appsettings.json')).toBeDefined();

    expect(Blazor._internal.getConfig!('appsettings.Production.json')).toBeUndefined();
  });

  test('resolves relative URLs against _framework/ directory', async () => {
    const fetchedUrls: string[] = [];
    global.fetch = jest.fn<typeof fetch>(async (input) => {
      const url = typeof input === 'string' ? input : (input as Request).url;
      fetchedUrls.push(url);
      const buffer = new Uint8Array([1, 2, 3]).buffer;
      return {
        ok: true,
        arrayBuffer: async () => buffer,
      } as any;
    });

    const config: MonoConfig = {
      appsettings: ['../appsettings.json'],
    };

    await WebAssemblyConfigLoader.initAsync(config, {});

    expect(fetchedUrls.length).toBe(1);
    // In jsdom document.baseURI is http://localhost/
    // Resolving '../appsettings.json' against 'http://localhost/_framework/' yields 'http://localhost/appsettings.json'
    expect(fetchedUrls[0]).toBe('http://localhost/appsettings.json');
  });

  test('loadBootResource can override content with custom Response promise', async () => {
    const customBytes = new Uint8Array([4, 5, 6]);
    const config: MonoConfig = {
      appsettings: ['appsettings.json'],
    };

    const loadBootResource = jest.fn(() => {
      return Promise.resolve({
        ok: true,
        arrayBuffer: async () => customBytes.buffer,
      } as any);
    });

    await WebAssemblyConfigLoader.initAsync(config, { loadBootResource: loadBootResource as any });

    expect(loadBootResource).toHaveBeenCalledWith('configuration', 'appsettings.json', expect.any(String), '', 'vfs');
    const bytes = Blazor._internal.getConfig!('appsettings.json');
    expect(bytes).toEqual(customBytes);
  });

  test('loadBootResource returning null falls back to default fetch', async () => {
    const fetchedUrls: string[] = [];
    global.fetch = jest.fn<typeof fetch>(async (input) => {
      const url = typeof input === 'string' ? input : (input as Request).url;
      fetchedUrls.push(url);
      const buffer = new Uint8Array([7, 8, 9]).buffer;
      return {
        ok: true,
        arrayBuffer: async () => buffer,
      } as any;
    });

    const config: MonoConfig = {
      appsettings: ['appsettings.json'],
    };

    const loadBootResource = jest.fn(() => null);

    await WebAssemblyConfigLoader.initAsync(config, { loadBootResource: loadBootResource as any });

    expect(loadBootResource).toHaveBeenCalled();
    expect(fetchedUrls.length).toBe(1);
    const bytes = Blazor._internal.getConfig!('appsettings.json');
    expect(bytes).toBeDefined();
  });
});
