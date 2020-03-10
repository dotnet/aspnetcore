import { Pointer, System_String } from '../Platform';

// Mono uses this global to hang various debugging-related items on
export declare namespace MONO {
  var loaded_files: string[];
  var mono_wasm_runtime_is_ready: boolean;
  function mono_wasm_runtime_ready (): void;
  function mono_wasm_setenv (name: string, value: string): void;
}

// Mono uses this global to hold low-level interop APIs
export declare namespace BINDING {
  function js_string_to_mono_string(jsString: string): Pointer;
  function js_typed_array_to_array(array: Uint8Array): Pointer;
  function js_typed_array_to_array<T>(array: Array<T>): Pointer;
  function conv_string(dotnetString: System_String | null): string | null;
  function bind_static_method(fqn: string, signature?: string): Function;
}
