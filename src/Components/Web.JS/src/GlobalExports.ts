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
    navigationManager: typeof navigationManagerInternalFunctions | any;
    domWrapper: typeof domFunctions;
    Virtualize: typeof Virtualize;
    PageTitle: typeof PageTitle;
    forceCloseConnection?: () => Promise<void>;
    InputFile?: typeof InputFile;
    NavigationLock: typeof NavigationLock;
    invokeJSFromDotNet?: (callInfo: Pointer, arg0: any, arg1: any, arg2: any) => any;
    invokeJSJson?: (identifier: string, targetInstanceId: number, resultType: number, argsJson: string, asyncHandle: number) => string | null;
    endInvokeDotNetFromJS?: (callId: string, success: boolean, resultJsonOrErrorMessage: string) => void;
    receiveByteArray?: (id: number, data: Uint8Array) => void;
    getPersistedState?: () => string;
    attachRootComponentToElement?: (arg0: any, arg1: any, arg2: any, arg3: any) => void;
    registeredComponents?: {
      getRegisteredComponentsCount: () => number;
      getId: (index) => number;
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
    receiveDotNetDataStream?: (streamId: number, data: any, bytesRead: number, errorMessage: string) => void;
    attachWebRendererInterop?: typeof attachWebRendererInterop;

    // JSExport APIs
    dotNetExports?: {
      InvokeDotNet: (assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number, argsJson: string) => string | null;
      EndInvokeJS: (argsJson: string) => void;
      BeginInvokeDotNet: (callId: string | null, assemblyNameOrDotNetObjectId: string, methodIdentifier: string, argsJson: string) => void;
      ReceiveByteArrayFromJS: (id: number, data: Uint8Array) => void;
    }

    // APIs invoked by hot reload
    initHotReload?: (url: string) => Promise<void>;
    applyHotReload?: (id: string, metadataDelta: string, ilDelta: string, pdbDelta: string | undefined) => void;
    getApplyUpdateCapabilities?: () => string;
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
    NavigationLock,
    getJSDataStreamChunk: getNextChunk,
    receiveDotNetDataStream: receiveDotNetDataStream,
    attachWebRendererInterop,
  },
};

// Make the following APIs available in global scope for invocation from JS
window['Blazor'] = Blazor;
