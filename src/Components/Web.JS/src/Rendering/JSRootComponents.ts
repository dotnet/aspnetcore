import { DotNet } from '@microsoft/dotnet-js-interop';

const pendingRootComponentContainerNamePrefix = '__bl-dynamic-root:';
const pendingRootComponentContainers = new Map<string, Element>();
let nextPendingDynamicRootComponentIdentifier = 0;

type ComponentParameters = object | null | undefined;
const textEncoder = new TextEncoder();

let manager: DotNet.DotNetObject | undefined;

// These are the public APIs at Blazor.rootComponents.*
export const RootComponentsFunctions = {
    async add(toElement: Element, componentIdentifier: FunctionStringCallback, initialParameters: ComponentParameters): Promise<DynamicRootComponent> {
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
    }
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

    constructor(componentId: number) {
        this._componentId = componentId;
    }

    setParameters(parameters: ComponentParameters) {
        parameters = parameters || {};
        const parameterCount = Object.keys(parameters).length;

        // TODO: Need to use the JSInterop serializer here so that special objects like JSObjectReference
        // get serialized properly.
        const parametersJson = JSON.stringify(parameters);
        const parametersUtf8 = textEncoder.encode(parametersJson);

        return getRequiredManager().invokeMethodAsync('SetRootComponentParameters', this._componentId, parameterCount, parametersUtf8);
    }

    async dispose() {
        if (this._componentId !== null) {
            await getRequiredManager().invokeMethodAsync('RemoveRootComponent', this._componentId);
            this._componentId = null; // Ensure it can't be used again
        }
    }
}

// Called by the framework
export function setDynamicRootComponentManager(instance: DotNet.DotNetObject) {
    if (manager) {
        // This will only happen in very nonstandard cases where someone has multiple hosts.
        // It's up to the developer to ensure that only one of them enables dynamic root components.
        throw new Error('Dynamic root components have already been enabled.');
    }

    manager = instance;
}

function getRequiredManager(): DotNet.DotNetObject {
    if (!manager) {
        throw new Error('Dynamic root components have not been enabled in this application.');
    }

    return manager;
}
