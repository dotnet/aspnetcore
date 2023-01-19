import { WebAssemblyBootResourceType } from './WebAssemblyStartOptions';
type LoadBootResourceCallback = (type: WebAssemblyBootResourceType, name: string, defaultUri: string, integrity: string) => string | Promise<Response> | null | undefined;
export declare class BootConfigResult {
    bootConfig: BootJsonData;
    applicationEnvironment: string;
    private constructor();
    static initAsync(loadBootResource?: LoadBootResourceCallback, environment?: string): Promise<BootConfigResult>;
}
export interface BootJsonData {
    readonly entryAssembly: string;
    readonly resources: ResourceGroups;
    /** Gets a value that determines if this boot config was produced from a non-published build (i.e. dotnet build or dotnet run) */
    readonly debugBuild: boolean;
    readonly linkerEnabled: boolean;
    readonly cacheBootResources: boolean;
    readonly config: string[];
    readonly icuDataMode: ICUDataMode;
    modifiableAssemblies: string | null;
    aspnetCoreBrowserTools: string | null;
}
export type BootJsonDataExtension = {
    [extensionName: string]: ResourceList;
};
export interface ResourceGroups {
    readonly assembly: ResourceList;
    readonly lazyAssembly: ResourceList;
    readonly pdb?: ResourceList;
    readonly runtime: ResourceList;
    readonly satelliteResources?: {
        [cultureName: string]: ResourceList;
    };
    readonly libraryInitializers?: ResourceList;
    readonly extensions?: BootJsonDataExtension;
}
export type ResourceList = {
    [name: string]: string;
};
export declare enum ICUDataMode {
    Sharded = 0,
    All = 1,
    Invariant = 2
}
export {};
