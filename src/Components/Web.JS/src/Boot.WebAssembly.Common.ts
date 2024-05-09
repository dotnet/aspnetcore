// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable array-element-newline */
/* eslint-disable @typescript-eslint/no-non-null-assertion */
import { Blazor } from './GlobalExports';
import * as Environment from './Environment';
import { monoPlatform, dispatcher, getInitializer } from './Platform/Mono/MonoPlatform';
import { renderBatch, getRendererer, attachRootComponentToElement, attachRootComponentToLogicalElement } from './Rendering/Renderer';
import { SharedMemoryRenderBatch } from './Rendering/RenderBatch/SharedMemoryRenderBatch';
import { Pointer } from './Platform/Platform';
import { WebAssemblyStartOptions } from './Platform/WebAssemblyStartOptions';
import { addDispatchEventMiddleware } from './Rendering/WebRendererInteropMethods';
import { WebAssemblyComponentDescriptor, discoverWebAssemblyPersistedState } from './Services/ComponentDescriptorDiscovery';
import { receiveDotNetDataStream } from './StreamingInterop';
import { WebAssemblyComponentAttacher } from './Platform/WebAssemblyComponentAttacher';
import { MonoConfig } from 'dotnet-runtime';
import { RootComponentManager } from './Services/RootComponentManager';
import { WebRendererId } from './Rendering/WebRendererId';

let options: Partial<WebAssemblyStartOptions> | undefined;
let platformLoadPromise: Promise<void> | undefined;
let loadedWebAssemblyPlatform = false;
let started = false;
let firstUpdate = true;
let waitForRootComponents = false;
let startPromise: Promise<void> | undefined;

let resolveBootConfigPromise: (value: MonoConfig) => void;
const bootConfigPromise = new Promise<MonoConfig>(resolve => {
  resolveBootConfigPromise = resolve;
});

let resolveInitialUpdatePromise: (value: string) => void;
const initialUpdatePromise = new Promise<string>(resolve => {
  resolveInitialUpdatePromise = resolve;
});

export function resolveInitialUpdate(value: string): void {
  resolveInitialUpdatePromise(value);
  firstUpdate = false;
}

let resolveInitializersPromise: (value: void) => void;
const initializersPromise = new Promise<void>(resolve => {
  resolveInitializersPromise = resolve;
});

export function isFirstUpdate() {
  return firstUpdate;
}

export function setWaitForRootComponents(): void {
  waitForRootComponents = true;
}

export function setWebAssemblyOptions(initializersReady: Promise<Partial<WebAssemblyStartOptions>>) {
  if (options) {
    throw new Error('WebAssembly options have already been configured.');
  }
  setOptions(initializersReady);


  async function setOptions(initializers: Promise<Partial<WebAssemblyStartOptions>>) {
    const configuredOptions = await initializers;
    options = configuredOptions;
    resolveInitializersPromise();
  }
}

export function startWebAssembly(components: RootComponentManager<WebAssemblyComponentDescriptor>): Promise<void> {
  if (startPromise !== undefined) {
    throw new Error('Blazor WebAssembly has already started.');
  }

  startPromise = new Promise(startCore.bind(null, components));

  return startPromise;
}

async function startCore(components: RootComponentManager<WebAssemblyComponentDescriptor>, resolve, _) {
  if (inAuthRedirectIframe()) {
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    await new Promise(() => { }); // See inAuthRedirectIframe for explanation
  }

  const platformLoadPromise = loadWebAssemblyPlatformIfNotStarted();

  addDispatchEventMiddleware((browserRendererId, eventHandlerId, continuation) => {
    // It's extremely unusual, but an event can be raised while we're in the middle of synchronously applying a
    // renderbatch. For example, a renderbatch might mutate the DOM in such a way as to cause an <input> to lose
    // focus, in turn triggering a 'change' event. It may also be possible to listen to other DOM mutation events
    // that are themselves triggered by the application of a renderbatch.
    const renderer = getRendererer(browserRendererId);
    if (renderer?.eventDelegator.getHandler(eventHandlerId)) {
      monoPlatform.invokeWhenHeapUnlocked(continuation);
    }
  });

  Blazor._internal.applyHotReload = (id: string, metadataDelta: string, ilDelta: string, pdbDelta: string | undefined, updatedTypes?: number[]) => {
    dispatcher.invokeDotNetStaticMethod('Microsoft.AspNetCore.Components.WebAssembly', 'ApplyHotReloadDelta', id, metadataDelta, ilDelta, pdbDelta, updatedTypes ?? null);
  };

  Blazor._internal.getApplyUpdateCapabilities = () => dispatcher.invokeDotNetStaticMethod('Microsoft.AspNetCore.Components.WebAssembly', 'GetApplyUpdateCapabilities');

  // Configure JS interop
  Blazor._internal.invokeJSJson = invokeJSJson;
  Blazor._internal.endInvokeDotNetFromJS = endInvokeDotNetFromJS;
  Blazor._internal.receiveWebAssemblyDotNetDataStream = receiveWebAssemblyDotNetDataStream;
  Blazor._internal.receiveByteArray = receiveByteArray;

  // Configure environment for execution under Mono WebAssembly with shared-memory rendering
  const platform = Environment.setPlatform(monoPlatform);
  Blazor.platform = platform;
  Blazor._internal.renderBatch = (browserRendererId: number, batchAddress: Pointer) => {
    // We're going to read directly from the .NET memory heap, so indicate to the platform
    // that we don't want anything to modify the memory contents during this time. Currently this
    // is only guaranteed by the fact that .NET code doesn't run during this time, but in the
    // future (when multithreading is implemented) we might need the .NET runtime to understand
    // that GC compaction isn't allowed during this critical section.
    const heapLock = monoPlatform.beginHeapLock();
    try {
      renderBatch(browserRendererId, new SharedMemoryRenderBatch(batchAddress));
    } finally {
      heapLock.release();
    }
  };

  Blazor._internal.navigationManager.listenForNavigationEvents(WebRendererId.WebAssembly, async (uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    await dispatcher.invokeDotNetStaticMethodAsync(
      'Microsoft.AspNetCore.Components.WebAssembly',
      'NotifyLocationChanged',
      uri,
      state,
      intercepted
    );
  }, async (callId: number, uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    const shouldContinueNavigation = await dispatcher.invokeDotNetStaticMethodAsync<boolean>(
      'Microsoft.AspNetCore.Components.WebAssembly',
      'NotifyLocationChangingAsync',
      uri,
      state,
      intercepted
    );

    Blazor._internal.navigationManager.endLocationChanging(callId, shouldContinueNavigation);
  });

  // Leverage the time while we are loading boot.config.json from the network to discover any potentially registered component on
  // the document.
  const componentAttacher = new WebAssemblyComponentAttacher(components);
  Blazor._internal.registeredComponents = {
    getRegisteredComponentsCount: () => componentAttacher.getCount(),
    getAssembly: (id) => componentAttacher.getAssembly(id),
    getTypeName: (id) => componentAttacher.getTypeName(id),
    getParameterDefinitions: (id) => componentAttacher.getParameterDefinitions(id) || '',
    getParameterValues: (id) => componentAttacher.getParameterValues(id) || '',
  };

  Blazor._internal.getPersistedState = () => discoverWebAssemblyPersistedState(document) || '';

  Blazor._internal.getInitialComponentsUpdate = () => initialUpdatePromise;

  Blazor._internal.updateRootComponents = (operations: string) =>
    Blazor._internal.dotNetExports?.UpdateRootComponentsCore(operations);

  Blazor._internal.endUpdateRootComponents = (batchId: number) =>
    components.onAfterUpdateRootComponents?.(batchId);

  Blazor._internal.attachRootComponentToElement = (selector, componentId, rendererId) => {
    const element = componentAttacher.resolveRegisteredElement(selector);
    if (!element) {
      attachRootComponentToElement(selector, componentId, rendererId);
    } else {
      attachRootComponentToLogicalElement(rendererId, element, componentId, false);
    }
  };

  try {
    await platformLoadPromise;
    await platform.start();
  } catch (ex) {
    throw new Error(`Failed to start platform. Reason: ${ex}`);
  }

  // Start up the application
  platform.callEntryPoint();
  // At this point .NET has been initialized (and has yielded), we can't await the promise because it will
  // only end when the app finishes running
  const initializer = getInitializer();
  initializer.invokeAfterStartedCallbacks(Blazor);
  started = true;
  resolve();
}

export function hasStartedWebAssembly(): boolean {
  return startPromise !== undefined;
}

export function waitForBootConfigLoaded(): Promise<MonoConfig> {
  return bootConfigPromise;
}

export function loadWebAssemblyPlatformIfNotStarted(): Promise<void> {
  platformLoadPromise ??= (async () => {
    await initializersPromise;
    const finalOptions = options ?? {};
    const existingConfig = options?.configureRuntime;
    finalOptions.configureRuntime = (config) => {
      existingConfig?.(config);
      if (waitForRootComponents) {
        config.withEnvironmentVariable('__BLAZOR_WEBASSEMBLY_WAIT_FOR_ROOT_COMPONENTS', 'true');
      }
    };
    await monoPlatform.load(finalOptions, resolveBootConfigPromise);
    loadedWebAssemblyPlatform = true;
  })();
  return platformLoadPromise;
}

export function hasStartedLoadingWebAssemblyPlatform(): boolean {
  return platformLoadPromise !== undefined;
}

export function hasLoadedWebAssemblyPlatform(): boolean {
  return loadedWebAssemblyPlatform;
}

export function updateWebAssemblyRootComponents(operations: string): void {
  if (!startPromise) {
    throw new Error('Blazor WebAssembly has not started.');
  }

  if (!Blazor._internal.updateRootComponents) {
    throw new Error('Blazor WebAssembly has not initialized.');
  }

  if (!started) {
    scheduleAfterStarted(operations);
  } else {
    Blazor._internal.updateRootComponents(operations);
  }
}

async function scheduleAfterStarted(operations: string): Promise<void> {
  await startPromise;

  if (!Blazor._internal.updateRootComponents) {
    throw new Error('Blazor WebAssembly has not initialized.');
  }

  Blazor._internal.updateRootComponents(operations);
}

function invokeJSJson(identifier: string, targetInstanceId: number, resultType: number, argsJson: string, asyncHandle: number): string | null {
  if (asyncHandle !== 0) {
    dispatcher.beginInvokeJSFromDotNet(asyncHandle, identifier, argsJson, resultType, targetInstanceId);
    return null;
  } else {
    return dispatcher.invokeJSFromDotNet(identifier, argsJson, resultType, targetInstanceId);
  }
}

function endInvokeDotNetFromJS(callId: string, success: boolean, resultJsonOrErrorMessage: string): void {
  dispatcher.endInvokeDotNetFromJS(callId, success, resultJsonOrErrorMessage);
}

function receiveWebAssemblyDotNetDataStream(streamId: number, data: Uint8Array, bytesRead: number, errorMessage: string): void {
  receiveDotNetDataStream(dispatcher, streamId, data, bytesRead, errorMessage);
}

function receiveByteArray(id: number, data: Uint8Array): void {
  dispatcher.receiveByteArray(id, data);
}

function inAuthRedirectIframe(): boolean {
  // We don't want the .NET runtime to start up a second time inside the AuthenticationService.ts iframe. It uses resources
  // unnecessarily and can lead to errors (#37355), plus the behavior is not well defined as the frame will be terminated shortly.
  // So, if we're in that situation, block the startup process indefinitely so that anything chained to Blazor.start never happens.
  // The detection logic here is based on the equivalent check in AuthenticationService.ts.
  // TODO: Later we want AuthenticationService.ts to become responsible for doing this via a JS initializer. Doing it here is a
  //       tactical fix for .NET 6 so we don't have to change how authentication is initialized.
  if (window.parent !== window && !window.opener && window.frameElement) {
    const settingsJson = window.sessionStorage && window.sessionStorage['Microsoft.AspNetCore.Components.WebAssembly.Authentication.CachedAuthSettings'];
    const settings = settingsJson && JSON.parse(settingsJson);
    return settings && settings.redirect_uri && location.href.startsWith(settings.redirect_uri);
  }

  return false;
}
