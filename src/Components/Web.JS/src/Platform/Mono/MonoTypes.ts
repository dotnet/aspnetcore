import { Pointer, System_String } from '../Platform';

// Mono uses this global to hang various debugging-related items on

declare interface MONO {
  loaded_files: string[];
  mono_wasm_runtime_ready (): void;
  mono_wasm_setenv (name: string, value: string): void;
}

// Mono uses this global to hold low-level interop APIs
declare interface BINDING {
  js_string_to_mono_string(jsString: string): Pointer;
  js_typed_array_to_array(array: Uint8Array): Pointer;
  js_typed_array_to_array<T>(array: Array<T>): Pointer;
  conv_string(dotnetString: System_String | null): string | null;
  bind_static_method(fqn: string, signature?: string): Function;
}

declare global {
  var MONO: MONO;
  var BINDING: BINDING;
}