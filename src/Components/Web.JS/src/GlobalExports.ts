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
import { Platform, Pointer, System_String, System_Array, System_Object, System_Boolean, System_Byte, System_Int } from './Platform/Platform';
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
    forceCloseConnection?: () => Promise<void>;
    InputFile?: typeof InputFile,
    invokeJSFromDotNet?: (callInfo: Pointer, arg0: any, arg1: any, arg2: any) => any;
    endInvokeDotNetFromJS?: (callId: System_String, success: System_Boolean, resultJsonOrErrorMessage: System_String) => void;
    receiveByteArray?: (id: System_Int, data: System_Array<System_Byte>) => void;
    retrieveByteArray?: () => System_Object;
    getPersistedState?: () => System_String;
    attachRootComponentToElement?: (arg0: any, arg1: any, arg2: any, arg3: any) => void;
    registeredComponents?: {
      getRegisteredComponentsCount: () => number,
      getId: (index) => number,
      getAssembly: (id) => System_String,
      getTypeName: (id) => System_String,
      getParameterDefinitions: (id) => System_String,
      getParameterValues: (id) => any,
    };
    renderBatch?: (browserRendererId: number, batchAddress: Pointer) => void,
    getConfig?: (dotNetFileName: System_String) => System_Object | undefined,
    getApplicationEnvironment?: () => System_String,
    readLazyAssemblies?: () => System_Array<System_Object>,
    readLazyPdbs?: () => System_Array<System_Object>,
    readSatelliteAssemblies?: () => System_Array<System_Object>,
    getLazyAssemblies?: any
    dotNetCriticalError?: any
    getSatelliteAssemblies?: any,
    sendJSDataStream?: (data: any, streamId: number, chunkSize: number) => void,
    getJSDataStreamChunk?: (data: any, position: number, chunkSize: number) => Promise<Uint8Array>,
    receiveDotNetDataStream?: (streamId: number, data: any, bytesRead: number, errorMessage: string) => void,
    attachWebRendererInterop?: typeof attachWebRendererInterop,

    // APIs invoked by hot reload
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
