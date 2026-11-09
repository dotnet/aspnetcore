// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach } from '@jest/globals';
import { documentStateStore } from '../src/Services/DocumentStateStore';
import { internalFunctions as navigationManagerFunctions } from '../src/Services/NavigationManager';
import { notifyEnhancedNavigationListeners } from '../src/Services/NavigationUtils';
import { attachWebRendererInterop, detachWebRendererInterop, isRendererAttached } from '../src/Rendering/WebRendererInteropMethods';
import { WebRendererId } from '../src/Rendering/WebRendererId';

function createMockInteropMethods(): any {
  return {
    invokeMethodAsync: () => Promise.resolve(),
    dispose: () => {},
    serializeAsArg: () => ({ __dotNetObject: 1 }),
  };
}

describe('Pattern 1 - DocumentStateStore and Renderer Attach Reconciliation', () => {
  beforeEach(() => {
    documentStateStore.resetForTest();
    if (isRendererAttached(WebRendererId.Server)) {
      detachWebRendererInterop(WebRendererId.Server);
    }
    if (isRendererAttached(WebRendererId.WebAssembly)) {
      detachWebRendererInterop(WebRendererId.WebAssembly);
    }
    history.replaceState(null, '', '/');
  });

  test('discards unclaimed persisted component state when document epoch advances', () => {
    const container = document.createElement('div');
    container.innerHTML = '<!--Blazor-Server-Component-State:cGFnZTE=--><!--Blazor-WebAssembly-Component-State:d2FzbTE=-->';

    documentStateStore.capturePersistedStateFromDocument(container);
    expect(documentStateStore.currentEpoch).toBe(1);

    // An enhanced navigation starts before the circuit or WASM renderer consumes epoch 1's state.
    documentStateStore.advanceEpoch();
    expect(documentStateStore.currentEpoch).toBe(2);

    // The stale state from epoch 1 must not be returned in epoch 2.
    expect(documentStateStore.consumeServerPersistedState()).toBe('');
    expect(documentStateStore.consumeWebAssemblyPersistedState()).toBe('');
  });

  test('consumes persisted state within the same epoch exactly once', () => {
    const container = document.createElement('div');
    container.innerHTML = '<!--Blazor-Server-Component-State:cGFnZTI=-->';

    documentStateStore.capturePersistedStateFromDocument(container);
    expect(documentStateStore.consumeServerPersistedState()).toBe('cGFnZTI=');
    expect(documentStateStore.consumeServerPersistedState()).toBe('');
  });

  test('reconciles enhanced navigation that occurs before Server renderer attaches (#70391)', () => {
    history.replaceState(null, '', '/list');

    const deliveredChanges: Array<{ uri: string; state: string | undefined; intercepted: boolean }> = [];
    navigationManagerFunctions.listenForNavigationEvents(
      WebRendererId.Server,
      async (uri, state, intercepted) => {
        deliveredChanges.push({ uri, state, intercepted });
      },
      async () => {},
    );

    // Simulate StartCircuit recording the start address while renderer is still unattached.
    const startUri = navigationManagerFunctions.getLocationHref();
    documentStateStore.recordRendererInitialLocation(WebRendererId.Server, startUri, undefined);

    // User clicks an enhanced link to /slow before attachWebRendererInterop arrives.
    history.pushState(null, '', '/slow');
    notifyEnhancedNavigationListeners(location.href, true);

    // While unattached, nothing could be delivered yet.
    expect(deliveredChanges.length).toBe(0);

    // Now the server renderer attaches; location reconciliation must fire immediately.
    attachWebRendererInterop(WebRendererId.Server, createMockInteropMethods());

    expect(deliveredChanges.length).toBe(1);
    expect(deliveredChanges[0].uri).toContain('/slow');
    expect(deliveredChanges[0].intercepted).toBe(true);
  });

  test('reconciles enhanced navigation and history userState before WebAssembly renderer attaches (#70266)', () => {
    history.replaceState(null, '', '/home');

    const deliveredChanges: Array<{ uri: string; state: string | undefined; intercepted: boolean }> = [];
    navigationManagerFunctions.listenForNavigationEvents(
      WebRendererId.WebAssembly,
      async (uri, state, intercepted) => {
        deliveredChanges.push({ uri, state, intercepted });
      },
      async () => {},
    );

    // WebAssemblyHostBuilder reads getLocationHref() during startup before renderer attaches.
    const initialHref = navigationManagerFunctions.getLocationHref();
    expect(initialHref).toContain('/home');

    // User navigates to /counter and history state is updated before WebAssemblyRenderer attaches.
    history.pushState({ userState: 'custom-state' }, '', '/counter');
    notifyEnhancedNavigationListeners(location.href, true);

    expect(deliveredChanges.length).toBe(0);

    attachWebRendererInterop(WebRendererId.WebAssembly, createMockInteropMethods());

    expect(deliveredChanges.length).toBe(1);
    expect(deliveredChanges[0].uri).toContain('/counter');
    expect(deliveredChanges[0].state).toBe('custom-state');
    expect(deliveredChanges[0].intercepted).toBe(true);
  });

  test('does not emit redundant locationChanged on attach when location and history state did not change', () => {
    history.replaceState(null, '', '/steady');

    const deliveredChanges: Array<{ uri: string; state: string | undefined; intercepted: boolean }> = [];
    navigationManagerFunctions.listenForNavigationEvents(
      WebRendererId.Server,
      async (uri, state, intercepted) => {
        deliveredChanges.push({ uri, state, intercepted });
      },
      async () => {},
    );

    documentStateStore.recordRendererInitialLocation(WebRendererId.Server, location.href, undefined);

    attachWebRendererInterop(WebRendererId.Server, createMockInteropMethods());

    expect(deliveredChanges.length).toBe(0);
  });
});
