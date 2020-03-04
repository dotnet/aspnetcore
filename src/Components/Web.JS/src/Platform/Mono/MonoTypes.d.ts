declare namespace Module {
  function UTF8ToString(utf8: Mono.Utf8Ptr): string;
  var preloadPlugins: any[];

  function stackSave(): Mono.StackSaveHandle;
  function stackAlloc(length: number): number;
  function stackRestore(handle: Mono.StackSaveHandle): void;

  // These should probably be in @types/emscripten
  function FS_createPath(parent, path, canRead, canWrite);
  function FS_createDataFile(parent, name, data, canRead, canWrite, canOwn);

  function mono_bind_static_method(fqn: string): BoundStaticMethod;
}

declare namespace Mono {
  interface DotNetPointer { DotnetPoint__DO_NOT_IMPLMENT: any }
  interface Utf8Ptr { Utf8Ptr__DO_NOT_IMPLEMENT: any }
  interface StackSaveHandle { StackSaveHandle__DO_NOT_IMPLEMENT: any }
}

// Mono uses this global to hang various debugging-related items on
declare namespace MONO {
  var loaded_files: string[];
  var mono_wasm_runtime_is_ready: boolean;
  function mono_wasm_runtime_ready (): void;
  function mono_wasm_setenv (name: string, value: string): void;
}

// mono_bind_static_method allows arbitrary JS data types to be sent over the wire. However we are
// artifically limiting it to a subset of types that we actually use.
declare type BoundStaticMethod = (...args: (string | number | null)[]) => (string | number | null);

declare namespace BINDING {
  function js_string_to_mono_string(jsString: string): Pointer;
  function js_typed_array_to_array(array: Uint8Array): Pointer;
  function js_typed_array_to_array<T>(array: Array<T>): Pointer;
  function conv_string(dotnetString: System_String | null): string | null;
}

// We don't actually instantiate any of these at runtime. For perf it's preferable to
// use the original 'number' instances without any boxing. The definitions are just
// for compile-time checking, since TypeScript doesn't support nominal types.
declare interface MethodHandle { MethodHandle__DO_NOT_IMPLEMENT: any }
declare interface System_Object { System_Object__DO_NOT_IMPLEMENT: any }
declare interface System_String extends System_Object { System_String__DO_NOT_IMPLEMENT: any }
declare interface System_Array<T> extends System_Object { System_Array__DO_NOT_IMPLEMENT: any }
declare interface Pointer { Pointer__DO_NOT_IMPLEMENT: any }
