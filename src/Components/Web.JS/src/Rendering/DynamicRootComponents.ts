import { DotNet } from '@microsoft/dotnet-js-interop';

const blazorDynamicRootComponentAttributeName = 'bl-dynamic-root';
const textEncoder = new TextEncoder();

let manager: DotNet.DotNetObject | undefined;
let nextDynamicRootComponentSelector = 0;

// These are the public APIs at Blazor.rootComponents.*
export const RootComponentsFunctions = {
    async add(toElement: Element, componentIdentifier: FunctionStringCallback): Promise<DynamicRootComponent> {
        // Attaching a selector like below assumes the element is within the document. If we need to support
        // rendering into nonattached elements, we can add that, but it's possible that other aspects of the
        // JS-side code will make assumptions about rendering only happening into document-attached nodes.
        // For now, limiting it to elements within the document.
        if (!toElement.isConnected) {
            throw new Error('The element is not connected to the DOM.');
        }

        const selectorValue = (++nextDynamicRootComponentSelector).toString();
        toElement.setAttribute(blazorDynamicRootComponentAttributeName, selectorValue);
        const selector = `[${blazorDynamicRootComponentAttributeName}='${selectorValue}']`;
        const componentId = await getRequiredManager().invokeMethodAsync<number>(
            'AddRootComponent', componentIdentifier, selector);
        return new DynamicRootComponent(componentId);
    }
};

class DynamicRootComponent {
    private _componentId: number | null;

    constructor(componentId: number) {
        this._componentId = componentId;
    }

    setParameters(parameters: object | null | undefined) {
        parameters = parameters || {};
        const parameterCount = Object.keys(parameters).length;

        // TODO: Need to use the JSInterop serializer here so that special objects like JSObjectReference
        // get serialized properly.
        const parametersJson = JSON.stringify(parameters);
        const parametersUtf8 = textEncoder.encode(parametersJson);

        return getRequiredManager().invokeMethodAsync('RenderRootComponentAsync', this._componentId, parameterCount, parametersUtf8);
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
