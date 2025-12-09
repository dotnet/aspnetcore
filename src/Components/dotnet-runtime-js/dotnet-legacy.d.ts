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
type MemOffset = number | VoidPtr | NativePointer | ManagedPointer;
type NumberOrPointer = number | VoidPtr | NativePointer | ManagedPointer;
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
interface WasmRootBuffer {
    get_address(index: number): MonoObjectRef;
    get_address_32(index: number): number;
    get(index: number): ManagedPointer;
    set(index: number, value: ManagedPointer): ManagedPointer;
    copy_value_from_address(index: number, sourceAddress: MonoObjectRef): void;
    clear(): void;
    release(): void;
    toString(): string;
}

/**
 * @deprecated Please use methods in top level API object instead
 */
type BINDINGType = {
    /**
     * @deprecated Please use [JSExportAttribute] instead
     */
    bind_static_method: (fqn: string, signature?: string) => Function;
    /**
     * @deprecated Please use runMain() instead
     */
    call_assembly_entry_point: (assembly: string, args?: any[], signature?: string) => number;
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
    js_string_to_mono_string: (string: string) => MonoString;
    /**
     * @deprecated Not GC or thread safe
     */
    js_typed_array_to_array: (js_obj: any) => MonoArray;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_array_to_js_array: (mono_array: MonoArray) => any[] | null;
    /**
     * @deprecated Not GC or thread safe
     */
    js_to_mono_obj: (js_obj: any) => MonoObject;
    /**
     * @deprecated Not GC or thread safe
     */
    conv_string: (mono_obj: MonoString) => string | null;
    /**
     * @deprecated Not GC or thread safe
     */
    unbox_mono_obj: (mono_obj: MonoObject) => any;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_obj_array_new_ref: (size: number, result: MonoObjectRef) => void;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_obj_array_set_ref: (array: MonoObjectRef, idx: number, obj: MonoObjectRef) => void;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    js_string_to_mono_string_root: (string: string, result: WasmRoot<MonoString>) => void;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    js_typed_array_to_array_root: (js_obj: any, result: WasmRoot<MonoArray>) => void;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    js_to_mono_obj_root: (js_obj: any, result: WasmRoot<MonoObject>, should_add_in_flight: boolean) => void;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    conv_string_root: (root: WasmRoot<MonoString>) => string | null;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    unbox_mono_obj_root: (root: WasmRoot<any>) => any;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_array_root_to_js_array: (arrayRoot: WasmRoot<MonoArray>) => any[] | null;
};
/**
 * @deprecated Please use methods in top level API object instead
 */
type MONOType = {
    /**
     * @deprecated Please use setEnvironmentVariable() instead
     */
    mono_wasm_setenv: (name: string, value: string) => void;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_bytes_into_heap: (bytes: Uint8Array) => VoidPtr;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_icu_data: (offset: VoidPtr) => boolean;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_runtime_ready: () => void;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_new_root_buffer: (capacity: number, name?: string) => WasmRootBuffer;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_new_root: <T extends MonoObject>(value?: T | undefined) => WasmRoot<T>;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_new_external_root: <T extends MonoObject>(address: VoidPtr | MonoObjectRef) => WasmRoot<T>;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_release_roots: (...args: WasmRoot<any>[]) => void;
    /**
     * @deprecated Please use runMain instead
     */
    mono_run_main: (main_assembly_name: string, args: string[]) => Promise<number>;
    /**
     * @deprecated Please use runMainAndExit instead
     */
    mono_run_main_and_exit: (main_assembly_name: string, args: string[]) => Promise<number>;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_add_assembly: (name: string, data: VoidPtr, size: number) => number;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_runtime: (unused: string, debugLevel: number) => void;
    /**
     * @deprecated Please use getConfig() instead
     */
    config: any;
    /**
     * @deprecated Please use config.assets instead
     */
    loaded_files: string[];
    /**
     * @deprecated Please use setHeapB32
     */
    setB32: (offset: MemOffset, value: number | boolean) => void;
    /**
     * @deprecated Please use setHeapI8
     */
    setI8: (offset: MemOffset, value: number) => void;
    /**
     * @deprecated Please use setHeapI16
     */
    setI16: (offset: MemOffset, value: number) => void;
    /**
     * @deprecated Please use setHeapI32
     */
    setI32: (offset: MemOffset, value: number) => void;
    /**
     * @deprecated Please use setHeapI52
     */
    setI52: (offset: MemOffset, value: number) => void;
    /**
     * @deprecated Please use setHeapU52
     */
    setU52: (offset: MemOffset, value: number) => void;
    /**
     * @deprecated Please use setHeapI64Big
     */
    setI64Big: (offset: MemOffset, value: bigint) => void;
    /**
     * @deprecated Please use setHeapU8
     */
    setU8: (offset: MemOffset, value: number) => void;
    /**
     * @deprecated Please use setHeapU16
     */
    setU16: (offset: MemOffset, value: number) => void;
    /**
     * @deprecated Please use setHeapU32
     */
    setU32: (offset: MemOffset, value: NumberOrPointer) => void;
    /**
     * @deprecated Please use setHeapF32
     */
    setF32: (offset: MemOffset, value: number) => void;
    /**
     * @deprecated Please use setHeapF64
     */
    setF64: (offset: MemOffset, value: number) => void;
    /**
     * @deprecated Please use getHeapB32
     */
    getB32: (offset: MemOffset) => boolean;
    /**
     * @deprecated Please use getHeapI8
     */
    getI8: (offset: MemOffset) => number;
    /**
     * @deprecated Please use getHeapI16
     */
    getI16: (offset: MemOffset) => number;
    /**
     * @deprecated Please use getHeapI32
     */
    getI32: (offset: MemOffset) => number;
    /**
     * @deprecated Please use getHeapI52
     */
    getI52: (offset: MemOffset) => number;
    /**
     * @deprecated Please use getHeapU52
     */
    getU52: (offset: MemOffset) => number;
    /**
     * @deprecated Please use getHeapI64Big
     */
    getI64Big: (offset: MemOffset) => bigint;
    /**
     * @deprecated Please use getHeapU8
     */
    getU8: (offset: MemOffset) => number;
    /**
     * @deprecated Please use getHeapU16
     */
    getU16: (offset: MemOffset) => number;
    /**
     * @deprecated Please use getHeapU32
     */
    getU32: (offset: MemOffset) => number;
    /**
     * @deprecated Please use getHeapF32
     */
    getF32: (offset: MemOffset) => number;
    /**
     * @deprecated Please use getHeapF64
     */
    getF64: (offset: MemOffset) => number;
};

export { BINDINGType, MONOType, MonoArray, MonoObject, MonoString };
