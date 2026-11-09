// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach } from '@jest/globals';
import { capabilityLeaseRegistry } from '../src/Services/CapabilityLeaseRegistry';
import { hasInteractiveRouter, setHasInteractiveRouter, disableInteractiveRouter } from '../src/Services/NavigationUtils';
import { NavigationLock } from '../src/NavigationLock';
import { WebRendererId } from '../src/Rendering/WebRendererId';
import { attachWebRendererInterop, detachWebRendererInterop, isRendererAttached } from '../src/Rendering/WebRendererInteropMethods';

describe('Pattern 3 & 4 - Generational Circuit Lifecycle and Scoped Capability Leases', () => {
  beforeEach(() => {
    capabilityLeaseRegistry.resetForTest();
    if (isRendererAttached(WebRendererId.Server)) {
      detachWebRendererInterop(WebRendererId.Server);
    }
    if (isRendererAttached(WebRendererId.WebAssembly)) {
      detachWebRendererInterop(WebRendererId.WebAssembly);
    }
  });

  test('CapabilityLeaseRegistry releases interactive router interception when last router is removed (#70075 / #50069)', () => {
    expect(hasInteractiveRouter()).toBe(false);

    setHasInteractiveRouter(WebRendererId.Server);
    expect(hasInteractiveRouter()).toBe(true);

    // Attempting to enable from another runtime while active throws.
    expect(() => setHasInteractiveRouter(WebRendererId.WebAssembly)).toThrow(
      'Only one interactive runtime may enable navigation interception at a time.',
    );

    // Releasing the router lease restores server-side navigation and allows another runtime to acquire it later.
    disableInteractiveRouter(WebRendererId.Server);
    expect(hasInteractiveRouter()).toBe(false);

    setHasInteractiveRouter(WebRendererId.WebAssembly);
    expect(hasInteractiveRouter()).toBe(true);
  });

  test('CapabilityLeaseRegistry revokes router, NavigationLock, and LocationChanging leases on circuit disposal', () => {
    setHasInteractiveRouter(WebRendererId.Server);
    NavigationLock.enableNavigationPrompt('lock-1', WebRendererId.Server);
    capabilityLeaseRegistry.setLocationChangingLease(WebRendererId.Server, true);

    expect(hasInteractiveRouter()).toBe(true);
    expect(capabilityLeaseRegistry.hasAnyNavigationPrompts()).toBe(true);
    expect(capabilityLeaseRegistry.hasLocationChangingLease(WebRendererId.Server)).toBe(true);

    capabilityLeaseRegistry.revokeAllForRenderer(WebRendererId.Server);

    expect(hasInteractiveRouter()).toBe(false);
    expect(capabilityLeaseRegistry.hasAnyNavigationPrompts()).toBe(false);
    expect(capabilityLeaseRegistry.hasLocationChangingLease(WebRendererId.Server)).toBe(false);
  });

  test('CircuitManager.resume and pause return false when circuit is disposed or disposing (#69869)', async () => {
    const { CircuitManager } = await import('../src/Platform/Circuits/CircuitManager');
    const manager: any = Object.create(CircuitManager.prototype);
    manager._circuitId = 'disposed-circuit-id';
    manager._disposePromise = Promise.resolve();
    manager._disposed = true;
    manager._renderingFailed = false;

    expect(await manager.pause()).toBe(false);
    expect(await manager.pauseCircuit()).toBe(false);
    expect(await manager.resume()).toBe(false);
    expect(await manager.reconnect()).toBe(false);
  });

  test('CircuitManager.reconnect stops connection and does not short-circuit when ConnectCircuit throws (#69931)', async () => {
    const { CircuitManager } = await import('../src/Platform/Circuits/CircuitManager');
    const manager: any = Object.create(CircuitManager.prototype);
    manager._circuitId = 'test-circuit-id';
    manager._disposePromise = undefined;
    manager._disposed = false;
    manager._renderingFailed = false;
    manager._circuitConnected = false;
    manager._options = {
      reconnectionHandler: {
        onConnectionUp: () => {},
        onConnectionDown: () => {},
      },
    };

    let stopCalled = 0;
    let connectCircuitAttempts = 0;
    manager.startConnection = async () => ({
      state: 'Connected',
      stop: async () => {
        stopCalled++;
      },
      invoke: async (method: string) => {
        if (method === 'ConnectCircuit') {
          connectCircuitAttempts++;
          if (connectCircuitAttempts === 1) {
            throw new Error('Transient hub invocation timeout');
          }
          return true;
        }
        return null;
      },
    });

    // Attempt 1: startConnection succeeds, but ConnectCircuit throws.
    await expect(manager.reconnect()).rejects.toThrow('Transient hub invocation timeout');
    expect(stopCalled).toBe(1);
    expect(manager._circuitConnected).toBe(false);

    // Attempt 2: must NOT short-circuit to true without calling ConnectCircuit.
    const secondResult = await manager.reconnect();
    expect(secondResult).toBe(true);
    expect(connectCircuitAttempts).toBe(2);
    expect(manager._circuitConnected).toBe(true);
  });
});
