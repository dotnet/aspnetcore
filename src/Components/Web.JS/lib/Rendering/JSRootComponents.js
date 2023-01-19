// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { DotNet } from '@microsoft/dotnet-js-interop';
const pendingRootComponentContainerNamePrefix = '__bl-dynamic-root:';
const pendingRootComponentContainers = new Map();
let nextPendingDynamicRootComponentIdentifier = 0;
let manager;
let jsComponentParametersByIdentifier;
// These are the public APIs at Blazor.rootComponents.*
export const RootComponentsFunctions = {
    async add(toElement, componentIdentifier, initialParameters) {
        if (!initialParameters) {
            throw new Error('initialParameters must be an object, even if empty.');
        }
        // Track the container so we can use it when the component gets attached to the document via a selector
        const containerIdentifier = pendingRootComponentContainerNamePrefix + (++nextPendingDynamicRootComponentIdentifier).toString();
        pendingRootComponentContainers.set(containerIdentifier, toElement);
        // Instruct .NET to add and render the new root component
        const componentId = await getRequiredManager().invokeMethodAsync('AddRootComponent', componentIdentifier, containerIdentifier);
        const component = new DynamicRootComponent(componentId, jsComponentParametersByIdentifier[componentIdentifier]);
        await component.setParameters(initialParameters);
        return component;
    },
};
export function getAndRemovePendingRootComponentContainer(containerIdentifier) {
    const container = pendingRootComponentContainers.get(containerIdentifier);
    if (container) {
        pendingRootComponentContainers.delete(containerIdentifier);
        return container;
    }
}
class EventCallbackWrapper {
    invoke(arg) {
        return this._callback(arg);
    }
    setCallback(callback) {
        if (!this._selfJSObjectReference) {
            this._selfJSObjectReference = DotNet.createJSObjectReference(this);
        }
        this._callback = callback;
    }
    getJSObjectReference() {
        return this._selfJSObjectReference;
    }
    dispose() {
        if (this._selfJSObjectReference) {
            DotNet.disposeJSObjectReference(this._selfJSObjectReference);
        }
    }
}
class DynamicRootComponent {
    constructor(componentId, parameters) {
        this._jsEventCallbackWrappers = new Map();
        this._componentId = componentId;
        for (const parameter of parameters) {
            if (parameter.type === 'eventcallback') {
                this._jsEventCallbackWrappers.set(parameter.name.toLowerCase(), new EventCallbackWrapper());
            }
        }
    }
    setParameters(parameters) {
        const mappedParameters = {};
        const entries = Object.entries(parameters || {});
        const parameterCount = entries.length;
        for (const [key, value] of entries) {
            const callbackWrapper = this._jsEventCallbackWrappers.get(key.toLowerCase());
            if (!callbackWrapper || !value) {
                mappedParameters[key] = value;
                continue;
            }
            callbackWrapper.setCallback(value);
            mappedParameters[key] = callbackWrapper.getJSObjectReference();
        }
        return getRequiredManager().invokeMethodAsync('SetRootComponentParameters', this._componentId, parameterCount, mappedParameters);
    }
    async dispose() {
        if (this._componentId !== null) {
            await getRequiredManager().invokeMethodAsync('RemoveRootComponent', this._componentId);
            this._componentId = null; // Ensure it can't be used again
            for (const jsEventCallbackWrapper of this._jsEventCallbackWrappers.values()) {
                jsEventCallbackWrapper.dispose();
            }
        }
    }
}
// Called by the framework
export function enableJSRootComponents(managerInstance, jsComponentParameters, jsComponentInitializers) {
    if (manager) {
        // This will only happen in very nonstandard cases where someone has multiple hosts.
        // It's up to the developer to ensure that only one of them enables dynamic root components.
        throw new Error('Dynamic root components have already been enabled.');
    }
    manager = managerInstance;
    jsComponentParametersByIdentifier = jsComponentParameters;
    // Call the registered initializers. This is an arbitrary subset of the JS component types that are registered
    // on the .NET side - just those of them that require some JS-side initialization (e.g., to register them
    // as custom elements).
    for (const [initializerIdentifier, componentIdentifiers] of Object.entries(jsComponentInitializers)) {
        const initializerFunc = DotNet.jsCallDispatcher.findJSFunction(initializerIdentifier, 0);
        for (const componentIdentifier of componentIdentifiers) {
            const parameters = jsComponentParameters[componentIdentifier];
            initializerFunc(componentIdentifier, parameters);
        }
    }
}
function getRequiredManager() {
    if (!manager) {
        throw new Error('Dynamic root components have not been enabled in this application.');
    }
    return manager;
}
//# sourceMappingURL=JSRootComponents.js.map