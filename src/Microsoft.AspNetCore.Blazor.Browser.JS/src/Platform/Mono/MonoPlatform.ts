import { MethodHandle, System_Object, System_String, System_Array, Pointer, Platform } from '../Platform';
import { getAssemblyNameFromUrl } from '../DotNet';
import { getRegisteredFunction } from '../../Interop/RegisteredFunction';

let assembly_load: (assemblyName: string) => number;
let find_class: (assemblyHandle: number, namespace: string, className: string) => number;
let find_method: (typeHandle: number, methodName: string, unknownArg: number) => MethodHandle;
let invoke_method: (method: MethodHandle, target: System_Object, argsArrayPtr: number, exceptionFlagIntPtr: number) => System_Object;
let mono_string_get_utf8: (managedString: System_String) => Mono.Utf8Ptr;
let mono_string: (jsString: string) => System_String;

export const monoPlatform: Platform = {
  start: function start(loadAssemblyUrls: string[]) {
    return new Promise<void>((resolve, reject) => {
      // mono.js assumes the existence of this
      window['Browser'] = {
        init: () => { },
        asyncLoad: asyncLoad
      };

      // Emscripten works by expecting the module config to be a global
      window['Module'] = createEmscriptenModuleInstance(loadAssemblyUrls, resolve, reject);

      addScriptTagsToDocument();
    });
  },

  findMethod: function findMethod(assemblyName: string, namespace: string, className: string, methodName: string): MethodHandle {
    // TODO: Cache the assembly_load outputs?
    const assemblyHandle = assembly_load(assemblyName);
    if (!assemblyHandle) {
      throw new Error(`Could not find assembly "${assemblyName}"`);
    }

    const typeHandle = find_class(assemblyHandle, namespace, className);
    if (!typeHandle) {
      throw new Error(`Could not find type "${className}" in namespace "${namespace}" in assembly "${assemblyName}"`);
    }

    const methodHandle = find_method(typeHandle, methodName, -1);
    if (!methodHandle) {
      throw new Error(`Could not find method "${methodName}" on type "${namespace}.${className}"`);
    }

    return methodHandle;
  },

  callEntryPoint: function callEntryPoint(assemblyName: string, entrypointMethod: string, args: System_Object[]): void {
    // Parse the entrypointMethod, which is of the form MyApp.MyNamespace.MyTypeName::MyMethodName
    // Note that we don't support specifying a method overload, so it has to be unique
    const entrypointSegments = entrypointMethod.split('::');
    if (entrypointSegments.length != 2) {
      throw new Error('Malformed entry point method name; could not resolve class name and method name.');
    }
    const typeFullName = entrypointSegments[0];
    const methodName = entrypointSegments[1];
    const lastDot = typeFullName.lastIndexOf('.');
    const namespace = lastDot > -1 ? typeFullName.substring(0, lastDot) : '';
    const typeShortName = lastDot > -1 ? typeFullName.substring(lastDot + 1) : typeFullName;

    const entryPointMethodHandle = monoPlatform.findMethod(assemblyName, namespace, typeShortName, methodName);
    monoPlatform.callMethod(entryPointMethodHandle, null, args);
  },

  callMethod: function callMethod(method: MethodHandle, target: System_Object, args: System_Object[]): System_Object {
    if (args.length > 4) {
      // Hopefully this restriction can be eased soon, but for now make it clear what's going on
      throw new Error(`Currently, MonoPlatform supports passing a maximum of 4 arguments from JS to .NET. You tried to pass ${args.length}.`);
    }

    const stack = Module.Runtime.stackSave();

    try {
      const argsBuffer = Module.Runtime.stackAlloc(args.length);
      const exceptionFlagManagedInt = Module.Runtime.stackAlloc(4);
      for (var i = 0; i < args.length; ++i) {
        Module.setValue(argsBuffer + i * 4, args[i], 'i32');
      }
      Module.setValue(exceptionFlagManagedInt, 0, 'i32');

      const res = invoke_method(method, target, argsBuffer, exceptionFlagManagedInt);

      if (Module.getValue(exceptionFlagManagedInt, 'i32') !== 0) {
        // If the exception flag is set, the returned value is exception.ToString()
        throw new Error(monoPlatform.toJavaScriptString(<System_String>res));
      }

      return res;
    } finally {
      Module.Runtime.stackRestore(stack);
    }
  },

  toJavaScriptString: function toJavaScriptString(managedString: System_String) {
    // Comments from original Mono sample:
    //FIXME this is wastefull, we could remove the temp malloc by going the UTF16 route
    //FIXME this is unsafe, cuz raw objects could be GC'd.

    const utf8 = mono_string_get_utf8(managedString);
    const res = Module.UTF8ToString(utf8);
    Module._free(utf8 as any);
    return res;
  },

  toDotNetString: function toDotNetString(jsString: string): System_String {
    return mono_string(jsString);
  },

  getArrayLength: function getArrayLength(array: System_Array<any>): number {
    return Module.getValue(getArrayDataPointer(array), 'i32');
  },

  getArrayEntryPtr: function getArrayEntryPtr<TPtr extends Pointer>(array: System_Array<TPtr>, index: number, itemSize: number): TPtr {
    // First byte is array length, followed by entries
    const address = getArrayDataPointer(array) + 4 + index * itemSize;
    return address as any as TPtr;
  },

  getObjectFieldsBaseAddress: function getObjectFieldsBaseAddress(referenceTypedObject: System_Object): Pointer {
    // The first two int32 values are internal Mono data
    return (referenceTypedObject as any as number + 8) as any as Pointer;
  },

  readInt32Field: function readHeapInt32(baseAddress: Pointer, fieldOffset?: number): number {
    return Module.getValue((baseAddress as any as number) + (fieldOffset || 0), 'i32');
  },

  readObjectField: function readHeapObject<T extends System_Object>(baseAddress: Pointer, fieldOffset?: number): T {
    return Module.getValue((baseAddress as any as number) + (fieldOffset || 0), 'i32') as any as T;
  },

  readStringField: function readHeapObject(baseAddress: Pointer, fieldOffset?: number): string | null {
    const fieldValue = Module.getValue((baseAddress as any as number) + (fieldOffset || 0), 'i32');
    return fieldValue === 0 ? null : monoPlatform.toJavaScriptString(fieldValue as any as System_String);
  },

  readStructField: function readStructField<T extends Pointer>(baseAddress: Pointer, fieldOffset?: number): T {
    return ((baseAddress as any as number) + (fieldOffset || 0)) as any as T;
  },
};

// Bypass normal type checking to add this extra function. It's only intended to be called from
// the JS code in Mono's driver.c. It's never intended to be called from TypeScript.
(monoPlatform as any).monoGetRegisteredFunction = getRegisteredFunction;

function addScriptTagsToDocument() {
  // Load either the wasm or asm.js version of the Mono runtime
  const browserSupportsNativeWebAssembly = typeof WebAssembly !== 'undefined' && WebAssembly.validate;
  const monoRuntimeUrlBase = '_framework/' + (browserSupportsNativeWebAssembly ? 'wasm' : 'asmjs');
  const monoRuntimeScriptUrl = `${monoRuntimeUrlBase}/mono.js`;

  if (!browserSupportsNativeWebAssembly) {
    // In the asmjs case, the initial memory structure is in a separate file we need to download
    const meminitXHR = Module['memoryInitializerRequest'] = new XMLHttpRequest();
    meminitXHR.open('GET', `${monoRuntimeUrlBase}/mono.js.mem`);
    meminitXHR.responseType = 'arraybuffer';
    meminitXHR.send(null);
  }

  document.write(`<script defer src="${monoRuntimeScriptUrl}"></script>`);
}

function createEmscriptenModuleInstance(loadAssemblyUrls: string[], onReady: () => void, onError: (reason?: any) => void) {
  const module = {} as typeof Module;

  module.print = line => console.log(`WASM: ${line}`);
  module.printErr = line => console.error(`WASM: ${line}`);
  module.wasmBinaryFile = '_framework/wasm/mono.wasm';
  module.asmjsCodeFile = '_framework/asmjs/mono.asm.js';
  module.preRun = [];
  module.postRun = [];
  module.preloadPlugins = [];

  module.preRun.push(() => {
    // By now, emscripten should be initialised enough that we can capture these methods for later use
    assembly_load = Module.cwrap('mono_wasm_assembly_load', 'number', ['string']);
    find_class = Module.cwrap('mono_wasm_assembly_find_class', 'number', ['number', 'string', 'string']);
    find_method = Module.cwrap('mono_wasm_assembly_find_method', 'number', ['number', 'string', 'number']);
    invoke_method = Module.cwrap('mono_wasm_invoke_method', 'number', ['number', 'number', 'number']);
    mono_string_get_utf8 = Module.cwrap('mono_wasm_string_get_utf8', 'number', ['number']);
    mono_string = Module.cwrap('mono_wasm_string_from_js', 'number', ['string']);

    Module.FS_createPath('/', 'appBinDir', true, true);
    loadAssemblyUrls.forEach(url =>
      FS.createPreloadedFile('appBinDir', `${getAssemblyNameFromUrl(url)}.dll`, url, true, false, undefined, onError));
  });

  module.postRun.push(() => {
    const load_runtime = Module.cwrap('mono_wasm_load_runtime', null, ['string']);
    load_runtime('appBinDir');
    onReady();
  });

  return module;
}

function asyncLoad(url, onload, onerror) {
  var xhr = new XMLHttpRequest;
  xhr.open('GET', url, /* async: */ true);
  xhr.responseType = 'arraybuffer';
  xhr.onload = function xhr_onload() {
    if (xhr.status == 200 || xhr.status == 0 && xhr.response) {
      var asm = new Uint8Array(xhr.response);
      onload(asm);
    } else {
      onerror(xhr);
    }
  };
  xhr.onerror = onerror;
  xhr.send(null);
}

function getArrayDataPointer<T>(array: System_Array<T>): number {
  return <number><any>array + 12; // First byte from here is length, then following bytes are entries
}
