// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { navigateTo, internalFunctions as navigationManagerInternalFunctions, NavigationOptions } from './Services/NavigationManager';
import { domFunctions } from './DomWrapper';
import { Virtualize } from './Virtualize';
import { PageTitle } from './PageTitle';
import { registerCustomEventType, EventTypeOptions } from './Rendering/Events/EventTypes';
import { HubConnection } from '@microsoft/signalr';
import { InputFile } from './InputFile';
import { DefaultReconnectionHandler } from './Platform/Circuits/DefaultReconnectionHandler';
import { CircuitStartOptions } from './Platform/Circuits/CircuitStartOptions';
import { WebAssemblyStartOptions } from './Platform/WebAssemblyStartOptions';
import { Platform, Pointer } from './Platform/Platform';
import { getNextChunk, receiveDotNetDataStream } from './StreamingInterop';
import { RootComponentsFunctions } from './Rendering/JSRootComponents';
import { attachWebRendererInterop } from './Rendering/WebRendererInteropMethods';

interface IBlazor {
  navigateTo: (uri: string, options: NavigationOptions) => void;
  registerCustomEventType: (eventName: string, options: EventTypeOptions) => void;

  disconnect?: () => void;
  reconnect?: (existingConnection?: HubConnection) => Promise<boolean>;
  defaultReconnectionHandler?: DefaultReconnectionHandler;
  start?: ((userOptions?: Partial<CircuitStartOptions>) => Promise<void>) | ((options?: Partial<WebAssemblyStartOptions>) => Promise<void>);
  platform?: Platform;
  rootComponents: typeof RootComponentsFunctions;

  _internal: {
    navigationManager: typeof navigationManagerInternalFunctions | any,
    domWrapper: typeof domFunctions,
    Virtualize: typeof Virtualize,
    PageTitle: typeof PageTitle,
    forceCloseConnection?: () => Promise<void>,
    InputFile?: typeof InputFile,
    invokeJSFromDotNet?: (callInfo: Pointer, arg0: any, arg1: any, arg2: any) => any, // obsolete, legacy, don't use for new code. Use [JSImport] instead
    invokeJSJson?: (identifier: string, targetInstanceId: number, resultType: number, marshalledCallArgsJson: string, marshalledCallAsyncHandle: number) => string | null,
    endInvokeDotNetFromJS?: (callId: string, success: boolean, resultJsonOrErrorMessage: string) => void,
    receiveByteArray?: (id: number, data: Uint8Array) => void,
    retrieveByteArray?: () => Uint8Array,
    getPersistedState?: () => string,
    attachRootComponentToElement?: (arg0: any, arg1: any, arg2: any, arg3: any) => void,
    registeredComponents?: {
      getRegisteredComponentsCount: () => number,
      getId: (index) => number,
      getAssembly: (id) => string,
      getTypeName: (id) => string,
      getParameterDefinitions: (id) => string,
      getParameterValues: (id) => string,
    };
    renderBatch?: (browserRendererId: number, batchAddress: Pointer) => void,
    getConfig?: (fileName: string) => Uint8Array | undefined,
    getApplicationEnvironment?: () => string,
    readLazyAssembly?: (index: number) => Uint8Array | null,
    readLazyPdb?: (index: number) => Uint8Array | null,
    readSatelliteAssembly?: (index: number) => Uint8Array | null,
    getLazyAssemblies?: (assemblies: string[]) => Promise<number>,
    dotNetCriticalError?: (string) => void,
    getSatelliteAssemblies?: (culturesToLoad: string[]) => Promise<number>,
    sendJSDataStream?: (data: ArrayBufferView | Blob, streamId: number, chunkSize: number) => void,
    getJSDataStreamChunk?: (data: ArrayBufferView | Blob, position: number, chunkSize: number) => Promise<Uint8Array>,
    receiveDotNetDataStream?: (streamId: number, data: Uint8Array, bytesRead: number, errorMessage: string) => void,
    attachWebRendererInterop?: typeof attachWebRendererInterop,

    readLazyAssembliesCount?: () => number,
    readSatelliteAssembliesCount?: () => number,
    InvokeDotNet?: (assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number, argsJson: string) => string | null, // obsolete, legacy, don't use for new code. Use [JSExport] instead
    BeginInvokeDotNet?: (callId: string | null, assemblyName: string | null, dotNetObjectId: number, methodIdentifier: string, argsJson: string) => void,
    EndInvokeJS?: (argsJson: string) => void,
    NotifyByteArrayAvailable?: (id: number) => void,

    // APIs invoked by hot reload
    initHotReload?: (url: string) => Promise<void>,
    applyHotReload?: (id: string, metadataDelta: string, ilDelta: string, pdbDelta: string | undefined) => void,
    getApplyUpdateCapabilities?: () => string,
  }
}

export const Blazor: IBlazor = {
  navigateTo,
  registerCustomEventType,
  rootComponents: RootComponentsFunctions,

  _internal: {
    navigationManager: navigationManagerInternalFunctions,
    domWrapper: domFunctions,
    Virtualize,
    PageTitle,
    InputFile,
    getJSDataStreamChunk: getNextChunk,
    receiveDotNetDataStream: receiveDotNetDataStream,
    attachWebRendererInterop,
  },
};

// Make the following APIs available in global scope for invocation from JS
window['Blazor'] = Blazor;
