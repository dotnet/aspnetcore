// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { navigateTo, internalFunctions as navigationManagerInternalFunctions, NavigationOptions } from './Services/NavigationManager';
import { domFunctions } from './DomWrapper';
import { Virtualize } from './Virtualize';
import { PageTitle } from './PageTitle';
import { registerCustomEventType, EventTypeOptions } from './Rendering/Events/EventTypes';
import { HubConnection } from '@microsoft/signalr';
import { InputFile } from './InputFile';
import { NavigationLock } from './NavigationLock';
import { DefaultReconnectionHandler } from './Platform/Circuits/DefaultReconnectionHandler';
import { CircuitStartOptions } from './Platform/Circuits/CircuitStartOptions';
import { WebAssemblyStartOptions } from './Platform/WebAssemblyStartOptions';
import { Platform, Pointer } from './Platform/Platform';
import { getNextChunk } from './StreamingInterop';
import { RootComponentsFunctions } from './Rendering/JSRootComponents';
import { attachWebRendererInterop } from './Rendering/WebRendererInteropMethods';
import { WebStartOptions } from './Platform/WebStartOptions';
import { RuntimeAPI } from '@microsoft/dotnet-runtime';
import { JSEventRegistry } from './Services/JSEventRegistry';

// TODO: It's kind of hard to tell which .NET platform(s) some of these APIs are relevant to.
// It's important to know this information when dealing with the possibility of mulitple .NET platforms being available.
// e.g., which of these APIs need to account for there being multiple .NET runtimes, and which don't?

// We should consider separating it all out so that we can easily identify the platform requirements of each API.
// For example:
// * Blazor.{foo}: public Blazor APIs
// * Blazor._internal.{foo}: internal, platform-agnostic Blazor APIs
// * Blazor.platform.{somePlatformName}.{foo}: public, platform-specific Blazor APIs (would be empty at first, so no initial breaking changes)
// * Blazor.platform.{somePlatformName}.{_internal}.{foo}: internal, platform-specific Blazor APIs
export interface IBlazor {
  navigateTo: (uri: string, options: NavigationOptions) => void;
  registerCustomEventType: (eventName: string, options: EventTypeOptions) => void;

  addEventListener?: typeof JSEventRegistry.prototype.addEventListener;
  removeEventListener?: typeof JSEventRegistry.prototype.removeEventListener;
  disconnect?: () => void;
  reconnect?: (existingConnection?: HubConnection) => Promise<boolean>;
  defaultReconnectionHandler?: DefaultReconnectionHandler;
  start?: ((userOptions?: Partial<CircuitStartOptions>) => Promise<void>) | ((options?: Partial<WebAssemblyStartOptions>) => Promise<void>) | ((options?: Partial<WebStartOptions>) => Promise<void>);
  platform?: Platform;
  rootComponents: typeof RootComponentsFunctions;
  runtime: RuntimeAPI,

  _internal: {
    navigationManager: typeof navigationManagerInternalFunctions | any;
    domWrapper: typeof domFunctions;
    Virtualize: typeof Virtualize;
    PageTitle: typeof PageTitle;
    forceCloseConnection?: () => Promise<void>;
    InputFile?: typeof InputFile;
    NavigationLock: typeof NavigationLock;
    invokeJSJson?: (identifier: string, targetInstanceId: number, resultType: number, argsJson: string, asyncHandle: number) => string | null;
    endInvokeDotNetFromJS?: (callId: string, success: boolean, resultJsonOrErrorMessage: string) => void;
    receiveByteArray?: (id: number, data: Uint8Array) => void;
    getPersistedState?: () => string;
    getInitialComponentsUpdate?: () => Promise<string>;
    updateRootComponents?: (operations: string) => void;
    endUpdateRootComponents?: (batchId: number) => void;
    attachRootComponentToElement?: (arg0: any, arg1: any, arg2: any, arg3: any) => void;
    registeredComponents?: {
      getRegisteredComponentsCount: () => number;
      getAssembly: (id) => string;
      getTypeName: (id) => string;
      getParameterDefinitions: (id) => string;
      getParameterValues: (id) => any;
    };
    renderBatch?: (browserRendererId: number, batchAddress: Pointer) => void;
    getConfig?: (fileName: string) => Uint8Array | undefined;
    getApplicationEnvironment?: () => string;
    dotNetCriticalError?: any;
    loadLazyAssembly?: any;
    loadSatelliteAssemblies?: any;
    sendJSDataStream?: (data: any, streamId: number, chunkSize: number) => void;
    getJSDataStreamChunk?: (data: any, position: number, chunkSize: number) => Promise<Uint8Array>;
    receiveWebAssemblyDotNetDataStream?: (streamId: number, data: any, bytesRead: number, errorMessage: string) => void;
    receiveWebViewDotNetDataStream?: (streamId: number, data: any, bytesRead: number, errorMessage: string) => void;
    attachWebRendererInterop?: typeof attachWebRendererInterop;

    // JSExport APIs
    dotNetExports?: {
      InvokeDotNet: (assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number, argsJson: string) => string | null;
      EndInvokeJS: (argsJson: string) => void;
      BeginInvokeDotNet: (callId: string | null, assemblyNameOrDotNetObjectId: string, methodIdentifier: string, argsJson: string) => void;
      ReceiveByteArrayFromJS: (id: number, data: Uint8Array) => void;
      UpdateRootComponentsCore: (operationsJson: string) => void;
    }

    // APIs invoked by hot reload

    // obsolete:
    applyHotReload?: (id: string, metadataDelta: string, ilDelta: string, pdbDelta: string | undefined, updatedTypes?: number[]) => void;

    applyHotReloadDeltas?: (deltas: { moduleId: string, metadataDelta: string, ilDelta: string, pdbDelta: string, updatedTypes: number[] }[], loggingLevel: number) => {message: string, severity: number}[];
    getApplyUpdateCapabilities?: () => string;
    hotReloadApplied?: () => void;
  }
}

export const Blazor: IBlazor = {
  navigateTo,
  registerCustomEventType,
  rootComponents: RootComponentsFunctions,
  runtime: {} as RuntimeAPI,

  _internal: {
    navigationManager: navigationManagerInternalFunctions,
    domWrapper: domFunctions,
    Virtualize,
    PageTitle,
    InputFile,
    NavigationLock,
    getJSDataStreamChunk: getNextChunk,
    attachWebRendererInterop,
  },
};

// Make the following APIs available in global scope for invocation from JS
window['Blazor'] = Blazor;
