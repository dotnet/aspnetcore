import { LogicalElement } from '../Rendering/LogicalElements';
import { WebAssemblyComponentDescriptor } from '../Services/ComponentDescriptorDiscovery';
export declare class WebAssemblyComponentAttacher {
    preregisteredComponents: WebAssemblyComponentDescriptor[];
    private componentsById;
    constructor(components: WebAssemblyComponentDescriptor[]);
    resolveRegisteredElement(id: string): LogicalElement | undefined;
    getParameterValues(id: number): string | undefined;
    getParameterDefinitions(id: number): string | undefined;
    getTypeName(id: number): string;
    getAssembly(id: number): string;
    getId(index: number): number;
    getCount(): number;
}
