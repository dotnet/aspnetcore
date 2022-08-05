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
declare type InstantiateWasmSuccessCallback = (instance: WebAssembly.Instance, module: WebAssembly.Module) => void;
declare type InstantiateWasmCallBack = (imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback) => any;
declare type TypedArray = Int8Array | Uint8Array | Uint8ClampedArray | Int16Array | Uint16Array | Int32Array | Uint32Array | Float32Array | Float64Array;

declare type MonoConfig = {
    assemblyRootFolder?: string;
    assets?: AssetEntry[];
    /**
     * debugLevel > 0 enables debugging and sets the debug log level to debugLevel
     * debugLevel == 0 disables debugging and enables interpreter optimizations
     * debugLevel < 0 enabled debugging and disables debug logging.
     */
    debugLevel?: number;
    maxParallelDownloads?: number;
    globalizationMode?: GlobalizationMode;
    diagnosticTracing?: boolean;
    remoteSources?: string[];
    environmentVariables?: {
        [i: string]: string;
    };
    runtimeOptions?: string[];
    aotProfilerOptions?: AOTProfilerOptions;
    coverageProfilerOptions?: CoverageProfilerOptions;
    ignorePdbLoadErrors?: boolean;
    waitForDebugger?: number;
};
interface ResourceRequest {
    name: string;
    behavior: AssetBehaviours;
    resolvedUrl?: string;
    hash?: string;
}
interface AssetEntry extends ResourceRequest {
    virtualPath?: string;
    culture?: string;
    loadRemote?: boolean;
    isOptional?: boolean;
    buffer?: ArrayBuffer;
    pending?: LoadingResource;
}
declare type AssetBehaviours = "resource" | "assembly" | "pdb" | "heap" | "icu" | "vfs" | "dotnetwasm" | "js-module-crypto" | "js-module-threads";
declare type GlobalizationMode = "icu" | // load ICU globalization data from any runtime assets with behavior "icu".
"invariant" | //  operate in invariant globalization mode.
"auto";
declare type AOTProfilerOptions = {
    writeAt?: string;
    sendTo?: string;
};
declare type CoverageProfilerOptions = {
    writeAt?: string;
    sendTo?: string;
};
declare type DotnetModuleConfig = {
    disableDotnet6Compatibility?: boolean;
    config?: MonoConfig;
    configSrc?: string;
    onConfigLoaded?: (config: MonoConfig) => void | Promise<void>;
    onDotnetReady?: () => void | Promise<void>;
    imports?: any;
    exports?: string[];
    downloadResource?: (request: ResourceRequest) => LoadingResource | undefined;
} & Partial<EmscriptenModule>;
interface LoadingResource {
    name: string;
    url: string;
    response: Promise<Response>;
}

declare type APIType = {
    runMain: (mainAssemblyName: string, args: string[]) => Promise<number>;
    runMainAndExit: (mainAssemblyName: string, args: string[]) => Promise<void>;
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
declare type DotnetPublicAPI = {
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
        buildConfiguration: string;
    };
} & APIType;

interface IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
}
declare const enum MemoryViewType {
    Byte = 0,
    Int32 = 1,
    Double = 2
}
interface IMemoryView {
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

declare function createDotnetRuntime(moduleFactory: DotnetModuleConfig | ((api: DotnetPublicAPI) => DotnetModuleConfig)): Promise<DotnetPublicAPI>;
declare type CreateDotnetRuntimeType = typeof createDotnetRuntime;
declare global {
    function getDotnetRuntime(runtimeId: number): DotnetPublicAPI | undefined;
}

/**
 * Span class is JS wrapper for System.Span<T>. This view doesn't own the memory, nor pin the underlying array.
 * It's ideal to be used on call from C# with the buffer pinned there or with unmanaged memory.
 * It is disposed at the end of the call to JS.
 */
declare class Span implements IMemoryView, IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
    set(source: TypedArray, targetOffset?: number | undefined): void;
    copyTo(target: TypedArray, sourceOffset?: number | undefined): void;
    slice(start?: number | undefined, end?: number | undefined): TypedArray;
    get length(): number;
    get byteLength(): number;
}
/**
 * ArraySegment class is JS wrapper for System.ArraySegment<T>.
 * This wrapper would also pin the underlying array and hold GCHandleType.Pinned until this JS instance is collected.
 * User could dispose it manually.
 */
declare class ArraySegment implements IMemoryView, IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
    set(source: TypedArray, targetOffset?: number | undefined): void;
    copyTo(target: TypedArray, sourceOffset?: number | undefined): void;
    slice(start?: number | undefined, end?: number | undefined): TypedArray;
    get length(): number;
    get byteLength(): number;
}
/**
 * Represents proxy to the System.Exception
 */
declare class ManagedError extends Error implements IDisposable {
    get stack(): string | undefined;
    dispose(): void;
    get isDisposed(): boolean;
    toString(): string;
}
/**
 * Represents proxy to the System.Object
 */
declare class ManagedObject implements IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
    toString(): string;
}

export { APIType, ArraySegment, AssetBehaviours, AssetEntry, CreateDotnetRuntimeType, DotnetModuleConfig, DotnetPublicAPI, EmscriptenModule, IMemoryView, LoadingResource, ManagedError, ManagedObject, MemoryViewType, MonoConfig, NativePointer, ResourceRequest, Span, createDotnetRuntime as default };
