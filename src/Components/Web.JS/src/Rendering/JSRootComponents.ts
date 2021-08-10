import { DotNet } from '@microsoft/dotnet-js-interop';

const pendingRootComponentContainerNamePrefix = '__bl-dynamic-root:';
const pendingRootComponentContainers = new Map<string, Element>();
let nextPendingDynamicRootComponentIdentifier = 0;

type ComponentParameters = object | null | undefined;

let manager: DotNet.DotNetObject | undefined;
let jsComponentParametersByIdentifier: JSComponentParametersByIdentifier;

// These are the public APIs at Blazor.rootComponents.*
export const RootComponentsFunctions = {
  async add(toElement: Element, componentIdentifier: string, initialParameters: ComponentParameters): Promise<DynamicRootComponent> {
    if (!initialParameters) {
      throw new Error('initialParameters must be an object, even if empty.');
    }

    // Track the container so we can use it when the component gets attached to the document via a selector
    const containerIdentifier = pendingRootComponentContainerNamePrefix + (++nextPendingDynamicRootComponentIdentifier).toString();
    pendingRootComponentContainers.set(containerIdentifier, toElement);

    // Instruct .NET to add and render the new root component
    const componentId = await getRequiredManager().invokeMethodAsync<number>(
      'AddRootComponent', componentIdentifier, containerIdentifier);
    const component = new DynamicRootComponent(componentId, jsComponentParametersByIdentifier[componentIdentifier]);
    await component.setParameters(initialParameters);
    return component;
  },
};

export function getAndRemovePendingRootComponentContainer(containerIdentifier: string): Element | undefined {
  const container = pendingRootComponentContainers.get(containerIdentifier);
  if (container) {
    pendingRootComponentContainers.delete(containerIdentifier);
    return container;
  }
}

class EventCallbackWrapper {
  private _wrapper: { func: any };

  private readonly _jsObjectReference: any;

  constructor(eventCallback: any) {
    this._wrapper = { func: eventCallback };
    this._jsObjectReference = DotNet.createJSObjectReference(this._wrapper);
  }

  setCallback(eventCallback: any) {
    this._wrapper.func = eventCallback;
  }

  getJSObjectReference() {
    return this._jsObjectReference;
  }

  dispose() {
    DotNet.disposeJSObjectReference(this._jsObjectReference);
  }
}

class DynamicRootComponent {
  private _componentId: number | null;

  private _jsEventCallbackWrappers = new Map<string, EventCallbackWrapper | null>();

  constructor(componentId: number, parameters: JSComponentParameter[]) {
    this._componentId = componentId;

    for (const parameter of parameters) {
      if (parameter.type === 'eventcallback') {
        // We lazily initialize event callback wrappers, so null entries are added for now.
        this._jsEventCallbackWrappers.set(parameter.name.toLowerCase(), null);
      }
    }
  }

  setParameters(parameters: ComponentParameters) {
    parameters = parameters || {};

    const keys = Object.keys(parameters);
    const parameterCount = keys.length;

    // We iterate over the provided parameters to modify event callback values.
    for (const key of keys) {
      const keyLowerCase = key.toLowerCase();
      let callbackWrapper = this._jsEventCallbackWrappers.get(keyLowerCase);
      if (callbackWrapper === undefined) {
        // Not an event callback.
        continue;
      }

      const callback = parameters[key];
      if (callbackWrapper === null) {
        // This is the first time this parameter has been provided.
        // Create the wrapper, then replace the existing parameter value with the JS object reference.
        callbackWrapper = new EventCallbackWrapper(callback);
        this._jsEventCallbackWrappers.set(keyLowerCase, callbackWrapper);
        parameters[key] = callbackWrapper.getJSObjectReference();
      } else {
        // This parameter has been seen before. Since the existing JS object reference is still valid in .NET,
        // we can update the callback on the JS side and skip this parameter.
        callbackWrapper.setCallback(callback);
        delete parameters[key];
      }
    }

    return getRequiredManager().invokeMethodAsync('SetRootComponentParameters', this._componentId, parameterCount, parameters);
  }

  async dispose() {
    if (this._componentId !== null) {
      await getRequiredManager().invokeMethodAsync('RemoveRootComponent', this._componentId);
      this._componentId = null; // Ensure it can't be used again

      for (const jsEventCallbackWrapper of this._jsEventCallbackWrappers.values()) {
        jsEventCallbackWrapper?.dispose();
      }
    }
  }
}

// Called by the framework
export function enableJSRootComponents(
  managerInstance: DotNet.DotNetObject,
  parameterInfo: JSComponentParametersByIdentifier,
  initializerInfo: JSComponentIdentifiersByInitializer
): void {
  if (manager) {
    // This will only happen in very nonstandard cases where someone has multiple hosts.
    // It's up to the developer to ensure that only one of them enables dynamic root components.
    throw new Error('Dynamic root components have already been enabled.');
  }

  manager = managerInstance;
  jsComponentParametersByIdentifier = parameterInfo;

  // Call the registered initializers. This is an arbitrary subset of the JS component types that are registered
  // on the .NET side - just those of them that require some JS-side initialization (e.g., to register them
  // as custom elements).
  for (const [initializerIdentifier, componentIdentifiers] of Object.entries(initializerInfo)) {
    const initializerFunc = DotNet.jsCallDispatcher.findJSFunction(initializerIdentifier, 0) as JSComponentInitializerCallback;
    for (const componentIdentifier of componentIdentifiers) {
      const parameters = parameterInfo[componentIdentifier];
      initializerFunc(componentIdentifier, parameters);
    }
  }
}

function getRequiredManager(): DotNet.DotNetObject {
  if (!manager) {
    throw new Error('Dynamic root components have not been enabled in this application.');
  }

  return manager;
}

// Keep in sync with equivalent in JSComponentConfigurationStore.cs
// These are an internal implementation detail not exposed in the registration APIs.
export type JSComponentParametersByIdentifier = { [identifier: string]: JSComponentParameter[] };
export type JSComponentIdentifiersByInitializer = { [initializer: string]: string[] };

// The following is public API
export interface JSComponentInitializerCallback {
  (identifier: string, parameters: JSComponentParameter[]): void;
}

export interface JSComponentParameter {
  name: string;
  type: JSComponentParameterType;
}

// JSON-primitive types, plus for those whose .NET equivalent isn't nullable, a '?' to indicate nullability
// This allows custom element authors to coerce attribute strings into the appropriate type
export type JSComponentParameterType = 'string' | 'boolean' | 'boolean?' | 'number' | 'number?' | 'object' | 'eventcallback';
