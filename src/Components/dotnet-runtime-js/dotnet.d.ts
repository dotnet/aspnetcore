//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//!
//! This is generated file, see src/mono/wasm/runtime/rollup.config.js

//! This is not considered public API with backward compatibility guarantees. 

declare interface NativePointer {
    __brandNativePointer: "NativePointer";
}
declare interface VoidPtr extends NativePointer {
    __brand: "VoidPtr";
}
declare interface CharPtr extends NativePointer {
    __brand: "CharPtr";
}
declare interface Int32Ptr extends NativePointer {
    __brand: "Int32Ptr";
}
declare interface EmscriptenModule {
    /** @deprecated Please use growableHeapI8() instead.*/
    HEAP8: Int8Array;
    /** @deprecated Please use growableHeapI16() instead.*/
    HEAP16: Int16Array;
    /** @deprecated Please use growableHeapI32() instead. */
    HEAP32: Int32Array;
    /** @deprecated Please use growableHeapI64() instead. */
    HEAP64: BigInt64Array;
    /** @deprecated Please use growableHeapU8() instead. */
    HEAPU8: Uint8Array;
    /** @deprecated Please use growableHeapU16() instead. */
    HEAPU16: Uint16Array;
    /** @deprecated Please use growableHeapU32() instead */
    HEAPU32: Uint32Array;
    /** @deprecated Please use growableHeapF32() instead */
    HEAPF32: Float32Array;
    /** @deprecated Please use growableHeapF64() instead. */
    HEAPF64: Float64Array;
    _malloc(size: number): VoidPtr;
    _free(ptr: VoidPtr): void;
    out(message: string): void;
    err(message: string): void;
    ccall<T>(ident: string, returnType?: string | null, argTypes?: string[], args?: any[], opts?: any): T;
    cwrap<T extends Function>(ident: string, returnType: string, argTypes?: string[], opts?: any): T;
    cwrap<T extends Function>(ident: string, ...args: any[]): T;
    setValue(ptr: VoidPtr, value: number, type: string, noSafe?: number | boolean): void;
    setValue(ptr: Int32Ptr, value: number, type: string, noSafe?: number | boolean): void;
    getValue(ptr: number, type: string, noSafe?: number | boolean): number;
    UTF8ToString(ptr: CharPtr, maxBytesToRead?: number): string;
    UTF8ArrayToString(u8Array: Uint8Array, idx?: number, maxBytesToRead?: number): string;
    stringToUTF8Array(str: string, heap: Uint8Array, outIdx: number, maxBytesToWrite: number): void;
    FS_createPath(parent: string, path: string, canRead?: boolean, canWrite?: boolean): string;
    FS_createDataFile(parent: string, name: string, data: TypedArray, canRead: boolean, canWrite: boolean, canOwn?: boolean): string;
    addFunction(fn: Function, signature: string): number;
    stackSave(): VoidPtr;
    stackRestore(stack: VoidPtr): void;
    stackAlloc(size: number): VoidPtr;
    instantiateWasm?: InstantiateWasmCallBack;
    preInit?: (() => any)[] | (() => any);
    preRun?: (() => any)[] | (() => any);
    onRuntimeInitialized?: () => any;
    postRun?: (() => any)[] | (() => any);
    onAbort?: {
        (error: any): void;
    };
    onExit?: {
        (code: number): void;
    };
}
type InstantiateWasmSuccessCallback = (instance: WebAssembly.Instance, module: WebAssembly.Module | undefined) => void;
type InstantiateWasmCallBack = (imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback) => any;
declare type TypedArray = Int8Array | Uint8Array | Uint8ClampedArray | Int16Array | Uint16Array | Int32Array | Uint32Array | Float32Array | Float64Array;

interface DotnetHostBuilder {
    withConfig(config: MonoConfig): DotnetHostBuilder;
    withConfigSrc(configSrc: string): DotnetHostBuilder;
    withApplicationArguments(...args: string[]): DotnetHostBuilder;
    withEnvironmentVariable(name: string, value: string): DotnetHostBuilder;
    withEnvironmentVariables(variables: {
        [i: string]: string;
    }): DotnetHostBuilder;
    withVirtualWorkingDirectory(vfsPath: string): DotnetHostBuilder;
    withDiagnosticTracing(enabled: boolean): DotnetHostBuilder;
    withDebugging(level: number): DotnetHostBuilder;
    withMainAssembly(mainAssemblyName: string): DotnetHostBuilder;
    withApplicationArgumentsFromQuery(): DotnetHostBuilder;
    withApplicationEnvironment(applicationEnvironment?: string): DotnetHostBuilder;
    withApplicationCulture(applicationCulture?: string): DotnetHostBuilder;
    /**
     * Overrides the built-in boot resource loading mechanism so that boot resources can be fetched
     * from a custom source, such as an external CDN.
     */
    withResourceLoader(loadBootResource?: LoadBootResourceCallback): DotnetHostBuilder;
    create(): Promise<RuntimeAPI>;
    run(): Promise<number>;
}
type MonoConfig = {
    /**
     * Additional search locations for assets.
     */
    remoteSources?: string[];
    /**
     * It will not fail the startup is .pdb files can't be downloaded
     */
    ignorePdbLoadErrors?: boolean;
    /**
     * We are throttling parallel downloads in order to avoid net::ERR_INSUFFICIENT_RESOURCES on chrome. The default value is 16.
     */
    maxParallelDownloads?: number;
    /**
     * We are making up to 2 more delayed attempts to download same asset. Default true.
     */
    enableDownloadRetry?: boolean;
    /**
     * Name of the assembly with main entrypoint
     */
    mainAssemblyName?: string;
    /**
     * Configures the runtime's globalization mode
     */
    globalizationMode?: GlobalizationMode;
    /**
     * debugLevel > 0 enables debugging and sets the debug log level to debugLevel
     * debugLevel == 0 disables debugging and enables interpreter optimizations
     * debugLevel < 0 enables debugging and disables debug logging.
     */
    debugLevel?: number;
    /**
     * Gets a value that determines whether to enable caching of the 'resources' inside a CacheStorage instance within the browser.
     */
    cacheBootResources?: boolean;
    /**
    * Enables diagnostic log messages during startup
    */
    diagnosticTracing?: boolean;
    /**
     * Dictionary-style Object containing environment variables
     */
    environmentVariables?: {
        [i: string]: string;
    };
    /**
     * initial number of workers to add to the emscripten pthread pool
     */
    pthreadPoolSize?: number;
    /**
     * If true, the snapshot of runtime's memory will be stored in the browser and used for faster startup next time. Default is false.
     */
    startupMemoryCache?: boolean;
    /**
     * application environment
     */
    applicationEnvironment?: string;
    /**
     * Gets the application culture. This is a name specified in the BCP 47 format. See https://tools.ietf.org/html/bcp47
     */
    applicationCulture?: string;
    /**
     * definition of assets to load along with the runtime.
     */
    resources?: ResourceGroups;
    /**
     * appsettings files to load to VFS
     */
    appsettings?: string[];
    /**
     * config extensions declared in MSBuild items @(WasmBootConfigExtension)
     */
    extensions?: {
        [name: string]: any;
    };
};
type ResourceExtensions = {
    [extensionName: string]: ResourceList;
};
interface ResourceGroups {
    hash?: string;
    assembly?: ResourceList;
    lazyAssembly?: ResourceList;
    pdb?: ResourceList;
    jsModuleWorker?: ResourceList;
    jsModuleGlobalization?: ResourceList;
    jsModuleNative: ResourceList;
    jsModuleRuntime: ResourceList;
    wasmSymbols?: ResourceList;
    wasmNative: ResourceList;
    icu?: ResourceList;
    satelliteResources?: {
        [cultureName: string]: ResourceList;
    };
    modulesAfterConfigLoaded?: ResourceList;
    modulesAfterRuntimeReady?: ResourceList;
    extensions?: ResourceExtensions;
    vfs?: {
        [virtualPath: string]: ResourceList;
    };
}
/**
 * A "key" is name of the file, a "value" is optional hash for integrity check.
 */
type ResourceList = {
    [name: string]: string | null | "";
};
/**
 * Overrides the built-in boot resource loading mechanism so that boot resources can be fetched
 * from a custom source, such as an external CDN.
 * @param type The type of the resource to be loaded.
 * @param name The name of the resource to be loaded.
 * @param defaultUri The URI from which the framework would fetch the resource by default. The URI may be relative or absolute.
 * @param integrity The integrity string representing the expected content in the response.
 * @returns A URI string or a Response promise to override the loading process, or null/undefined to allow the default loading behavior.
 * When returned string is not qualified with `./` or absolute URL, it will be resolved against the application base URI.
 */
type LoadBootResourceCallback = (type: WebAssemblyBootResourceType, name: string, defaultUri: string, integrity: string, behavior: AssetBehaviors) => string | Promise<Response> | null | undefined;
interface ResourceRequest {
    name: string;
    behavior: AssetBehaviors;
    resolvedUrl?: string;
    hash?: string | null | "";
}
interface LoadingResource {
    name: string;
    url: string;
    response: Promise<Response>;
}
interface AssetEntry extends ResourceRequest {
    /**
     * If specified, overrides the path of the asset in the virtual filesystem and similar data structures once downloaded.
     */
    virtualPath?: string;
    /**
     * Culture code
     */
    culture?: string;
    /**
     * If true, an attempt will be made to load the asset from each location in MonoConfig.remoteSources.
     */
    loadRemote?: boolean;
    /**
     * If true, the runtime startup would not fail if the asset download was not successful.
     */
    isOptional?: boolean;
    /**
     * If provided, runtime doesn't have to fetch the data.
     * Runtime would set the buffer to null after instantiation to free the memory.
     */
    buffer?: ArrayBuffer;
    /**
     * It's metadata + fetch-like Promise<Response>
     * If provided, the runtime doesn't have to initiate the download. It would just await the response.
     */
    pendingDownload?: LoadingResource;
}
type SingleAssetBehaviors = 
/**
 * The binary of the dotnet runtime.
 */
"dotnetwasm"
/**
 * The javascript module for loader.
 */
 | "js-module-dotnet"
/**
 * The javascript module for threads.
 */
 | "js-module-threads"
/**
 * The javascript module for runtime.
 */
 | "js-module-runtime"
/**
 * The javascript module for emscripten.
 */
 | "js-module-native"
/**
 * The javascript module for hybrid globalization.
 */
 | "js-module-globalization"
/**
 * Typically blazor.boot.json
 */
 | "manifest";
type AssetBehaviors = SingleAssetBehaviors | 
/**
 * Load asset as a managed resource assembly.
 */
"resource"
/**
 * Load asset as a managed assembly.
 */
 | "assembly"
/**
 * Load asset as a managed debugging information.
 */
 | "pdb"
/**
 * Store asset into the native heap.
 */
 | "heap"
/**
 * Load asset as an ICU data archive.
 */
 | "icu"
/**
 * Load asset into the virtual filesystem (for fopen, File.Open, etc).
 */
 | "vfs"
/**
 * The javascript module that came from nuget package .
 */
 | "js-module-library-initializer"
/**
 * The javascript module for threads.
 */
 | "symbols";
declare const enum GlobalizationMode {
    /**
     * Load sharded ICU data.
     */
    Sharded = "sharded",
    /**
     * Load all ICU data.
     */
    All = "all",
    /**
     * Operate in invariant globalization mode.
     */
    Invariant = "invariant",
    /**
     * Use user defined icu file.
     */
    Custom = "custom",
    /**
     * Operate in hybrid globalization mode with small ICU files, using native platform functions.
     */
    Hybrid = "hybrid"
}
type DotnetModuleConfig = {
    disableDotnet6Compatibility?: boolean;
    config?: MonoConfig;
    configSrc?: string;
    onConfigLoaded?: (config: MonoConfig) => void | Promise<void>;
    onDotnetReady?: () => void | Promise<void>;
    onDownloadResourceProgress?: (resourcesLoaded: number, totalResources: number) => void;
    imports?: any;
    exports?: string[];
} & Partial<EmscriptenModule>;
type APIType = {
    runMain: (mainAssemblyName: string, args: string[]) => Promise<number>;
    runMainAndExit: (mainAssemblyName: string, args: string[]) => Promise<number>;
    setEnvironmentVariable: (name: string, value: string) => void;
    getAssemblyExports(assemblyName: string): Promise<any>;
    setModuleImports(moduleName: string, moduleImports: any): void;
    getConfig: () => MonoConfig;
    invokeLibraryInitializers: (functionName: string, args: any[]) => Promise<void>;
    setHeapB32: (offset: NativePointer, value: number | boolean) => void;
    setHeapU8: (offset: NativePointer, value: number) => void;
    setHeapU16: (offset: NativePointer, value: number) => void;
    setHeapU32: (offset: NativePointer, value: NativePointer | number) => void;
    setHeapI8: (offset: NativePointer, value: number) => void;
    setHeapI16: (offset: NativePointer, value: number) => void;
    setHeapI32: (offset: NativePointer, value: number) => void;
    setHeapI52: (offset: NativePointer, value: number) => void;
    setHeapU52: (offset: NativePointer, value: number) => void;
    setHeapI64Big: (offset: NativePointer, value: bigint) => void;
    setHeapF32: (offset: NativePointer, value: number) => void;
    setHeapF64: (offset: NativePointer, value: number) => void;
    getHeapB32: (offset: NativePointer) => boolean;
    getHeapU8: (offset: NativePointer) => number;
    getHeapU16: (offset: NativePointer) => number;
    getHeapU32: (offset: NativePointer) => number;
    getHeapI8: (offset: NativePointer) => number;
    getHeapI16: (offset: NativePointer) => number;
    getHeapI32: (offset: NativePointer) => number;
    getHeapI52: (offset: NativePointer) => number;
    getHeapU52: (offset: NativePointer) => number;
    getHeapI64Big: (offset: NativePointer) => bigint;
    getHeapF32: (offset: NativePointer) => number;
    getHeapF64: (offset: NativePointer) => number;
    localHeapViewI8: () => Int8Array;
    localHeapViewI16: () => Int16Array;
    localHeapViewI32: () => Int32Array;
    localHeapViewI64Big: () => BigInt64Array;
    localHeapViewU8: () => Uint8Array;
    localHeapViewU16: () => Uint16Array;
    localHeapViewU32: () => Uint32Array;
    localHeapViewF32: () => Float32Array;
    localHeapViewF64: () => Float64Array;
};
type RuntimeAPI = {
    /**
     * @deprecated Please use API object instead. See also MONOType in dotnet-legacy.d.ts
     */
    MONO: any;
    /**
     * @deprecated Please use API object instead. See also BINDINGType in dotnet-legacy.d.ts
     */
    BINDING: any;
    INTERNAL: any;
    Module: EmscriptenModule;
    runtimeId: number;
    runtimeBuildInfo: {
        productVersion: string;
        gitHash: string;
        buildConfiguration: string;
    };
} & APIType;
type ModuleAPI = {
    dotnet: DotnetHostBuilder;
    exit: (code: number, reason?: any) => void;
};
type CreateDotnetRuntimeType = (moduleFactory: DotnetModuleConfig | ((api: RuntimeAPI) => DotnetModuleConfig)) => Promise<RuntimeAPI>;
type WebAssemblyBootResourceType = "assembly" | "pdb" | "dotnetjs" | "dotnetwasm" | "globalization" | "manifest" | "configuration";

interface IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
}
interface IMemoryView extends IDisposable {
    /**
     * copies elements from provided source to the wasm memory.
     * target has to have the elements of the same type as the underlying C# array.
     * same as TypedArray.set()
     */
    set(source: TypedArray, targetOffset?: number): void;
    /**
     * copies elements from wasm memory to provided target.
     * target has to have the elements of the same type as the underlying C# array.
     */
    copyTo(target: TypedArray, sourceOffset?: number): void;
    /**
     * same as TypedArray.slice()
     */
    slice(start?: number, end?: number): TypedArray;
    get length(): number;
    get byteLength(): number;
}

declare function mono_exit(exit_code: number, reason?: any): void;

declare const dotnet: DotnetHostBuilder;
declare const exit: typeof mono_exit;

declare global {
    function getDotnetRuntime(runtimeId: number): RuntimeAPI | undefined;
}
declare const createDotnetRuntime: CreateDotnetRuntimeType;

export { AssetBehaviors, AssetEntry, CreateDotnetRuntimeType, DotnetHostBuilder, DotnetModuleConfig, EmscriptenModule, GlobalizationMode, IMemoryView, ModuleAPI, MonoConfig, ResourceRequest, RuntimeAPI, createDotnetRuntime as default, dotnet, exit };
