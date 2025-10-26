// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/no-explicit-any */
/* eslint-disable @typescript-eslint/no-non-null-assertion */
/* eslint-disable no-prototype-builtins */
import { DotNet } from '@microsoft/dotnet-js-interop';
import { attachDebuggerHotkey } from './MonoDebugger';
import { showErrorNotification } from '../../BootErrors';
import { Platform, System_Array, Pointer, System_Object, System_String, HeapLock, PlatformApi } from '../Platform';
import { WebAssemblyBootResourceType, WebAssemblyStartOptions } from '../WebAssemblyStartOptions';
import { Blazor } from '../../GlobalExports';
import { DotnetModuleConfig, EmscriptenModule, MonoConfig, ModuleAPI, RuntimeAPI, GlobalizationMode } from 'dotnet';
import { BINDINGType, MONOType } from 'dotnet/dotnet-legacy';
import { fetchAndInvokeInitializers } from '../../JSInitializers/JSInitializers.WebAssembly';
import { JSInitializer } from '../../JSInitializers/JSInitializers';
import { WebRendererId } from '../../Rendering/WebRendererId';

// initially undefined and only fully initialized after createEmscriptenModuleInstance()
export let BINDING: BINDINGType = undefined as any;
export let MONO: MONOType = undefined as any;
export let Module: DotnetModuleConfig & EmscriptenModule = undefined as any;
export let dispatcher: DotNet.ICallDispatcher = undefined as any;
let MONO_INTERNAL: any = undefined as any;
let runtime: RuntimeAPI = undefined as any;
let jsInitializer: JSInitializer;

const uint64HighOrderShift = Math.pow(2, 32);
const maxSafeNumberHighPart = Math.pow(2, 21) - 1; // The high-order int32 from Number.MAX_SAFE_INTEGER

let currentHeapLock: MonoHeapLock | null = null;

// Memory access helpers
// The implementations are exactly equivalent to what the global getValue(addr, type) function does,
// except without having to parse the 'type' parameter, and with less risk of mistakes at the call site
function getValueI16(ptr: number) {
  return MONO.getI16(ptr);
}
function getValueI32(ptr: number) {
  return MONO.getI32(ptr);
}
function getValueFloat(ptr: number) {
  return MONO.getF32(ptr);
}

export function getInitializer() {
  return jsInitializer;
}

function getValueU64(ptr: number) {
  // There is no Module.HEAPU64, and Module.getValue(..., 'i64') doesn't work because the implementation
  // treats 'i64' as being the same as 'i32'. Also we must take care to read both halves as unsigned.
  const heapU32Index = ptr >> 2;
  const highPart = Module.HEAPU32[heapU32Index + 1];
  if (highPart > maxSafeNumberHighPart) {
    throw new Error(`Cannot read uint64 with high order part ${highPart}, because the result would exceed Number.MAX_SAFE_INTEGER.`);
  }

  return (highPart * uint64HighOrderShift) + Module.HEAPU32[heapU32Index];
}

export const monoPlatform: Platform = {
  load: function load(options: Partial<WebAssemblyStartOptions>, onConfigLoaded?: (loadedConfig: MonoConfig) => void) {
    return createRuntimeInstance(options, onConfigLoaded);
  },

  start: function start() {
    return configureRuntimeInstance();
  },

  callEntryPoint: async function callEntryPoint(): Promise<any> {
    try {
      await runtime.runMain(runtime.getConfig().mainAssemblyName!, []);
    } catch (error) {
      console.error(error);
      showErrorNotification();
    }
  },

  toUint8Array: function toUint8Array(array: System_Array<any>): Uint8Array {
    const dataPtr = getArrayDataPointer(array);
    const length = getValueI32(dataPtr);
    const uint8Array = new Uint8Array(length);
    uint8Array.set(Module.HEAPU8.subarray(dataPtr + 4, dataPtr + 4 + length));
    return uint8Array;
  },

  getArrayLength: function getArrayLength(array: System_Array<any>): number {
    return getValueI32(getArrayDataPointer(array));
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
    return getValueI16((baseAddress as any as number) + (fieldOffset || 0));
  },

  readInt32Field: function readHeapInt32(baseAddress: Pointer, fieldOffset?: number): number {
    return getValueI32((baseAddress as unknown as number) + (fieldOffset || 0));
  },

  readUint64Field: function readHeapUint64(baseAddress: Pointer, fieldOffset?: number): number {
    return getValueU64((baseAddress as unknown as number) + (fieldOffset || 0));
  },

  readFloatField: function readHeapFloat(baseAddress: Pointer, fieldOffset?: number): number {
    return getValueFloat((baseAddress as unknown as number) + (fieldOffset || 0));
  },

  readObjectField: function readHeapObject<T extends System_Object>(baseAddress: Pointer, fieldOffset?: number): T {
    return getValueI32((baseAddress as unknown as number) + (fieldOffset || 0)) as any as T;
  },

  readStringField: function readHeapObject(baseAddress: Pointer, fieldOffset?: number, readBoolValueAsString?: boolean): string | null {
    const fieldValue = getValueI32((baseAddress as unknown as number) + (fieldOffset || 0));
    if (fieldValue === 0) {
      return null;
    }

    if (readBoolValueAsString) {
      // Some fields are stored as a union of bool | string | null values, but need to read as a string.
      // If the stored value is a bool, the behavior we want is empty string ('') for true, or null for false.
      const unboxedValue = BINDING.unbox_mono_obj(fieldValue as any as System_Object);
      if (typeof (unboxedValue) === 'boolean') {
        return unboxedValue ? '' : null;
      }
      return unboxedValue;
    }

    return BINDING.conv_string(fieldValue as any as System_String);
  },

  readStructField: function readStructField<T extends Pointer>(baseAddress: Pointer, fieldOffset?: number): T {
    return ((baseAddress as any as number) + (fieldOffset || 0)) as any as T;
  },

  beginHeapLock: function beginHeapLock() {
    assertHeapIsNotLocked();
    currentHeapLock = MonoHeapLock.create();
    return currentHeapLock;
  },

  invokeWhenHeapUnlocked: function invokeWhenHeapUnlocked(callback) {
    // This is somewhat like a sync context. If we're not locked, just pass through the call directly.
    if (!currentHeapLock) {
      callback();
    } else {
      currentHeapLock.enqueuePostReleaseAction(callback);
    }
  },
};

async function importDotnetJs(startOptions: Partial<WebAssemblyStartOptions>): Promise<ModuleAPI> {
  const browserSupportsNativeWebAssembly = typeof WebAssembly !== 'undefined' && WebAssembly.validate;
  if (!browserSupportsNativeWebAssembly) {
    throw new Error('This browser does not support WebAssembly.');
  }

  let src = '_framework/dotnet.js';

  // Allow overriding the URI from which the dotnet.*.js file is loaded
  if (startOptions.loadBootResource) {
    const resourceType: WebAssemblyBootResourceType = 'dotnetjs';
    const customSrc = startOptions.loadBootResource(resourceType, 'dotnet.js', src, '', 'js-module-dotnet');
    if (typeof (customSrc) === 'string') {
      src = customSrc;
    } else if (customSrc) {
      // Since we must load this via a import, it's only valid to supply a URI (and not a Request, say)
      throw new Error(`For a ${resourceType} resource, custom loaders must supply a URI string.`);
    }
  }

  const absoluteUrl = new URL(src, document.baseURI);
  if (!absoluteUrl.searchParams.has('v')) {
    absoluteUrl.searchParams.set('v', '8.0.19');
  }
  const absoluteSrc = absoluteUrl.toString();
  return await import(/* webpackIgnore: true */ absoluteSrc);
}

function prepareRuntimeConfig(options: Partial<WebAssemblyStartOptions>, onConfigLoadedCallback?: (loadedConfig: MonoConfig) => void): DotnetModuleConfig {
  const config: MonoConfig = {
    maxParallelDownloads: 1000000, // disable throttling parallel downloads
    enableDownloadRetry: false, // disable retry downloads
    applicationEnvironment: options.environment,
  };

  const onConfigLoaded = async (loadedConfig: MonoConfig) => {
    if (!loadedConfig.environmentVariables) {
      loadedConfig.environmentVariables = {};
    }

    if (loadedConfig.globalizationMode === GlobalizationMode.Sharded) {
      loadedConfig.environmentVariables['__BLAZOR_SHARDED_ICU'] = '1';
    }

    Blazor._internal.getApplicationEnvironment = () => loadedConfig.applicationEnvironment!;

    onConfigLoadedCallback?.(loadedConfig);

    jsInitializer = await fetchAndInvokeInitializers(options, loadedConfig);
  };

  const moduleConfig = (window['Module'] || {}) as typeof Module;
  const dotnetModuleConfig: DotnetModuleConfig = {
    ...moduleConfig,
    onConfigLoaded: (onConfigLoaded as (config: MonoConfig) => void | Promise<void>),
    onDownloadResourceProgress: setProgress,
    config,
    disableDotnet6Compatibility: false,
    out: print,
    err: printErr,
  };

  return dotnetModuleConfig;
}

async function createRuntimeInstance(options: Partial<WebAssemblyStartOptions>, onConfigLoaded?: (loadedConfig: MonoConfig) => void): Promise<void> {
  const { dotnet } = await importDotnetJs(options);
  const moduleConfig = prepareRuntimeConfig(options, onConfigLoaded);

  if (options.applicationCulture) {
    dotnet.withApplicationCulture(options.applicationCulture);
  }

  if (options.environment) {
    dotnet.withApplicationEnvironment(options.environment);
  }

  if (options.loadBootResource) {
    dotnet.withResourceLoader(options.loadBootResource);
  }

  const anyDotnet = (dotnet as any);
  anyDotnet.withModuleConfig(moduleConfig);

  if (options.configureRuntime) {
    options.configureRuntime(dotnet);
  }

  runtime = await dotnet.create();
}

async function configureRuntimeInstance(): Promise<PlatformApi> {
  if (!runtime) {
    throw new Error('The runtime must be loaded it gets configured.');
  }

  const { MONO: mono, BINDING: binding, Module: module, setModuleImports, INTERNAL: mono_internal, getConfig, invokeLibraryInitializers } = runtime;
  Module = module;
  BINDING = binding;
  MONO = mono;
  MONO_INTERNAL = mono_internal;

  attachDebuggerHotkey(getConfig());

  Blazor.runtime = runtime;
  Blazor._internal.dotNetCriticalError = printErr;
  setModuleImports('blazor-internal', {
    Blazor: { _internal: Blazor._internal },
  });
  const exports = await runtime.getAssemblyExports('Microsoft.AspNetCore.Components.WebAssembly');
  Object.assign(Blazor._internal, {
    dotNetExports: {
      ...exports.Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime,
    },
  });
  attachInteropInvoker();

  return {
    invokeLibraryInitializers,
  };
}

function setProgress(resourcesLoaded, totalResources) {
  const percentage = resourcesLoaded / totalResources * 100;
  document.documentElement.style.setProperty('--blazor-load-percentage', `${percentage}%`);
  document.documentElement.style.setProperty('--blazor-load-percentage-text', `"${Math.floor(percentage)}%"`);
}

const suppressMessages = ['DEBUGGING ENABLED'];
const print = line => (suppressMessages.indexOf(line) < 0 && console.log(line));
const printErr = line => {
  // If anything writes to stderr, treat it as a critical exception. The underlying runtime writes
  // to stderr if a truly critical problem occurs outside .NET code. Note that .NET unhandled
  // exceptions also reach this, but via a different code path - see dotNetCriticalError below.
  console.error(line || '(null)');
  showErrorNotification();
};

function getArrayDataPointer<T>(array: System_Array<T>): number {
  return <number><any>array + 12; // First byte from here is length, then following bytes are entries
}

function attachInteropInvoker(): void {
  dispatcher = DotNet.attachDispatcher({
    beginInvokeDotNetFromJS: (callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: any | null, argsJson: string): void => {
      assertHeapIsNotLocked();
      if (!dotNetObjectId && !assemblyName) {
        throw new Error('Either assemblyName or dotNetObjectId must have a non null value.');
      }
      // As a current limitation, we can only pass 4 args. Fortunately we only need one of
      // 'assemblyName' or 'dotNetObjectId', so overload them in a single slot
      const assemblyNameOrDotNetObjectId: string = dotNetObjectId
        ? dotNetObjectId.toString()
        : assemblyName;

      Blazor._internal.dotNetExports!.BeginInvokeDotNet!(
        callId ? callId.toString() : null,
        assemblyNameOrDotNetObjectId,
        methodIdentifier,
        argsJson,
      );
    },
    endInvokeJSFromDotNet: (asyncHandle, succeeded, serializedArgs): void => {
      Blazor._internal.dotNetExports!.EndInvokeJS(serializedArgs);
    },
    sendByteArray: (id: number, data: Uint8Array): void => {
      Blazor._internal.dotNetExports!.ReceiveByteArrayFromJS(id, data);
    },
    invokeDotNetFromJS: (assemblyName, methodIdentifier, dotNetObjectId, argsJson) => {
      assertHeapIsNotLocked();
      return Blazor._internal.dotNetExports!.InvokeDotNet(
        assemblyName ? assemblyName : null,
        methodIdentifier,
        dotNetObjectId ?? 0,
        argsJson,
      ) as string;
    },
  });
}

function assertHeapIsNotLocked() {
  if (currentHeapLock) {
    throw new Error('Assertion failed - heap is currently locked');
  }
}

class MonoHeapLock implements HeapLock {
  // eslint-disable-next-line @typescript-eslint/ban-types
  private postReleaseActions?: Function[];

  // eslint-disable-next-line @typescript-eslint/ban-types
  enqueuePostReleaseAction(callback: Function): void {
    if (!this.postReleaseActions) {
      this.postReleaseActions = [];
    }

    this.postReleaseActions.push(callback);
  }

  release() {
    if (currentHeapLock !== this) {
      throw new Error('Trying to release a lock which isn\'t current');
    }

    MONO_INTERNAL.mono_wasm_gc_unlock();

    currentHeapLock = null;

    while (this.postReleaseActions?.length) {
      const nextQueuedAction = this.postReleaseActions.shift()!;

      // It's possible that the action we invoke here might itself take a succession of heap locks,
      // but since heap locks must be released synchronously, by the time we get back to this stack
      // frame, we know the heap should no longer be locked.
      nextQueuedAction();
      assertHeapIsNotLocked();
    }
  }

  static create(): MonoHeapLock {
    MONO_INTERNAL.mono_wasm_gc_lock();
    return new MonoHeapLock();
  }
}
