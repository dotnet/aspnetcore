// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach, afterEach, jest } from '@jest/globals';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions, CircuitStartOptions, CircuitHandler } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';
import { Blazor } from '../../../src/GlobalExports';
import { DefaultReconnectionHandler } from '../../../src/Platform/Circuits/DefaultReconnectionHandler';
import { ConsoleLogger } from '../../../src/Platform/Logging/Loggers';
import { LogLevel } from '../../../src/Platform/Logging/Logger';
import * as rendererInterop from '../../../src/Rendering/WebRendererInteropMethods';

interface InternalServerPause {
  handleServerInitiatedPause(): Promise<void>;
  pauseCircuit(externalSignal?: AbortSignal): Promise<boolean>;
  pause(remote?: boolean): Promise<boolean>;
}

describe('CircuitManager pause deferral', () => {
  let options: CircuitStartOptions;
  let circuit: CircuitManager;
  let internal: InternalServerPause;
  let order: string[];

  function onPausing(handler: CircuitHandler['onCircuitPausing']) {
    // Deferrals are the onCircuitPausing hooks of the registered circuit handlers.
    options.circuitHandlers.push({ onCircuitPausing: handler });
  }

  beforeEach(() => {
    // Pass a fresh circuitHandlers array so registrations don't leak across tests.
    options = resolveOptions({ circuitHandlers: [] });
    circuit = new CircuitManager({} as never, '', options, { log: () => { /* no-op */ } } as never, new JSEventRegistry());
    internal = circuit as unknown as InternalServerPause;
    order = [];
    // The package-provided pause hook is absent in these tests, so the handler falls back
    // to the circuit's own pause(); stub it to record ordering instead of touching SignalR.
    internal.pause = () => {
      order.push('pause');
      return Promise.resolve(true);
    };
  });

  describe('CircuitManager disposal', () => {
    let connection: HubConnection;
    let options: CircuitStartOptions;
    let circuit: CircuitManager;
    let circuitEvents: EventTarget;
    let previousCircuitEvents: EventTarget | undefined;
    let connectionClosed: ((error?: Error) => void) | undefined;
    const logger = new ConsoleLogger(LogLevel.None);
    const componentManager = {
      initialComponents: [],
      resolveRootComponent: () => { throw new Error('No root components are registered'); },
    };
    const fetchDescriptor = Object.getOwnPropertyDescriptor(globalThis, 'fetch');

    beforeEach(() => {
      previousCircuitEvents = Blazor._internal.circuitEvents;
      circuitEvents = new EventTarget();
      Blazor._internal.circuitEvents = circuitEvents;
      connection = new HubConnectionBuilder().withUrl('http://localhost/_blazor').build();
      jest.spyOn(connection, 'state', 'get').mockReturnValue(HubConnectionState.Connected);
      jest.spyOn(connection, 'start').mockResolvedValue();
      jest.spyOn(connection, 'stop').mockResolvedValue();
      jest.spyOn(connection, 'invoke').mockImplementation(async method => {
        if (method === 'StartCircuit') {
          return 'test-circuit';
        }
        if (method === 'PauseCircuit') {
          return true;
        }
        throw new Error(`Unexpected invocation: ${method}`);
      });
      connectionClosed = undefined;
      jest.spyOn(connection, 'onclose').mockImplementation(callback => { connectionClosed = callback; });
      jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);
      jest.spyOn(rendererInterop, 'detachWebRendererInterop').mockReturnValue({} as never);
      Object.defineProperty(globalThis, 'fetch', { configurable: true, value: jest.fn() });
      options = resolveOptions({
        circuitHandlers: [],
        reconnectionOptions: { maxRetries: 0, retryIntervalMilliseconds: 0, dialogId: 'reconnect-modal' },
      });
      circuit = new CircuitManager(componentManager, '', options, logger, new JSEventRegistry());
    });

    afterEach(() => {
      jest.restoreAllMocks();
      Blazor._internal.circuitEvents = previousCircuitEvents;
      if (fetchDescriptor) {
        Object.defineProperty(globalThis, 'fetch', fetchDescriptor);
      } else {
        Reflect.deleteProperty(globalThis, 'fetch');
      }
    });

    function configureHandler(kind: string) {
      const display = {
        show: jest.fn(),
        update: jest.fn(),
        hide: jest.fn(),
        failed: jest.fn(),
        rejected: jest.fn(),
      };
      const handler = kind === 'default'
        ? new DefaultReconnectionHandler(logger, display, async () => true, async () => true)
        : { onConnectionDown: jest.fn(), onConnectionUp: jest.fn() };
      options.reconnectionHandler = handler;
      return { display, onConnectionUp: jest.spyOn(handler, 'onConnectionUp') };
    }

    test.each(['default', 'custom'])('ordinary connection loss does not trigger pause UI cleanup (%s)', async kind => {
      const { display, onConnectionUp } = configureHandler(kind);
      await circuit.start();
      connectionClosed!(new Error('Connection lost'));

      await circuit.dispose();

      expect(onConnectionUp).not.toHaveBeenCalled();
      expect(display.hide).not.toHaveBeenCalled();
    });

    test.each(['default', 'custom'])('disposal clears an outstanding pause display once (%s)', async kind => {
      const { display, onConnectionUp } = configureHandler(kind);
      await circuit.start();
      expect(await circuit.pauseCircuit()).toBe(true);

      const disposal = circuit.dispose();
      expect(circuit.dispose()).toBe(disposal);
      await disposal;

      expect(onConnectionUp).toHaveBeenCalledTimes(1);
      if (kind === 'default') {
        expect(display.show).toHaveBeenCalledWith({ type: 'pause', remote: false, currentAttempt: 0, secondsToNextAttempt: 0 });
        expect(display.hide).toHaveBeenCalledTimes(1);
      }
    });

    test('publishes retirement once before awaiting startup or creating a replacement', async () => {
      const startup = Promise.withResolvers<void>();
      jest.spyOn(connection, 'start').mockReturnValue(startup.promise);
      const reentrantDisposals: Promise<void>[] = [];
      const disposed = jest.fn(() => { reentrantDisposals.push(circuit.dispose()); });
      circuitEvents.addEventListener('disposed', disposed);
      const starting = circuit.start();
      const disposal = circuit.dispose();
      try {
        expect(circuit.isDisposedOrDisposing()).toBe(true);
        expect(disposed).toHaveBeenCalledTimes(1);
        expect(reentrantDisposals).toEqual([disposal]);
        new CircuitManager(componentManager, '', options, logger, new JSEventRegistry());
      } finally {
        startup.resolve();
        await starting;
        await disposal;
      }
      expect(disposed).toHaveBeenCalledTimes(1);
    });

    test('cleanup callback errors reject the shared disposal promise instead of throwing synchronously', async () => {
      const { onConnectionUp } = configureHandler('custom');
      const error = new Error('Custom cleanup failed');
      onConnectionUp.mockImplementation(() => { throw error; });
      await circuit.start();
      expect(await circuit.pauseCircuit()).toBe(true);

      const disposal = circuit.dispose();

      expect(circuit.dispose()).toBe(disposal);
      await expect(disposal).rejects.toBe(error);
      expect(onConnectionUp).toHaveBeenCalledTimes(1);
    });
  });

  test('awaits a registered pause deferral to completion before pausing', async () => {
    let deferCompleted = false;
    onPausing(async () => {
      order.push('defer-start');
      await Promise.resolve();
      deferCompleted = true;
      order.push('defer-end');
    });

    await internal.handleServerInitiatedPause();

    expect(deferCompleted).toBe(true);
    expect(order).toEqual([
      'defer-start',
      'defer-end',
      'pause',
    ]);
  });

  test('still pauses when no pause deferral is registered', async () => {
    await internal.handleServerInitiatedPause();

    expect(order).toEqual(['pause']);
  });

  test('server-initiated pause fires every registered deferral before pausing', async () => {
    onPausing(() => { order.push('a'); });
    onPausing(() => { order.push('b'); });

    await internal.handleServerInitiatedPause();

    expect(order).toContain('a');
    expect(order).toContain('b');
    expect(order[order.length - 1]).toBe('pause');
  });

  test('client/auto pause fires every registered deferral before pausing', async () => {
    onPausing(() => { order.push('a'); });
    onPausing(() => { order.push('b'); });

    await internal.pauseCircuit();

    expect(order).toContain('a');
    expect(order).toContain('b');
    expect(order[order.length - 1]).toBe('pause');
  });
});
