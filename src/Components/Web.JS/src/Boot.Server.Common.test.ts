// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test } from '@jest/globals';
import { setCircuitOptions, startCircuit, startServer } from '../src/Boot.Server.Common';
import { JSInitializer } from '../src/JSInitializers/JSInitializers';
import { resolveOptions } from '../src/Platform/Circuits/CircuitStartOptions';
import { WebStartOptions } from '../src/Platform/WebStartOptions';
import { JSEventRegistry } from '../src/Services/JSEventRegistry';

test('server startup rejects when beforeWebStart supplies an invalid acknowledgement timeout', async () => {
  const options: Partial<WebStartOptions> = { circuit: resolveOptions() };
  const initializer = new JSInitializer(false);
  const initializing = initializer.importInitializersAsync([{
    moduleExports: {
      beforeWebStart: (options: WebStartOptions) => {
        options.circuit.messageAckTimeoutMilliseconds = -1;
      },
    },
  }], [options]);
  setCircuitOptions(initializing.then(() => options.circuit!));

  const starting = startServer({
    initialComponents: [],
    resolveRootComponent: () => { throw new Error('There are no root components in this test.'); },
  }, new JSEventRegistry());

  await expect(starting).rejects.toThrow('messageAckTimeoutMilliseconds must be an integer between 0 and 2147483647.');
  await expect(startCircuit()).rejects.toThrow('messageAckTimeoutMilliseconds must be an integer between 0 and 2147483647.');
}, 1000);
