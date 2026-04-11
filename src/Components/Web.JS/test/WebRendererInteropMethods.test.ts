// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach } from '@jest/globals';
import {
  attachWebRendererInterop,
  detachWebRendererInterop,
  isRendererAttached,
} from '../src/Rendering/WebRendererInteropMethods';

function createMockInteropMethods(label?: string): any {
  return {
    _label: label || 'mock',
    invokeMethodAsync: () => Promise.resolve(),
    dispose: () => {},
    serializeAsArg: () => ({ __dotNetObject: 1 }),
  };
}

const ServerRendererId = 1;

describe('Issue #64738 - reconnect then resume double registration', () => {
  beforeEach(() => {
    if (isRendererAttached(ServerRendererId)) {
      detachWebRendererInterop(ServerRendererId);
    }
  });

  test('CircuitManager.reconnect detaches interop when ConnectCircuit fails, allowing resume to register new renderer', async () => {
    // 1. Server creates initial RemoteRenderer → registers interop for renderer 1
    const initialMethods = createMockInteropMethods('initial-circuit');
    attachWebRendererInterop(ServerRendererId, initialMethods);

    // 2. Connection drops → onclose detaches and saves interop methods
    const savedMethods = detachWebRendererInterop(ServerRendererId);

    // 3–4. CircuitManager.reconnect() runs: re-attaches saved methods,
    //       then ConnectCircuit returns false. With the fix, reconnect()
    //       detaches interop before returning false.
    const { CircuitManager } = await import('../src/Platform/Circuits/CircuitManager');
    const manager: any = Object.create(CircuitManager.prototype);
    manager._circuitId = 'test-circuit-id';
    manager._interopMethodsForReconnection = savedMethods;
    manager._connection = { state: 'Disconnected' };
    manager.startConnection = async () => ({
      state: 'Connected',
      invoke: async (method: string) => {
        if (method === 'ConnectCircuit') {
          return false; // Server rejects — circuit expired
        }
        return null;
      },
    });

    const result = await manager.reconnect();
    expect(result).toBe(false);

    // After the fix, renderer 1 should be detached
    expect(isRendererAttached(ServerRendererId)).toBe(false);

    // 5–6. resume() fallback → server creates new RemoteRenderer whose
    //       constructor calls attachWebRendererInterop(1, newMethods)
    const newCircuitMethods = createMockInteropMethods('new-circuit-from-resume');
    attachWebRendererInterop(ServerRendererId, newCircuitMethods);

    // Must succeed — the new renderer registered without throwing
    expect(isRendererAttached(ServerRendererId)).toBe(true);
  });

  test('CircuitManager.reconnect does not throw when ConnectCircuit fails and renderer is not attached', async () => {
    // This covers the edge case where a custom reconnectionHandler calls
    // Blazor.reconnect() without a prior connection drop, so
    // _interopMethodsForReconnection is undefined and the renderer
    // was never attached on the JS side.
    const { CircuitManager } = await import('../src/Platform/Circuits/CircuitManager');
    const manager: any = Object.create(CircuitManager.prototype);
    manager._circuitId = 'test-circuit-id';
    manager._interopMethodsForReconnection = undefined; // no saved methods
    manager._connection = { state: 'Disconnected' };
    manager.startConnection = async () => ({
      state: 'Connected',
      invoke: async (method: string) => {
        if (method === 'ConnectCircuit') {
          return false;
        }
        return null;
      },
    });

    expect(isRendererAttached(ServerRendererId)).toBe(false);

    // Must not throw — the guard skips detach when renderer isn't attached
    const result = await manager.reconnect();
    expect(result).toBe(false);
    expect(isRendererAttached(ServerRendererId)).toBe(false);
  });
});
