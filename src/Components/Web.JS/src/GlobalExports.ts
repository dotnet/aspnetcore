import { navigateTo, internalFunctions as navigationManagerInternalFunctions } from './Services/NavigationManager';
import { domFunctions } from './DomWrapper';
import { Virtualize } from './Virtualize';
import { registerCustomEventType, EventTypeOptions } from './Rendering/Events/EventTypes';
import { HubConnection } from '@microsoft/signalr';
import { InputFile } from './InputFile';
import { DefaultReconnectionHandler } from './Platform/Circuits/DefaultReconnectionHandler';
import { CircuitStartOptions } from './Platform/Circuits/CircuitStartOptions';
import { WebAssemblyStartOptions } from './Platform/WebAssemblyStartOptions';
import { Platform } from './Platform/Platform';
import { Pointer, System_String, System_Array, System_Object } from './Platform/Platform';

interface IBlazor {
  navigateTo: (uri: string, forceLoad: boolean, replace: boolean) => void;
  registerCustomEventType: (eventName: string, options: EventTypeOptions) => void;

  disconnect?: () => void;
  reconnect?: (existingConnection?: HubConnection) => Promise<boolean>;
  defaultReconnectionHandler?: DefaultReconnectionHandler;
  start?: ((userOptions?: Partial<CircuitStartOptions>) => Promise<void>) | ((options?: Partial<WebAssemblyStartOptions>) => Promise<void>);
  platform?: Platform;

  _internal: {
    navigationManager: typeof navigationManagerInternalFunctions | any,
    domWrapper: typeof domFunctions,
    Virtualize: typeof Virtualize,
    forceCloseConnection?: () => Promise<void>;
    InputFile?: typeof InputFile,
    invokeJSFromDotNet?: (callInfo: Pointer, arg0: any, arg1: any, arg2: any) => any;
    getPersistedState?: () => System_String;
    attachRootComponentToElement?: (arg0: any, arg1: any, arg2: any) => void;
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
    applyHotReload?: (id: string, metadataDelta: string, ilDelta: string) => void
  }
}

export const Blazor: IBlazor = {
  navigateTo,
  registerCustomEventType,

  _internal: {
    navigationManager: navigationManagerInternalFunctions,
    domWrapper: domFunctions,
    Virtualize,
  },
};

// Make the following APIs available in global scope for invocation from JS
window['Blazor'] = Blazor;
