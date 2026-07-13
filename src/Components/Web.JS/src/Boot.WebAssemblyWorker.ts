// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Runs inside a Web Worker to host the Blazor WebAssembly runtime.
// Render batches are forwarded to the main thread via postMessage.
// DOM events and JS interop calls are proxied across the worker boundary.
//
// Note: the rollup build injects `if (typeof window === 'undefined') { globalThis.window = globalThis; }`
// as an intro before this module so that @microsoft/dotnet-js-interop can initialise
// (it captures `window` at module-load time).

/* eslint-disable @typescript-eslint/no-explicit-any */
import { DotNet } from '@microsoft/dotnet-js-interop';

export type WorkerToMainMessage =
  | { type: 'blazor:workerReady' }
  | { type: 'blazor:error'; message: string }
  | { type: 'blazor:renderBatch'; rendererId: number; batchId: number; batchData: ArrayBuffer }
  | { type: 'blazor:attachRootComponentToElement'; selector: string; componentId: number; rendererId: number }
  | { type: 'blazor:jsCall'; asyncHandle: number; identifier: string; argsJson: string; resultType: number; targetInstanceId: number; callType: number }
  | { type: 'blazor:syncJsCall'; identifier: string; argsJson: string; resultType: number; targetInstanceId: number; callType: number; signal: SharedArrayBuffer; resultBuffer: SharedArrayBuffer }
  | { type: 'blazor:rendererAttached'; rendererId: number }
  | { type: 'blazor:endLocationChanging'; callId: number; shouldContinue: boolean }
  | { type: 'blazor:endUpdateRootComponents'; batchId: number };

export type WorkerRegisteredComponent = {
  assembly: string;
  typeName: string;
  parameterDefinitions: string;
  parameterValues: string;
};

export type MainToWorkerMessage =
  | {
    type: 'blazor:init';
    dotnetJsUrl: string;
    persistedState: string;
    baseUri: string;
    locationHref: string;
    waitForRootComponents: boolean;
    environment?: string;
    applicationCulture?: string;
    environmentVariables: Record<string, string>;
    registeredComponents: WorkerRegisteredComponent[];
  }
  | { type: 'blazor:dispatchEvent'; rendererId: number; eventDescriptor: string; eventArgs: string }
  | { type: 'blazor:initialComponentsUpdate'; operations: string }
  | { type: 'blazor:renderBatchCompleted'; batchId: number; errorMessage?: string }
  | { type: 'blazor:jsCallResult'; serializedArgs: string }
  | { type: 'blazor:locationChanged'; uri: string; state: string | undefined; intercepted: boolean }
  | { type: 'blazor:locationChanging'; callId: number; uri: string; state: string | undefined; intercepted: boolean }
  | { type: 'blazor:updateRootComponents'; operations: string; webAssemblyState: string };

// Per-renderer interop objects keyed by rendererId, set when .NET calls
// Blazor._internal.attachWebRendererInterop from inside the worker.
const rendererInterop: Record<number, { invokeMethodAsync(method: string, ...args: any[]): Promise<any> }> = {};

interface PendingRenderBatch {
  resolve(): void;
  reject(reason?: unknown): void;
}

const pendingRenderBatches = new Map<number, PendingRenderBatch>();

let dotNetExports: {
  BeginInvokeDotNet(callId: string | null, assemblyNameOrObjectId: string, method: string, args: string): void;
  EndInvokeJS(serializedArgs: string): void;
  InvokeDotNet(assembly: string | null, method: string, objectId: number, args: string): string | null;
  ReceiveByteArrayFromJS(id: number, data: Uint8Array): void;
  UpdateRootComponentsCore(operations: string, appState: string): void;
} | null = null;

type WorkerCallDispatcher = DotNet.ICallDispatcher & {
  invokeDotNetMethodAsync<T>(assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, args: any[] | null): Promise<T>;
};

let workerDispatcher: WorkerCallDispatcher | null = null;
let persistedState = '';
let registeredComponents: WorkerRegisteredComponent[] = [];
// The current base URI and location, supplied by the main thread at init time since
// the Worker has no document/location of its own to read them from.
let baseUri = '';
let locationHref = '';
// Populated from the boot config once the runtime is created (see bootBlazorInWorker).
let applicationEnvironment = '';
let applicationCulture = '';
let nextRenderBatchId = 1;
let resolveInitialComponentsUpdate: (value: string) => void;
const initialComponentsUpdatePromise = new Promise<string>(resolve => {
  resolveInitialComponentsUpdate = resolve;
});

const syncJsCallStatusIndex = 0;
const syncJsCallLengthIndex = 1;
const syncJsCallStatusPending = 0;
const syncJsCallStatusSuccess = 1;
const syncJsCallStatusFailure = 2;
const syncJsCallResultBufferSize = 1024 * 1024;
const syncJsCallTimeoutMilliseconds = 30_000;

(self as any).addEventListener('message', async (e: MessageEvent<MainToWorkerMessage>) => {
  const msg = e.data;

  switch (msg.type) {
    case 'blazor:init':
      persistedState = msg.persistedState;
      registeredComponents = msg.registeredComponents;
      baseUri = msg.baseUri;
      locationHref = msg.locationHref;
      await bootBlazorInWorker(msg);
      break;

    case 'blazor:dispatchEvent': {
      // EventDescriptor carries no renderer id of its own; the main thread supplies it
      // separately (captured when it created the event-forwarding proxy for this renderer).
      const descriptor = JSON.parse(msg.eventDescriptor);
      const eventArgs = JSON.parse(msg.eventArgs);
      const interop = rendererInterop[msg.rendererId];
      if (interop) {
        interop.invokeMethodAsync('DispatchEventAsync', descriptor, eventArgs)
          .catch((err: unknown) => console.error('[Blazor Worker] DispatchEventAsync failed:', err));
      }
      break;
    }

    case 'blazor:renderBatchCompleted': {
      const pendingBatch = pendingRenderBatches.get(msg.batchId);
      if (pendingBatch) {
        pendingRenderBatches.delete(msg.batchId);
        if (msg.errorMessage !== undefined) {
          pendingBatch.reject(new Error(msg.errorMessage));
        } else {
          pendingBatch.resolve();
        }
      }
      break;
    }

    case 'blazor:initialComponentsUpdate':
      resolveInitialComponentsUpdate(msg.operations);
      break;

    case 'blazor:jsCallResult':
      // Main thread completed a JS function called from .NET resume the .NET task.
      dotNetExports?.EndInvokeJS(msg.serializedArgs);
      break;

    case 'blazor:locationChanged':
      locationHref = msg.uri;
      workerDispatcher?.invokeDotNetStaticMethodAsync(
        'Microsoft.AspNetCore.Components.WebAssembly',
        'NotifyLocationChanged',
        msg.uri,
        msg.state,
        msg.intercepted,
      ).catch((err: unknown) => console.error('[Blazor Worker] NotifyLocationChanged failed:', err));
      break;

    case 'blazor:locationChanging': {
      const shouldContinue = await workerDispatcher?.invokeDotNetStaticMethodAsync<boolean>(
        'Microsoft.AspNetCore.Components.WebAssembly',
        'NotifyLocationChangingAsync',
        msg.uri,
        msg.state,
        msg.intercepted,
      ).catch(() => true as boolean) ?? true;
      postToMain({ type: 'blazor:endLocationChanging', callId: msg.callId, shouldContinue });
      break;
    }

    case 'blazor:updateRootComponents':
      dotNetExports?.UpdateRootComponentsCore(msg.operations, msg.webAssemblyState);
      break;
  }
});

async function bootBlazorInWorker(init: Extract<MainToWorkerMessage, { type: 'blazor:init' }>): Promise<void> {
  try {
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore – dynamic import, URL resolved by the main thread
    const { dotnet } = await import(/* webpackIgnore: true */ init.dotnetJsUrl);

    // The OOP renderer must be active when running in a Worker so that render batches
    // are serialised to bytes and postMessage'd to the main thread rather than using
    // WASM heap pointers that are only valid on this thread.
    dotnet.withEnvironmentVariable('__BLAZOR_WEBASSEMBLY_OUT_OF_PROCESS_RENDERER', 'true');
    dotnet.withEnvironmentVariables(init.environmentVariables);
    if (init.waitForRootComponents) {
      dotnet.withEnvironmentVariable('__BLAZOR_WEBASSEMBLY_WAIT_FOR_ROOT_COMPONENTS', 'true');
    }
    if (init.environment) {
      dotnet.withApplicationEnvironment(init.environment);
    }
    if (init.applicationCulture) {
      dotnet.withApplicationCulture(init.applicationCulture);
    }

    const runtime = await dotnet.create();

    // The environment/culture come from the boot config; .NET queries them synchronously
    // via Blazor._internal during host startup, so they must be resolved before runMain.
    const runtimeConfig = runtime.getConfig();
    applicationEnvironment = runtimeConfig.applicationEnvironment ?? '';
    applicationCulture = runtimeConfig.applicationCulture ?? '';

    // Retrieve JSExport methods from the Blazor WebAssembly assembly
    const assemblyExports = await runtime.getAssemblyExports('Microsoft.AspNetCore.Components.WebAssembly');
    dotNetExports = assemblyExports.Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime;

    // Create the dispatcher so that DotNet.DotNetObject.invokeMethodAsync works
    // inside this worker (used by rendererInterop for event dispatch).
    workerDispatcher = DotNet.attachDispatcher({
      beginInvokeDotNetFromJS(callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson) {
        const target = dotNetObjectId ? String(dotNetObjectId) : assemblyName!;
        dotNetExports!.BeginInvokeDotNet(callId !== null ? String(callId) : null, target, methodIdentifier, argsJson);
      },
      endInvokeJSFromDotNet(_asyncHandle, _succeeded, serializedArgs) {
        // JS call completed on the main thread; pass the result back into .NET.
        dotNetExports!.EndInvokeJS(serializedArgs);
      },
      sendByteArray(id, data) {
        dotNetExports!.ReceiveByteArrayFromJS(id, data);
      },
      invokeDotNetFromJS(assemblyName, methodIdentifier, dotNetObjectId, argsJson) {
        return dotNetExports!.InvokeDotNet(
          assemblyName ?? null,
          methodIdentifier,
          dotNetObjectId ?? 0,
          argsJson,
        ) ?? '';
      },
    }) as WorkerCallDispatcher;

    // Provide all JSImport implementations that Blazor's .NET code calls into.
    runtime.setModuleImports('blazor-internal', {
      Blazor: { _internal: buildInternalApis() },
    });

    // Start the user's Program entry point. This does not normally complete because
    // WebAssemblyHost.RunAsync keeps the app alive, matching the main-thread startup path.
    runtime.runMain(runtime.getConfig().mainAssemblyName!, [])
      .catch((err: unknown) => {
        console.error('[Blazor Worker] Program failed:', err);
        postToMain({ type: 'blazor:error', message: err instanceof Error ? err.message : String(err) });
      });

    postToMain({ type: 'blazor:workerReady' });
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : String(err);
    console.error('[Blazor Worker] Boot failed:', err);
    postToMain({ type: 'blazor:error', message });
  }
}

function buildInternalApis() {
  return {
    renderBatch() {
      // Shared-memory rendering requires DOM access on the same thread.
      // Worker mode always uses the OOP renderer.
      throw new Error('Shared-memory rendering is not available in Web Worker mode. ' +
        'Set __BLAZOR_WEBASSEMBLY_OUT_OF_PROCESS_RENDERER=true.');
    },

    renderBatchOutOfProcess(rendererId: number, batchData: Uint8Array): Promise<void> {
      // Transfer the bytes to the main thread (zero-copy via transferable).
      const buffer = batchData.buffer.slice(
        batchData.byteOffset,
        batchData.byteOffset + batchData.byteLength,
      ) as ArrayBuffer;

      const batchId = nextRenderBatchId++;
      const renderBatchCompleted = new Promise<void>((resolve, reject) => {
        pendingRenderBatches.set(batchId, { resolve, reject });
      });

      try {
        postToMain(
          { type: 'blazor:renderBatch', rendererId, batchId, batchData: buffer },
          [buffer],
        );
      } catch (err: unknown) {
        pendingRenderBatches.delete(batchId);
        return Promise.reject(err);
      }

      return renderBatchCompleted;
    },

    invokeJSJson(
      identifier: string,
      targetInstanceId: number,
      resultType: number,
      argsJson: string,
      asyncHandle: number,
      callType: number,
    ): string | null {
      if (asyncHandle !== 0) {
        // Async: relay the call to the main thread where window/document exist.
        postToMain({ type: 'blazor:jsCall', asyncHandle, identifier, argsJson, resultType, targetInstanceId, callType });
        return null;
      }

      // Synchronous: only intercept framework-internal calls that we can handle
      // locally; reject everything else because we cannot synchronously round-trip
      // across the worker boundary.
      return handleSyncJSCall(identifier, targetInstanceId, resultType, argsJson, callType);
    },

    endInvokeDotNetFromJS(callId: string, success: boolean, resultJsonOrError: string) {
      // The call being completed (e.g. rendererInterop.invokeMethodAsync) was always
      // started via workerDispatcher, entirely within this Worker so it must be
      // completed here too, not forwarded to the main thread's (unrelated) dispatcher.
      workerDispatcher!.endInvokeDotNetFromJS(callId, success, resultJsonOrError);
    },

    receiveWebAssemblyDotNetDataStream(
      _streamId: number,
      _data: Uint8Array,
      _bytesRead: number,
      _errorMessage: string,
    ) {
      // Streaming interop across the worker boundary is not yet supported.
    },

    receiveByteArray(id: number, data: Uint8Array) {
      dotNetExports?.ReceiveByteArrayFromJS(id, data);
    },

    getApplicationEnvironment: () => applicationEnvironment,
    getApplicationCulture: () => applicationCulture,

    getPersistedState: () => persistedState,

    getInitialComponentsUpdate: (): Promise<string> => initialComponentsUpdatePromise,

    updateRootComponents(operations: string, webAssemblyState: string) {
      dotNetExports?.UpdateRootComponentsCore(operations, webAssemblyState);
    },

    endUpdateRootComponents(batchId: number) {
      postToMain({ type: 'blazor:endUpdateRootComponents', batchId });
    },

    attachRootComponentToElement(
      selector: string,
      componentId: number,
      rendererId: number,
    ) {
      // The target element only exists in the main thread's DOM, so the actual
      // attachment (which creates the BrowserRenderer registry entry that renderBatch
      // depends on) has to happen there rather than in the Worker.
      postToMain({ type: 'blazor:attachRootComponentToElement', selector, componentId, rendererId });
    },

    registeredComponents: {
      getRegisteredComponentsCount: () => registeredComponents.length,
      getAssembly: (id: number) => registeredComponents[id].assembly,
      getTypeName: (id: number) => registeredComponents[id].typeName,
      getParameterDefinitions: (id: number) => registeredComponents[id].parameterDefinitions,
      getParameterValues: (id: number) => registeredComponents[id].parameterValues,
    },

    navigationManager: {
      listenForNavigationEvents: () => { /* main thread drives navigation */ },
      enableNavigationInterception: () => { /* main thread intercepts navigation */ },
      endLocationChanging(callId: number, shouldContinue: boolean) {
        postToMain({ type: 'blazor:endLocationChanging', callId, shouldContinue });
      },
      getBaseURI: (): string => baseUri,
      getLocationHref: (): string => locationHref,
      scrollToElement: () => { /* no-op in worker */ },
      navigateTo: () => { /* no-op; navigation handled on main thread */ },
      refresh: () => { /* no-op */ },
      getHistoryEntryState: (): string | undefined => undefined,
    },

    domWrapper: {
      focus: () => { /* no-op */ },
      focusBySelector: () => { /* no-op */ },
    },

    attachWebRendererInterop(rendererId: number, interopMethods: DotNet.DotNetObject): void {
      // Store the interop object for event dispatch within the worker.
      rendererInterop[rendererId] = interopMethods;
      // Notify the main thread so it can register a forwarding proxy.
      postToMain({ type: 'blazor:rendererAttached', rendererId });
    },

    detachWebRendererInterop(rendererId: number): void {
      delete rendererInterop[rendererId];
    },
  };
}

// Some framework-internal calls arrive synchronously (asyncHandle === 0).
// We intercept the ones we can satisfy locally; everything else is unsupported.
function handleSyncJSCall(identifier: string, targetInstanceId: number, resultType: number, argsJson: string, callType: number): string | null {
  switch (identifier) {
    // attachWebRendererInterop is called synchronously from .NET on WebAssembly.
    // We already provide it as a dedicated JSImport above (via setModuleImports),
    // but the call still arrives here when .NET uses the fast in-process path.
    case 'Blazor._internal.attachWebRendererInterop': {
      const args = parseArgsWithDotNetRefs(argsJson) as [number, DotNet.DotNetObject];
      buildInternalApis().attachWebRendererInterop(args[0], args[1]);
      return null;
    }

    case 'Blazor._internal.detachWebRendererInterop': {
      const [rendererId] = JSON.parse(argsJson) as [number];
      buildInternalApis().detachWebRendererInterop(rendererId);
      return null;
    }

    case 'getCurrentUrl':
      return JSON.stringify(locationHref);

    default:
      if (resultType === 3 && typeof SharedArrayBuffer === 'undefined') {
        // JSVoidResult calls don't need a return value. We can safely forward these
        // to the main thread without blocking the Worker, which preserves common
        // in-process side-effect APIs such as NavigationManager.NavigateTo when
        // cross-origin isolation is not enabled.
        postToMain({ type: 'blazor:jsCall', asyncHandle: 0, identifier, argsJson, resultType, targetInstanceId, callType });
        return null;
      }

      return invokeSyncJSCallOnMainThread(identifier, targetInstanceId, resultType, argsJson, callType);
  }
}

function invokeSyncJSCallOnMainThread(identifier: string, targetInstanceId: number, resultType: number, argsJson: string, callType: number): string | null {
  if (typeof SharedArrayBuffer === 'undefined') {
    throw new Error(`Synchronous JS interop call to '${identifier}' with a return value requires SharedArrayBuffer. ` +
      'Enable cross-origin isolation or use InvokeAsync instead of InvokeMethod.');
  }

  const signal = new SharedArrayBuffer(Int32Array.BYTES_PER_ELEMENT * 2);
  const signalView = new Int32Array(signal);
  const resultBuffer = new SharedArrayBuffer(syncJsCallResultBufferSize);

  Atomics.store(signalView, syncJsCallStatusIndex, syncJsCallStatusPending);
  Atomics.store(signalView, syncJsCallLengthIndex, -1);

  postToMain({ type: 'blazor:syncJsCall', identifier, argsJson, resultType, targetInstanceId, callType, signal, resultBuffer });

  const waitResult = Atomics.wait(
    signalView,
    syncJsCallStatusIndex,
    syncJsCallStatusPending,
    syncJsCallTimeoutMilliseconds,
  );

  if (waitResult === 'timed-out') {
    throw new Error(`Synchronous JS interop call to '${identifier}' timed out in Web Worker mode.`);
  }

  const status = Atomics.load(signalView, syncJsCallStatusIndex);
  const resultLength = Atomics.load(signalView, syncJsCallLengthIndex);
  const resultJson = resultLength < 0
    ? null
    : new TextDecoder().decode(new Uint8Array(resultBuffer, 0, resultLength).slice());

  if (status === syncJsCallStatusSuccess) {
    return resultJson;
  }

  throw new Error(resultJson ?? `Synchronous JS interop call to '${identifier}' failed in Web Worker mode.`);
}

// Minimal JSON reviver that reconstructs DotNet.DotNetObject references so
// that the worker can call .NET instance methods (e.g. DispatchEventAsync).
function parseArgsWithDotNetRefs(argsJson: string): unknown[] {
  return JSON.parse(argsJson, (_key, value: unknown) => {
    if (value !== null && typeof value === 'object' && '__dotNetObject' in value) {
      const id = (value as { __dotNetObject: number }).__dotNetObject;
      // Wrap as a minimal proxy that invokes .NET methods via the worker dispatcher.
      return {
        invokeMethodAsync<T>(method: string, ...args: unknown[]): Promise<T> {
          return workerDispatcher!.invokeDotNetMethodAsync<T>(null, method, id, args as any[]);
        },
      };
    }
    return value;
  }) as unknown[];
}

function postToMain(msg: WorkerToMainMessage, transfer?: Transferable[]): void {
  if (transfer?.length) {
    (self as any).postMessage(msg, transfer);
  } else {
    (self as any).postMessage(msg);
  }
}
