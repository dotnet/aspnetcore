// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, beforeEach, expect, jest, test } from '@jest/globals';
import { CircuitStartOptions } from '../src/Platform/Circuits/CircuitStartOptions';
import { WebStartOptions } from '../src/Platform/WebStartOptions';

const invalidTimeoutError = 'messageAckTimeoutMilliseconds must be an integer between 0 and 2147483647.';
const originalFetch = globalThis.fetch;

beforeEach(() => {
  jest.resetModules();
});

afterEach(() => {
  jest.restoreAllMocks();
  globalThis.fetch = originalFetch;
});

function createRootComponentManager() {
  return {
    initialComponents: [],
    resolveRootComponent: () => {
      throw new Error('There are no root components in this test.');
    },
  };
}

test('server startup rejects when beforeWebStart supplies an invalid acknowledgement timeout', async () => {
  const { setCircuitOptions, startCircuit, startServer } = await import('../src/Boot.Server.Common');
  const { JSInitializer } = await import('../src/JSInitializers/JSInitializers');
  const { resolveOptions } = await import('../src/Platform/Circuits/CircuitStartOptions');
  const { JSEventRegistry } = await import('../src/Services/JSEventRegistry');

  const circuitOptions = resolveOptions();
  const options: Partial<WebStartOptions> = { circuit: circuitOptions };
  const initializer = new JSInitializer(false);
  const assets = [
    {
      moduleExports: {
        beforeWebStart: (webOptions: WebStartOptions) => {
          webOptions.circuit.messageAckTimeoutMilliseconds = -1;
        },
      },
    },
  ];
  const initializing = initializer.importInitializersAsync(assets, [options]);
  setCircuitOptions(initializing.then(() => circuitOptions));

  const starting = startServer(createRootComponentManager(), new JSEventRegistry());

  await expect(starting).rejects.toThrow(invalidTimeoutError);
  await expect(startCircuit()).rejects.toThrow(invalidTimeoutError);
});

test('server startup rejects when beforeServerStart supplies an invalid acknowledgement timeout in web mode', async () => {
  const { setCircuitOptions, startCircuit, startServer } = await import('../src/Boot.Server.Common');
  const { JSInitializer } = await import('../src/JSInitializers/JSInitializers');
  const { resolveOptions } = await import('../src/Platform/Circuits/CircuitStartOptions');
  const { JSEventRegistry } = await import('../src/Services/JSEventRegistry');

  const circuitOptions = resolveOptions();
  const options: Partial<WebStartOptions> = { circuit: circuitOptions };
  const initializer = new JSInitializer(false);
  const assets = [
    {
      moduleExports: {
        beforeServerStart: (serverOptions: Partial<CircuitStartOptions>) => {
          serverOptions.messageAckTimeoutMilliseconds = -1;
        },
      },
    },
  ];
  await initializer.importInitializersAsync(assets, [options]);
  setCircuitOptions(Promise.resolve(circuitOptions));

  const starting = startServer(createRootComponentManager(), new JSEventRegistry());

  await expect(starting).rejects.toThrow(invalidTimeoutError);
  await expect(startCircuit()).rejects.toThrow(invalidTimeoutError);
});

test.each(['beforeServerStart', 'beforeStart'])('server startup rejects when standalone %s supplies an invalid acknowledgement timeout', async (hookName) => {
  const { setCircuitOptions, startCircuit, startServer } = await import('../src/Boot.Server.Common');
  const { JSInitializer } = await import('../src/JSInitializers/JSInitializers');
  const { resolveOptions } = await import('../src/Platform/Circuits/CircuitStartOptions');
  const { JSEventRegistry } = await import('../src/Services/JSEventRegistry');

  globalThis.fetch = jest.fn(() => Promise.resolve({
    json: () => Promise.resolve(['_content/Test/initializer.js']),
  })) as unknown as typeof fetch;

  const originalImport = JSInitializer.prototype.importInitializersAsync;
  const assets = [
    {
      moduleExports: {
        [hookName]: (serverOptions: Partial<CircuitStartOptions>) => {
          serverOptions.messageAckTimeoutMilliseconds = -1;
        },
      },
    },
  ];
  jest.spyOn(JSInitializer.prototype, 'importInitializersAsync').mockImplementation(function(this, _assets, args) {
    return originalImport.call(this, assets, args);
  });

  setCircuitOptions(Promise.resolve(resolveOptions()));

  const starting = startServer(createRootComponentManager(), new JSEventRegistry());

  await expect(starting).rejects.toThrow(invalidTimeoutError);
  await expect(startCircuit()).rejects.toThrow(invalidTimeoutError);
});
