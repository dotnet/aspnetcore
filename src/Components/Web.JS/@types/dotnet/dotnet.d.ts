//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//!
//! This is generated file, see src/mono/wasm/runtime/rollup.config.js

//! This is not considered public API with backward compatibility guarantees.

declare interface ManagedPointer {
    __brandManagedPointer: "ManagedPointer";
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
    stackSave(): VoidPtr;
    stackRestore(stack: VoidPtr): void;
    stackAlloc(size: number): VoidPtr;
    ready: Promise<unknown>;
    preInit?: (() => any)[];
    preRun?: (() => any)[];
    postRun?: (() => any)[];
    onAbort?: {
        (error: any): void;
    };
    onRuntimeInitialized?: () => any;
    instantiateWasm: (imports: any, successCallback: Function) => any;
}
declare type TypedArray = Int8Array | Uint8Array | Uint8ClampedArray | Int16Array | Uint16Array | Int32Array | Uint32Array | Float32Array | Float64Array;

/**
 * Allocates a block of memory that can safely contain pointers into the managed heap.
 * The result object has get(index) and set(index, value) methods that can be used to retrieve and store managed pointers.
 * Once you are done using the root buffer, you must call its release() method.
 * For small numbers of roots, it is preferable to use the mono_wasm_new_root and mono_wasm_new_roots APIs instead.
 */
declare function mono_wasm_new_root_buffer(capacity: number, name?: string): WasmRootBuffer;
/**
 * Allocates a WasmRoot pointing to a root provided and controlled by external code. Typicaly on managed stack.
 * Releasing this root will not de-allocate the root space. You still need to call .release().
 */
declare function mono_wasm_new_external_root<T extends MonoObject>(address: VoidPtr | MonoObjectRef): WasmRoot<T>;
/**
 * Allocates temporary storage for a pointer into the managed heap.
 * Pointers stored here will be visible to the GC, ensuring that the object they point to aren't moved or collected.
 * If you already have a managed pointer you can pass it as an argument to initialize the temporary storage.
 * The result object has get() and set(value) methods, along with a .value property.
 * When you are done using the root you must call its .release() method.
 */
declare function mono_wasm_new_root<T extends MonoObject>(value?: T | undefined): WasmRoot<T>;
/**
 * Releases 1 or more root or root buffer objects.
 * Multiple objects may be passed on the argument list.
 * 'undefined' may be passed as an argument so it is safe to call this method from finally blocks
 *  even if you are not sure all of your roots have been created yet.
 * @param {... WasmRoot} roots
 */
declare function mono_wasm_release_roots(...args: WasmRoot<any>[]): void;
declare class WasmRootBuffer {
    private __count;
    private length;
    private __offset;
    private __offset32;
    private __handle;
    private __ownsAllocation;
    constructor(offset: VoidPtr, capacity: number, ownsAllocation: boolean, name?: string);
    _throw_index_out_of_range(): void;
    _check_in_range(index: number): void;
    get_address(index: number): MonoObjectRef;
    get_address_32(index: number): number;
    get(index: number): ManagedPointer;
    set(index: number, value: ManagedPointer): ManagedPointer;
    copy_value_from_address(index: number, sourceAddress: MonoObjectRef): void;
    _unsafe_get(index: number): number;
    _unsafe_set(index: number, value: ManagedPointer | NativePointer): void;
    clear(): void;
    release(): void;
    toString(): string;
}
interface WasmRoot<T extends MonoObject> {
    get_address(): MonoObjectRef;
    get_address_32(): number;
    get address(): MonoObjectRef;
    get(): T;
    set(value: T): T;
    get value(): T;
    set value(value: T);
    copy_from_address(source: MonoObjectRef): void;
    copy_to_address(destination: MonoObjectRef): void;
    copy_from(source: WasmRoot<T>): void;
    copy_to(destination: WasmRoot<T>): void;
    valueOf(): T;
    clear(): void;
    release(): void;
    toString(): string;
}

interface MonoObject extends ManagedPointer {
    __brandMonoObject: "MonoObject";
}
interface MonoString extends MonoObject {
    __brand: "MonoString";
}
interface MonoArray extends MonoObject {
    __brand: "MonoArray";
}
interface MonoObjectRef extends ManagedPointer {
    __brandMonoObjectRef: "MonoObjectRef";
}
declare type MonoConfig = {
    isError: false;
    assembly_root: string;
    assets: AllAssetEntryTypes[];
    debug_level?: number;
    enable_debugging?: number;
    globalization_mode: GlobalizationMode;
    diagnostic_tracing?: boolean;
    remote_sources?: string[];
    environment_variables?: {
        [i: string]: string;
    };
    runtime_options?: string[];
    aot_profiler_options?: AOTProfilerOptions;
    coverage_profiler_options?: CoverageProfilerOptions;
    ignore_pdb_load_errors?: boolean;
    wait_for_debugger?: number;
};
declare type MonoConfigError = {
    isError: true;
    message: string;
    error: any;
};
declare type AllAssetEntryTypes = AssetEntry | AssemblyEntry | SatelliteAssemblyEntry | VfsEntry | IcuData;
declare type AssetEntry = {
    name: string;
    behavior: AssetBehaviours;
    virtual_path?: string;
    culture?: string;
    load_remote?: boolean;
    is_optional?: boolean;
    buffer?: ArrayBuffer;
};
interface AssemblyEntry extends AssetEntry {
    name: "assembly";
}
interface SatelliteAssemblyEntry extends AssetEntry {
    name: "resource";
    culture: string;
}
interface VfsEntry extends AssetEntry {
    name: "vfs";
    virtual_path: string;
}
interface IcuData extends AssetEntry {
    name: "icu";
    load_remote: boolean;
}
declare const enum AssetBehaviours {
    Resource = "resource",
    Assembly = "assembly",
    Heap = "heap",
    ICU = "icu",
    VFS = "vfs"
}
declare const enum GlobalizationMode {
    ICU = "icu",
    INVARIANT = "invariant",
    AUTO = "auto"
}
declare type AOTProfilerOptions = {
    write_at?: string;
    send_to?: string;
};
declare type CoverageProfilerOptions = {
    write_at?: string;
    send_to?: string;
};
interface EventPipeSessionOptions {
    collectRundownEvents?: boolean;
    providers: string;
}
declare type DotnetModuleConfig = {
    disableDotnet6Compatibility?: boolean;
    config?: MonoConfig | MonoConfigError;
    configSrc?: string;
    onConfigLoaded?: (config: MonoConfig) => Promise<void>;
    onDotnetReady?: () => void;
    imports?: DotnetModuleConfigImports;
    exports?: string[];
} & Partial<EmscriptenModule>;
declare type DotnetModuleConfigImports = {
    require?: (name: string) => any;
    fetch?: (url: string) => Promise<Response>;
    fs?: {
        promises?: {
            readFile?: (path: string) => Promise<string | Buffer>;
        };
        readFileSync?: (path: string, options: any | undefined) => string;
    };
    crypto?: {
        randomBytes?: (size: number) => Buffer;
    };
    ws?: WebSocket & {
        Server: any;
    };
    path?: {
        normalize?: (path: string) => string;
        dirname?: (path: string) => string;
    };
    url?: any;
};

declare type EventPipeSessionID = bigint;
interface EventPipeSession {
    get sessionID(): EventPipeSessionID;
    start(): void;
    stop(): void;
    getTraceBlob(): Blob;
}
declare const eventLevel: {
    readonly LogAlways: 0;
    readonly Critical: 1;
    readonly Error: 2;
    readonly Warning: 3;
    readonly Informational: 4;
    readonly Verbose: 5;
};
declare type EventLevel = typeof eventLevel;
declare type UnnamedProviderConfiguration = Partial<{
    keyword_mask: string | 0;
    level: number;
    args: string;
}>;
interface ProviderConfiguration extends UnnamedProviderConfiguration {
    name: string;
}
declare class SessionOptionsBuilder {
    private _rundown?;
    private _providers;
    constructor();
    static get Empty(): SessionOptionsBuilder;
    static get DefaultProviders(): SessionOptionsBuilder;
    setRundownEnabled(enabled: boolean): SessionOptionsBuilder;
    addProvider(provider: ProviderConfiguration): SessionOptionsBuilder;
    addRuntimeProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder;
    addRuntimePrivateProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder;
    addSampleProfilerProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder;
    build(): EventPipeSessionOptions;
}
interface Diagnostics {
    EventLevel: EventLevel;
    SessionOptionsBuilder: typeof SessionOptionsBuilder;
    createEventPipeSession(options?: EventPipeSessionOptions): EventPipeSession | null;
}

declare function mono_wasm_runtime_ready(): void;

declare function mono_wasm_setenv(name: string, value: string): void;
declare function mono_load_runtime_and_bcl_args(config: MonoConfig | MonoConfigError | undefined): Promise<void>;
declare function mono_wasm_load_data_archive(data: Uint8Array, prefix: string): boolean;
/**
 * Loads the mono config file (typically called mono-config.json) asynchroniously
 * Note: the run dependencies are so emsdk actually awaits it in order.
 *
 * @param {string} configFilePath - relative path to the config file
 * @throws Will throw an error if the config file loading fails
 */
declare function mono_wasm_load_config(configFilePath: string): Promise<void>;

declare function mono_wasm_load_icu_data(offset: VoidPtr): boolean;

/**
 * @deprecated Not GC or thread safe
 */
declare function conv_string(mono_obj: MonoString): string | null;
declare function conv_string_root(root: WasmRoot<MonoString>): string | null;
declare function js_string_to_mono_string_root(string: string, result: WasmRoot<MonoString>): void;
/**
 * @deprecated Not GC or thread safe
 */
declare function js_string_to_mono_string(string: string): MonoString;

/**
 * @deprecated Not GC or thread safe. For blazor use only
 */
declare function js_to_mono_obj(js_obj: any): MonoObject;
declare function js_to_mono_obj_root(js_obj: any, result: WasmRoot<MonoObject>, should_add_in_flight: boolean): void;
declare function js_typed_array_to_array_root(js_obj: any, result: WasmRoot<MonoArray>): void;
/**
 * @deprecated Not GC or thread safe
 */
declare function js_typed_array_to_array(js_obj: any): MonoArray;

declare function unbox_mono_obj(mono_obj: MonoObject): any;
declare function unbox_mono_obj_root(root: WasmRoot<any>): any;
declare function mono_array_to_js_array(mono_array: MonoArray): any[] | null;
declare function mono_array_root_to_js_array(arrayRoot: WasmRoot<MonoArray>): any[] | null;

declare function mono_bind_static_method(fqn: string, signature?: string): Function;
declare function mono_call_assembly_entry_point(assembly: string, args?: any[], signature?: string): number;

declare function mono_wasm_load_bytes_into_heap(bytes: Uint8Array): VoidPtr;

declare type _MemOffset = number | VoidPtr | NativePointer | ManagedPointer;
declare type _NumberOrPointer = number | VoidPtr | NativePointer | ManagedPointer;
declare function setB32(offset: _MemOffset, value: number | boolean): void;
declare function setU8(offset: _MemOffset, value: number): void;
declare function setU16(offset: _MemOffset, value: number): void;
declare function setU32(offset: _MemOffset, value: _NumberOrPointer): void;
declare function setI8(offset: _MemOffset, value: number): void;
declare function setI16(offset: _MemOffset, value: number): void;
declare function setI32(offset: _MemOffset, value: number): void;
/**
 * Throws for values which are not 52 bit integer. See Number.isSafeInteger()
 */
declare function setI52(offset: _MemOffset, value: number): void;
/**
 * Throws for values which are not 52 bit integer or are negative. See Number.isSafeInteger().
 */
declare function setU52(offset: _MemOffset, value: number): void;
declare function setI64Big(offset: _MemOffset, value: bigint): void;
declare function setF32(offset: _MemOffset, value: number): void;
declare function setF64(offset: _MemOffset, value: number): void;
declare function getB32(offset: _MemOffset): boolean;
declare function getU8(offset: _MemOffset): number;
declare function getU16(offset: _MemOffset): number;
declare function getU32(offset: _MemOffset): number;
declare function getI8(offset: _MemOffset): number;
declare function getI16(offset: _MemOffset): number;
declare function getI32(offset: _MemOffset): number;
/**
 * Throws for Number.MIN_SAFE_INTEGER > value > Number.MAX_SAFE_INTEGER
 */
declare function getI52(offset: _MemOffset): number;
/**
 * Throws for 0 > value > Number.MAX_SAFE_INTEGER
 */
declare function getU52(offset: _MemOffset): number;
declare function getI64Big(offset: _MemOffset): bigint;
declare function getF32(offset: _MemOffset): number;
declare function getF64(offset: _MemOffset): number;

declare function mono_run_main_and_exit(main_assembly_name: string, args: string[]): Promise<void>;
declare function mono_run_main(main_assembly_name: string, args: string[]): Promise<number>;

interface IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
}
declare class ManagedObject implements IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
    toString(): string;
}
declare class ManagedError extends Error implements IDisposable {
    constructor(message: string);
    get stack(): string | undefined;
    dispose(): void;
    get isDisposed(): boolean;
    toString(): string;
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

declare function mono_wasm_get_assembly_exports(assembly: string): Promise<any>;

declare const MONO: {
    mono_wasm_setenv: typeof mono_wasm_setenv;
    mono_wasm_load_bytes_into_heap: typeof mono_wasm_load_bytes_into_heap;
    mono_wasm_load_icu_data: typeof mono_wasm_load_icu_data;
    mono_wasm_runtime_ready: typeof mono_wasm_runtime_ready;
    mono_wasm_load_data_archive: typeof mono_wasm_load_data_archive;
    mono_wasm_load_config: typeof mono_wasm_load_config;
    mono_load_runtime_and_bcl_args: typeof mono_load_runtime_and_bcl_args;
    mono_wasm_new_root_buffer: typeof mono_wasm_new_root_buffer;
    mono_wasm_new_root: typeof mono_wasm_new_root;
    mono_wasm_new_external_root: typeof mono_wasm_new_external_root;
    mono_wasm_release_roots: typeof mono_wasm_release_roots;
    mono_run_main: typeof mono_run_main;
    mono_run_main_and_exit: typeof mono_run_main_and_exit;
    mono_wasm_get_assembly_exports: typeof mono_wasm_get_assembly_exports;
    mono_wasm_add_assembly: (name: string, data: VoidPtr, size: number) => number;
    mono_wasm_load_runtime: (unused: string, debug_level: number) => void;
    config: MonoConfig | MonoConfigError;
    loaded_files: string[];
    setB32: typeof setB32;
    setI8: typeof setI8;
    setI16: typeof setI16;
    setI32: typeof setI32;
    setI52: typeof setI52;
    setU52: typeof setU52;
    setI64Big: typeof setI64Big;
    setU8: typeof setU8;
    setU16: typeof setU16;
    setU32: typeof setU32;
    setF32: typeof setF32;
    setF64: typeof setF64;
    getB32: typeof getB32;
    getI8: typeof getI8;
    getI16: typeof getI16;
    getI32: typeof getI32;
    getI52: typeof getI52;
    getU52: typeof getU52;
    getI64Big: typeof getI64Big;
    getU8: typeof getU8;
    getU16: typeof getU16;
    getU32: typeof getU32;
    getF32: typeof getF32;
    getF64: typeof getF64;
    diagnostics: Diagnostics;
};
declare type MONOType = typeof MONO;
declare const BINDING: {
    /**
     * @deprecated Not GC or thread safe
     */
    mono_obj_array_new: (size: number) => MonoArray;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_obj_array_set: (array: MonoArray, idx: number, obj: MonoObject) => void;
    /**
     * @deprecated Not GC or thread safe
     */
    js_string_to_mono_string: typeof js_string_to_mono_string;
    /**
     * @deprecated Not GC or thread safe
     */
    js_typed_array_to_array: typeof js_typed_array_to_array;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_array_to_js_array: typeof mono_array_to_js_array;
    /**
     * @deprecated Not GC or thread safe
     */
    js_to_mono_obj: typeof js_to_mono_obj;
    /**
     * @deprecated Not GC or thread safe
     */
    conv_string: typeof conv_string;
    /**
     * @deprecated Not GC or thread safe
     */
    unbox_mono_obj: typeof unbox_mono_obj;
    /**
     * @deprecated Renamed to conv_string_root
     */
    conv_string_rooted: typeof conv_string_root;
    mono_obj_array_new_ref: (size: number, result: MonoObjectRef) => void;
    mono_obj_array_set_ref: (array: MonoObjectRef, idx: number, obj: MonoObjectRef) => void;
    js_string_to_mono_string_root: typeof js_string_to_mono_string_root;
    js_typed_array_to_array_root: typeof js_typed_array_to_array_root;
    js_to_mono_obj_root: typeof js_to_mono_obj_root;
    conv_string_root: typeof conv_string_root;
    unbox_mono_obj_root: typeof unbox_mono_obj_root;
    mono_array_root_to_js_array: typeof mono_array_root_to_js_array;
    bind_static_method: typeof mono_bind_static_method;
    call_assembly_entry_point: typeof mono_call_assembly_entry_point;
};
declare type BINDINGType = typeof BINDING;
interface DotnetPublicAPI {
    MONO: typeof MONO;
    BINDING: typeof BINDING;
    INTERNAL: any;
    EXPORTS: any;
    IMPORTS: any;
    Module: EmscriptenModule;
    RuntimeId: number;
    RuntimeBuildInfo: {
        ProductVersion: string;
        Configuration: string;
    };
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
 * User could dispose it manualy.
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

export { ArraySegment, BINDINGType, CreateDotnetRuntimeType, DotnetModuleConfig, DotnetPublicAPI, EmscriptenModule, IMemoryView, MONOType, ManagedError, ManagedObject, MemoryViewType, MonoArray, MonoObject, MonoString, Span, VoidPtr, createDotnetRuntime as default };
