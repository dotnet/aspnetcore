import { DotNet } from '@microsoft/dotnet-js-interop';

const pendingRootComponentContainerNamePrefix = '__bl-dynamic-root:';
const pendingRootComponentContainers = new Map<string, Element>();
const jsFunctionPropertyName = 'func';
let nextPendingDynamicRootComponentIdentifier = 0;

type ComponentParameters = object | null | undefined;

let manager: DotNet.DotNetObject | undefined;

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
    const component = new DynamicRootComponent(componentId);
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

class DynamicRootComponent {
  private _componentId: number | null;

  private _jsFunctionObjectReferences = new Map<any, any>();

  constructor(componentId: number) {
    this._componentId = componentId;
  }

  setParameters(parameters: ComponentParameters) {
    parameters = parameters || {};

    const keys = Object.keys(parameters);
    const parameterCount = keys.length;

    // Wrap parameters of function type in JSObjectReference instances so they can be invoked from .NET
    for (const key of keys) {
      const value = parameters[key];

      if (typeof value !== 'function') {
        continue;
      }

      let existingJsObjectReference = this._jsFunctionObjectReferences.get(value);

      if (!existingJsObjectReference) {
        existingJsObjectReference = DotNet.createJSObjectReference({ [jsFunctionPropertyName]: value });
        this._jsFunctionObjectReferences.set(value, existingJsObjectReference);
      }

      parameters[key] = existingJsObjectReference;
    }

    return getRequiredManager().invokeMethodAsync('SetRootComponentParameters', this._componentId, parameterCount, parameters);
  }

  async dispose() {
    if (this._componentId !== null) {
      await getRequiredManager().invokeMethodAsync('RemoveRootComponent', this._componentId);
      this._componentId = null; // Ensure it can't be used again

      for (const jsObjectReference of this._jsFunctionObjectReferences.values()) {
        DotNet.disposeJSObjectReference(jsObjectReference);
      }
    }
  }
}

// Called by the framework
export function enableJSRootComponents(managerInstance: DotNet.DotNetObject, initializerInfo: JSComponentInfoByInitializer) {
  if (manager) {
    // This will only happen in very nonstandard cases where someone has multiple hosts.
    // It's up to the developer to ensure that only one of them enables dynamic root components.
    throw new Error('Dynamic root components have already been enabled.');
  }

  manager = managerInstance;

  // Call the registered initializers. This is an arbitrary subset of the JS component types that are registered
  // on the .NET side - just those of them that require some JS-side initialization (e.g., to register them
  // as custom elements).
  for (const [initializerIdentifier, jsComponentInfos] of Object.entries(initializerInfo)) {
    const initializerFunc = DotNet.jsCallDispatcher.findJSFunction(initializerIdentifier, 0) as JSComponentInitializerCallback;
    jsComponentInfos.forEach(jsComponentInfo => {
      initializerFunc(jsComponentInfo.identifier, jsComponentInfo.parameters);
    });
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
export type JSComponentInfoByInitializer = { [jsInitializer: string]: JSComponentInfo[] };
interface JSComponentInfo { identifier: string; parameters: JSComponentParameter[]; }

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
export type JSComponentParameterType = 'string' | 'boolean' | 'boolean?' | 'number' | 'number?' | 'object';
