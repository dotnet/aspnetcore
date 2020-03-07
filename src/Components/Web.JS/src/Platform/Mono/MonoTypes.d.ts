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

// Emscripten declares these globals
declare const addRunDependency: any;
declare const removeRunDependency: any;

declare namespace Mono {
  interface Utf8Ptr { Utf8Ptr__DO_NOT_IMPLEMENT: any }
  interface StackSaveHandle { StackSaveHandle__DO_NOT_IMPLEMENT: any }
}

// Mono uses this global to hang various debugging-related items on
declare namespace MONO {
  var loaded_files: string[];
  var mono_wasm_runtime_is_ready: boolean;
  function mono_wasm_setenv (name: string, value: string): void;
}

// mono_bind_static_method allows arbitrary JS data types to be sent over the wire. However we are
// artifically limiting it to a subset of types that we actually use.
declare type BoundStaticMethod = (...args: (string | number | null)[]) => (string | number | null);
