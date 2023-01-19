export declare function discoverComponents(document: Document, type: 'webassembly' | 'server'): ServerComponentDescriptor[] | WebAssemblyComponentDescriptor[];
export declare function discoverPersistedState(node: Node): string | null | undefined;
interface ServerComponentMarker {
    type: string;
    sequence: number;
    descriptor: string;
}
export declare class ServerComponentDescriptor {
    type: string;
    start: Node;
    end?: Node;
    sequence: number;
    descriptor: string;
    constructor(type: string, start: Node, end: Node | undefined, sequence: number, descriptor: string);
    toRecord(): ServerComponentMarker;
}
export declare class WebAssemblyComponentDescriptor {
    private static globalId;
    type: 'webassembly';
    typeName: string;
    assembly: string;
    parameterDefinitions?: string;
    parameterValues?: string;
    id: number;
    start: Node;
    end?: Node;
    constructor(type: 'webassembly', start: Node, end: Node | undefined, assembly: string, typeName: string, parameterDefinitions?: string, parameterValues?: string);
}
export {};
