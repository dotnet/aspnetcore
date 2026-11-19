// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, jest, test } from '@jest/globals';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

type InternalCircuitManager = {
  _startCircuitUri?: string;
  _connection: {
    send(method: string, ...args: unknown[]): Promise<void>;
  };
  _circuitId?: string;
  disconnect(): Promise<void>;
};

test('a failed startup location replay marks the circuit as failed and disconnects it', async () => {
  const errorUi = document.createElement('div');
  errorUi.id = 'blazor-error-ui';
  document.body.appendChild(errorUi);

  try {
    const circuit = new CircuitManager(
      {} as never,
      '',
      resolveOptions({}),
      { log: () => { /* no-op */ } } as never,
      new JSEventRegistry(),
    );
    const internal = circuit as unknown as InternalCircuitManager;
    const send = Promise.reject(new Error('Location replay failed'));
    internal._startCircuitUri = 'http://localhost/previous';
    internal._circuitId = 'circuit-id';
    internal._connection = { send: () => send };
    const disconnect = jest.fn(() => Promise.resolve());
    internal.disconnect = disconnect;

    circuit.onRendererAttached();
    await expect(send).rejects.toThrow('Location replay failed');
    await Promise.resolve();

    expect(circuit.didRenderingFail()).toBe(true);
    expect(errorUi.style.display).toBe('block');
    expect(disconnect).toHaveBeenCalled();
  } finally {
    errorUi.remove();
  }
});
