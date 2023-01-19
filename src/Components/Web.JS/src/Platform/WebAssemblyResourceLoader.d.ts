import { BootJsonData, ResourceList } from './BootConfig';
import { WebAssemblyStartOptions, WebAssemblyBootResourceType } from './WebAssemblyStartOptions';
export declare class WebAssemblyResourceLoader {
    readonly bootConfig: BootJsonData;
    readonly cacheIfUsed: Cache | null;
    readonly startOptions: Partial<WebAssemblyStartOptions>;
    private usedCacheKeys;
    private networkLoads;
    private cacheLoads;
    static initAsync(bootConfig: BootJsonData, startOptions: Partial<WebAssemblyStartOptions>): Promise<WebAssemblyResourceLoader>;
    constructor(bootConfig: BootJsonData, cacheIfUsed: Cache | null, startOptions: Partial<WebAssemblyStartOptions>);
    loadResources(resources: ResourceList, url: (name: string) => string, resourceType: WebAssemblyBootResourceType): LoadingResource[];
    loadResource(name: string, url: string, contentHash: string, resourceType: WebAssemblyBootResourceType): LoadingResource;
    logToConsole(): void;
    purgeUnusedCacheEntriesAsync(): Promise<void>;
    private loadResourceWithCaching;
    private loadResourceWithoutCaching;
    private addToCacheAsync;
}
export interface LoadingResource {
    name: string;
    url: string;
    response: Promise<Response>;
}
