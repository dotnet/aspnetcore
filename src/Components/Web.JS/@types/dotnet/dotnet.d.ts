//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//!
//! This is generated file, see src/mono/wasm/runtime/rollup.config.js

//! This is not considered public API with backward compatibility guarantees.

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
    create(): Promise<RuntimeAPI>;
    run(): Promise<number>;
}

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
    HEAP8: Int8Array;
    HEAP16: Int16Array;
    HEAP32: Int32Array;
    HEAPU8: Uint8Array;
    HEAPU16: Uint16Array;
    HEAPU32: Uint32Array;
    HEAPF32: Float32Array;
    HEAPF64: Float64Array;
    _malloc(size: number): VoidPtr;
    _free(ptr: VoidPtr): void;
    print(message: string): void;
    printErr(message: string): void;
    ccall<T>(ident: string, returnType?: string | null, argTypes?: string[], args?: any[], opts?: any): T;
    cwrap<T extends Function>(ident: string, returnType: string, argTypes?: string[], opts?: any): T;
    cwrap<T extends Function>(ident: string, ...args: any[]): T;
    setValue(ptr: VoidPtr, value: number, type: string, noSafe?: number | boolean): void;
    setValue(ptr: Int32Ptr, value: number, type: string, noSafe?: number | boolean): void;
    getValue(ptr: number, type: string, noSafe?: number | boolean): number;
    UTF8ToString(ptr: CharPtr, maxBytesToRead?: number): string;
    UTF8ArrayToString(u8Array: Uint8Array, idx?: number, maxBytesToRead?: number): string;
    FS_createPath(parent: string, path: string, canRead?: boolean, canWrite?: boolean): string;
    FS_createDataFile(parent: string, name: string, data: TypedArray, canRead: boolean, canWrite: boolean, canOwn?: boolean): string;
    FS_readFile(filename: string, opts: any): any;
    removeRunDependency(id: string): void;
    addRunDependency(id: string): void;
    addFunction(fn: Function, signature: string): number;
    getWasmTableEntry(index: number): any;
    stackSave(): VoidPtr;
    stackRestore(stack: VoidPtr): void;
    stackAlloc(size: number): VoidPtr;
    ready: Promise<unknown>;
    instantiateWasm?: InstantiateWasmCallBack;
    preInit?: (() => any)[] | (() => any);
    preRun?: (() => any)[] | (() => any);
    onRuntimeInitialized?: () => any;
    postRun?: (() => any)[] | (() => any);
    onAbort?: {
        (error: any): void;
    };
}
type InstantiateWasmSuccessCallback = (instance: WebAssembly.Instance, module: WebAssembly.Module) => void;
type InstantiateWasmCallBack = (imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback) => any;
declare type TypedArray = Int8Array | Uint8Array | Uint8ClampedArray | Int16Array | Uint16Array | Int32Array | Uint32Array | Float32Array | Float64Array;

type MonoConfig = {
    /**
     * The subfolder containing managed assemblies and pdbs. This is relative to dotnet.js script.
     */
    assemblyRootFolder?: string;
    /**
     * A list of assets to load along with the runtime.
     */
    assets?: AssetEntry[];
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
     * debugLevel < 0 enabled debugging and disables debug logging.
     */
    debugLevel?: number;
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
};
interface ResourceRequest {
    name: string;
    behavior: AssetBehaviours;
    resolvedUrl?: string;
    hash?: string;
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
type AssetBehaviours = "resource" | "assembly" | "pdb" | "heap" | "icu" | "vfs" | "dotnetwasm" | "js-module-threads";
type GlobalizationMode = "icu" | // load ICU globalization data from any runtime assets with behavior "icu".
    "invariant" | //  operate in invariant globalization mode.
    "auto";
type DotnetModuleConfig = {
    disableDotnet6Compatibility?: boolean;
    config?: MonoConfig;
    configSrc?: string;
    onConfigLoaded?: (config: MonoConfig) => void | Promise<void>;
    onDotnetReady?: () => void | Promise<void>;
    imports?: any;
    exports?: string[];
    downloadResource?: (request: ResourceRequest) => LoadingResource | undefined;
} & Partial<EmscriptenModule>;
type APIType = {
    runMain: (mainAssemblyName: string, args: string[]) => Promise<number>;
    runMainAndExit: (mainAssemblyName: string, args: string[]) => Promise<number>;
    setEnvironmentVariable: (name: string, value: string) => void;
    getAssemblyExports(assemblyName: string): Promise<any>;
    setModuleImports(moduleName: string, moduleImports: any): void;
    getConfig: () => MonoConfig;
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
declare function createDotnetRuntime(moduleFactory: DotnetModuleConfig | ((api: RuntimeAPI) => DotnetModuleConfig)): Promise<RuntimeAPI>;
type CreateDotnetRuntimeType = typeof createDotnetRuntime;

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

declare global {
    function getDotnetRuntime(runtimeId: number): RuntimeAPI | undefined;
}

declare const dotnet: ModuleAPI["dotnet"];
declare const exit: ModuleAPI["exit"];

export { CreateDotnetRuntimeType, DotnetModuleConfig, EmscriptenModule, IMemoryView, ModuleAPI, MonoConfig, RuntimeAPI, AssetEntry, ResourceRequest, createDotnetRuntime as default, dotnet, exit };
