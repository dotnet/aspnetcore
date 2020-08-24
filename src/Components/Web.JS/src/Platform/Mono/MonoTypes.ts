import { Pointer, System_String, System_Array, System_Object } from '../Platform';

// Mono uses this global to hang various debugging-related items on

declare interface MONO {
  loaded_files: string[];
  mono_wasm_runtime_ready (): void;
  mono_wasm_setenv (name: string, value: string): void;
  mono_wasm_load_data_archive(data: Uint8Array, prefix: string): void;
  mono_wasm_load_bytes_into_heap (data: Uint8Array): Pointer;
  mono_wasm_load_icu_data(heapAddress: Pointer): boolean;
}

// Mono uses this global to hold low-level interop APIs
declare interface BINDING {
  mono_obj_array_new(length: number): System_Array<System_Object>;
  mono_obj_array_set(array: System_Array<System_Object>, index: Number, value: System_Object): void;
  js_string_to_mono_string(jsString: string): System_String;
  js_typed_array_to_array(array: Uint8Array): System_Object;
  js_to_mono_obj(jsObject: any) : System_Object;
  mono_array_to_js_array<TInput, TOutput>(array: System_Array<TInput>) : Array<TOutput>;
  conv_string(dotnetString: System_String | null): string | null;
  bind_static_method(fqn: string, signature?: string): Function;
  unbox_mono_obj(object: System_Object): any;
}

declare global {
  var MONO: MONO;
  var BINDING: BINDING;
}

