declare namespace Module {
  function UTF8ToString(utf8: Mono.Utf8Ptr): string;
  var preloadPlugins: any[];

  function stackSave(): Mono.StackSaveHandle;
  function stackAlloc(length: number): number;
  function stackRestore(handle: Mono.StackSaveHandle): void;

  // These should probably be in @types/emscripten
  function FS_createPath(parent, path, canRead, canWrite);
  function FS_createDataFile(parent, name, data, canRead, canWrite, canOwn);
}

declare namespace Mono {
  interface Utf8Ptr { Utf8Ptr__DO_NOT_IMPLEMENT: any }
  interface StackSaveHandle { StackSaveHandle__DO_NOT_IMPLEMENT: any }
}
