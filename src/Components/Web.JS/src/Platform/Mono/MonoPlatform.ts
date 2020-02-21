import { System_Object, System_String, System_Array, Pointer, Platform } from '../Platform';
import { getFileNameFromUrl } from '../Url';
import { attachDebuggerHotkey, hasDebuggingEnabled } from './MonoDebugger';
import { showErrorNotification } from '../../BootErrors';

let mono_string_get_utf8: (managedString: System_String) => Mono.Utf8Ptr;
const appBinDirName = 'appBinDir';
const uint64HighOrderShift = Math.pow(2, 32);
const maxSafeNumberHighPart = Math.pow(2, 21) - 1; // The high-order int32 from Number.MAX_SAFE_INTEGER

export const monoPlatform: Platform = {
  start: function start(loadAssemblyUrls: string[]) {
    return new Promise<void>((resolve, reject) => {
      attachDebuggerHotkey(loadAssemblyUrls);

      // dotnet.js assumes the existence of this
      window['Browser'] = {
        init: () => { },
      };

      // Emscripten works by expecting the module config to be a global
      // For compatibility with macOS Catalina, we have to assign a temporary value to window.Module
      // before we start loading the WebAssembly files
      addGlobalModuleScriptTagsToDocument(() => {
        window['Module'] = createEmscriptenModuleInstance(loadAssemblyUrls, resolve, reject);
        addScriptTagsToDocument();
      });
    });
  },

  callEntryPoint: function callEntryPoint(assemblyName: string) {
    // Instead of using Module.mono_call_assembly_entry_point, we have our own logic for invoking
    // the entrypoint which adds support for async main.
    // Currently we disregard the return value from the entrypoint, whether it's sync or async.
    // In the future, we might want Blazor.start to return a Promise<Promise<value>>, where the
    // outer promise reflects the startup process, and the inner one reflects the possibly-async
    // .NET entrypoint method.
    const invokeEntrypoint = bindStaticMethod('Microsoft.AspNetCore.Blazor', 'Microsoft.AspNetCore.Blazor.Hosting.EntrypointInvoker', 'InvokeEntrypoint');
    // Note we're passing in null because passing arrays is problematic until https://github.com/mono/mono/issues/18245 is resolved.
    invokeEntrypoint(assemblyName, null);
  },

  toJavaScriptString: function toJavaScriptString(managedString: System_String) {
    // Comments from original Mono sample:
    // FIXME this is wastefull, we could remove the temp malloc by going the UTF16 route
    // FIXME this is unsafe, cuz raw objects could be GC'd.

    const utf8 = mono_string_get_utf8(managedString);
    const res = Module.UTF8ToString(utf8);
    Module._free(utf8 as any);
    return res;
  },

  toUint8Array: function toUint8Array(array: System_Array<any>): Uint8Array {
    const dataPtr = getArrayDataPointer(array);
    const length = Module.getValue(dataPtr, 'i32');
    return new Uint8Array(Module.HEAPU8.buffer, dataPtr + 4, length);
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

  readInt16Field: function readHeapInt16(baseAddress: Pointer, fieldOffset?: number): number {
    return Module.getValue((baseAddress as any as number) + (fieldOffset || 0), 'i16');
  },

  readInt32Field: function readHeapInt32(baseAddress: Pointer, fieldOffset?: number): number {
    return Module.getValue((baseAddress as any as number) + (fieldOffset || 0), 'i32');
  },

  readUint64Field: function readHeapUint64(baseAddress: Pointer, fieldOffset?: number): number {
    // Module.getValue(..., 'i64') doesn't work because the implementation treats 'i64' as
    // being the same as 'i32'. Also we must take care to read both halves as unsigned.
    const address = (baseAddress as any as number) + (fieldOffset || 0);
    const heapU32Index = address >> 2;
    const highPart = Module.HEAPU32[heapU32Index + 1];
    if (highPart > maxSafeNumberHighPart) {
      throw new Error(`Cannot read uint64 with high order part ${highPart}, because the result would exceed Number.MAX_SAFE_INTEGER.`);
    }

    return (highPart * uint64HighOrderShift) + Module.HEAPU32[heapU32Index];
  },

  readFloatField: function readHeapFloat(baseAddress: Pointer, fieldOffset?: number): number {
    return Module.getValue((baseAddress as any as number) + (fieldOffset || 0), 'float');
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

function addScriptTagsToDocument() {
  const browserSupportsNativeWebAssembly = typeof WebAssembly !== 'undefined' && WebAssembly.validate;
  if (!browserSupportsNativeWebAssembly) {
    throw new Error('This browser does not support WebAssembly.');
  }

  const scriptElem = document.createElement('script');
  scriptElem.src = '_framework/wasm/dotnet.js';
  scriptElem.defer = true;
  document.body.appendChild(scriptElem);
}

// Due to a strange behavior in macOS Catalina, we have to delay loading the WebAssembly files
// until after it finishes evaluating a <script> element that assigns a value to window.Module.
// This may be fixed in a later version of macOS/iOS, or even if not it may be possible to reduce
// this to a smaller workaround.
function addGlobalModuleScriptTagsToDocument(callback: () => void) {
  const scriptElem = document.createElement('script');

  // This pollutes global but is needed so it can be called from the script.
  // The callback is put in the global scope so that it can be run after the script is loaded.
  // onload cannot be used in this case for non-file scripts.
  window['__wasmmodulecallback__'] = callback;
  scriptElem.type = 'text/javascript';
  scriptElem.text = 'var Module; window.__wasmmodulecallback__(); delete window.__wasmmodulecallback__;';

  document.body.appendChild(scriptElem);
}

function createEmscriptenModuleInstance(loadAssemblyUrls: string[], onReady: () => void, onError: (reason?: any) => void) {
  const module = {} as typeof Module;
  const wasmBinaryFile = '_framework/wasm/dotnet.wasm';
  const suppressMessages = ['DEBUGGING ENABLED'];

  module.print = line => (suppressMessages.indexOf(line) < 0 && console.log(`WASM: ${line}`));

  module.printErr = line => {
    console.error(`WASM: ${line}`);
    showErrorNotification();
  };
  module.preRun = [];
  module.postRun = [];
  module.preloadPlugins = [];

  module.locateFile = fileName => {
    switch (fileName) {
      case 'dotnet.wasm': return wasmBinaryFile;
      default: return fileName;
    }
  };

  module.preRun.push(() => {
    // By now, emscripten should be initialised enough that we can capture these methods for later use
    const mono_wasm_add_assembly = Module.cwrap('mono_wasm_add_assembly', null, [
      'string',
      'number',
      'number',
    ]);

    mono_string_get_utf8 = Module.cwrap('mono_wasm_string_get_utf8', 'number', ['number']);

    MONO.loaded_files = [];

    loadAssemblyUrls.forEach(url => {
      const filename = getFileNameFromUrl(url);
      const runDependencyId = `blazor:${filename}`;
      addRunDependency(runDependencyId);
      asyncLoad(url).then(
        data => {
          const heapAddress = Module._malloc(data.length);
          const heapMemory = new Uint8Array(Module.HEAPU8.buffer, heapAddress, data.length);
          heapMemory.set(data);
          mono_wasm_add_assembly(filename, heapAddress, data.length);
          MONO.loaded_files.push(toAbsoluteUrl(url));
          removeRunDependency(runDependencyId);
        },
        errorInfo => {
          // If it's a 404 on a .pdb, we don't want to block the app from starting up.
          // We'll just skip that file and continue (though the 404 is logged in the console).
          // This happens if you build a Debug build but then run in Production environment.
          const isPdb404 = errorInfo instanceof XMLHttpRequest
            && errorInfo.status === 404
            && filename.match(/\.pdb$/);
          if (!isPdb404) {
            onError(errorInfo);
          }
          removeRunDependency(runDependencyId);
        }
      );
    });
  });

  module.postRun.push(() => {
    MONO.mono_wasm_setenv("MONO_URI_DOTNETRELATIVEORABSOLUTE", "true");
    const load_runtime = Module.cwrap('mono_wasm_load_runtime', null, ['string', 'number']);
    load_runtime(appBinDirName, hasDebuggingEnabled() ? 1 : 0);
    MONO.mono_wasm_runtime_is_ready = true;
    attachInteropInvoker();
    onReady();
  });

  return module;
}

const anchorTagForAbsoluteUrlConversions = document.createElement('a');
function toAbsoluteUrl(possiblyRelativeUrl: string) {
  anchorTagForAbsoluteUrlConversions.href = possiblyRelativeUrl;
  return anchorTagForAbsoluteUrlConversions.href;
}

function asyncLoad(url: string) {
  return new Promise<Uint8Array>((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('GET', url, /* async: */ true);
    xhr.responseType = 'arraybuffer';
    xhr.onload = function xhr_onload() {
      if (xhr.status == 200 || xhr.status == 0 && xhr.response) {
        const asm = new Uint8Array(xhr.response);
        resolve(asm);
      } else {
        reject(xhr);
      }
    };
    xhr.onerror = reject;
    xhr.send(undefined);
  });
}

function getArrayDataPointer<T>(array: System_Array<T>): number {
  return <number><any>array + 12; // First byte from here is length, then following bytes are entries
}

function bindStaticMethod(assembly: string, typeName: string, method: string) : (...args: any[]) => any {
  // Fully qualified name looks like this: "[debugger-test] Math:IntAdd"
  const fqn = `[${assembly}] ${typeName}:${method}`;
  return Module.mono_bind_static_method(fqn);
}

function attachInteropInvoker(): void {
  const dotNetDispatcherInvokeMethodHandle =  bindStaticMethod('Mono.WebAssembly.Interop', 'Mono.WebAssembly.Interop.MonoWebAssemblyJSRuntime', 'InvokeDotNet');
  const dotNetDispatcherBeginInvokeMethodHandle = bindStaticMethod('Mono.WebAssembly.Interop', 'Mono.WebAssembly.Interop.MonoWebAssemblyJSRuntime', 'BeginInvokeDotNet');
  const dotNetDispatcherEndInvokeJSMethodHandle = bindStaticMethod('Mono.WebAssembly.Interop', 'Mono.WebAssembly.Interop.MonoWebAssemblyJSRuntime', 'EndInvokeJS');

  DotNet.attachDispatcher({
    beginInvokeDotNetFromJS: (callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: any | null, argsJson: string): void => {
      if (!dotNetObjectId && !assemblyName) {
        throw new Error('Either assemblyName or dotNetObjectId must have a non null value.');
      }
      // As a current limitation, we can only pass 4 args. Fortunately we only need one of
      // 'assemblyName' or 'dotNetObjectId', so overload them in a single slot
      const assemblyNameOrDotNetObjectId: string = dotNetObjectId
        ? dotNetObjectId.toString()
        : assemblyName;

        dotNetDispatcherBeginInvokeMethodHandle(
          callId ? callId.toString() : null,
          assemblyNameOrDotNetObjectId,
          methodIdentifier,
          argsJson,
        );
    },
    endInvokeJSFromDotNet: (asyncHandle, succeeded, serializedArgs): void => {
      dotNetDispatcherEndInvokeJSMethodHandle(
        serializedArgs
      );
    },
    invokeDotNetFromJS: (assemblyName, methodIdentifier, dotNetObjectId, argsJson) => {
      return dotNetDispatcherInvokeMethodHandle(
        assemblyName ? assemblyName : null,
        methodIdentifier,
        dotNetObjectId ? dotNetObjectId.toString() : null,
        argsJson,
      ) as string;
    },
  });
}
