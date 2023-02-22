// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/no-explicit-any */
/* eslint-disable @typescript-eslint/no-non-null-assertion */
/* eslint-disable no-prototype-builtins */
import { DotNet } from '@microsoft/dotnet-js-interop';
import { attachDebuggerHotkey, hasDebuggingEnabled } from './MonoDebugger';
import { showErrorNotification } from '../../BootErrors';
import { WebAssemblyResourceLoader, LoadingResource } from '../WebAssemblyResourceLoader';
import { Platform, System_Array, Pointer, System_Object, System_String, HeapLock } from '../Platform';
import { WebAssemblyBootResourceType } from '../WebAssemblyStartOptions';
import { BootJsonData, ICUDataMode } from '../BootConfig';
import { Blazor } from '../../GlobalExports';
import { DotnetModuleConfig, EmscriptenModule, AssetEntry, MonoConfig, ModuleAPI } from 'dotnet';
import { BINDINGType, MONOType } from 'dotnet/dotnet-legacy';

// initially undefined and only fully initialized after createEmscriptenModuleInstance()
export let BINDING: BINDINGType = undefined as any;
export let MONO: MONOType = undefined as any;
export let Module: DotnetModuleConfig & EmscriptenModule = undefined as any;

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
  start: async function start(resourceLoader: WebAssemblyResourceLoader) {
    attachDebuggerHotkey(resourceLoader);

    await createRuntimeInstance(resourceLoader);
  },

  callEntryPoint: async function callEntryPoint(assemblyName: string): Promise<any> {
    const emptyArray = [[]];

    try {
      await BINDING.call_assembly_entry_point(assemblyName, emptyArray, 'm');
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

    let decodedString: string | null | undefined;
    if (currentHeapLock) {
      decodedString = currentHeapLock.stringCache.get(fieldValue);
      if (decodedString === undefined) {
        decodedString = BINDING.conv_string(fieldValue as any as System_String);
        currentHeapLock.stringCache.set(fieldValue, decodedString);
      }
    } else {
      decodedString = BINDING.conv_string(fieldValue as any as System_String);
    }

    return decodedString;
  },

  readStructField: function readStructField<T extends Pointer>(baseAddress: Pointer, fieldOffset?: number): T {
    return ((baseAddress as any as number) + (fieldOffset || 0)) as any as T;
  },

  beginHeapLock: function beginHeapLock() {
    assertHeapIsNotLocked();
    currentHeapLock = new MonoHeapLock();
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

function importDotnetJs(resourceLoader: WebAssemblyResourceLoader): Promise<ModuleAPI> {
  const browserSupportsNativeWebAssembly = typeof WebAssembly !== 'undefined' && WebAssembly.validate;
  if (!browserSupportsNativeWebAssembly) {
    throw new Error('This browser does not support WebAssembly.');
  }

  // The dotnet.*.js file has a version or hash in its name as a form of cache-busting. This is needed
  // because it's the only part of the loading process that can't use cache:'no-cache' (because it's
  // not a 'fetch') and isn't controllable by the developer (so they can't put in their own cache-busting
  // querystring). So, to find out the exact URL we have to search the boot manifest.
  const dotnetJsResourceName = Object
    .keys(resourceLoader.bootConfig.resources.runtime)
    .filter(n => n.startsWith('dotnet.') && n.endsWith('.js'))[0];
  const dotnetJsContentHash = resourceLoader.bootConfig.resources.runtime[dotnetJsResourceName];
  let src = `_framework/${dotnetJsResourceName}`;

  // Allow overriding the URI from which the dotnet.*.js file is loaded
  if (resourceLoader.startOptions.loadBootResource) {
    const resourceType: WebAssemblyBootResourceType = 'dotnetjs';
    const customSrc = resourceLoader.startOptions.loadBootResource(resourceType, dotnetJsResourceName, src, dotnetJsContentHash);
    if (typeof (customSrc) === 'string') {
      src = customSrc;
    } else if (customSrc) {
      // Since we must load this via a import, it's only valid to supply a URI (and not a Request, say)
      throw new Error(`For a ${resourceType} resource, custom loaders must supply a URI string.`);
    }
  }

  // For consistency with WebAssemblyResourceLoader, we only enforce SRI if caching is allowed
  if (resourceLoader.bootConfig.cacheBootResources) {
    const scriptElem = document.createElement('link');
    scriptElem.rel = 'modulepreload';
    scriptElem.href = src;
    scriptElem.crossOrigin = 'anonymous';
    // it will make dynamic import fail if the hash doesn't match
    // It's currently only validated by chromium browsers
    // Firefox doesn't break on it, but doesn't validate it either
    scriptElem.integrity = dotnetJsContentHash;
    document.head.appendChild(scriptElem);
  }

  const absoluteSrc = (new URL(src, document.baseURI)).toString();
  return import(/* webpackIgnore: true */ absoluteSrc);
}

function prepareRuntimeConfig(resourceLoader: WebAssemblyResourceLoader): DotnetModuleConfig {
  const resources = resourceLoader.bootConfig.resources;

  const assets: AssetEntry[] = [];
  const environmentVariables = {};
  const config: MonoConfig = {
    assets,
    globalizationMode: 'icu',
    environmentVariables: environmentVariables,
    debugLevel: hasDebuggingEnabled() ? 1 : 0,
    maxParallelDownloads: 1000000, // disable throttling parallel downloads
    enableDownloadRetry: false, // disable retry downloads
  };
  const monoToBlazorAssetTypeMap: { [key: string]: WebAssemblyBootResourceType | undefined } = {
    'assembly': 'assembly',
    'pdb': 'pdb',
    'icu': 'globalization',
    'vfs': 'globalization',
    'dotnetwasm': 'dotnetwasm',
  };
  const behaviorByName = (name) => {
    return name === 'dotnet.timezones.blat' ? 'vfs'
      : (name.startsWith('dotnet.worker') && name.endsWith('.js')) ? 'js-module-threads'
        : (name.startsWith('dotnet') && name.endsWith('.js')) ? 'js-module-dotnet'
          : name.startsWith('icudt') ? 'icu'
            : 'other';
  };

  // it would not `loadResource` on types for which there is no typesMap mapping
  const downloadResource = (asset: AssetEntry): LoadingResource | undefined => {
    // GOTCHA: the mapping to blazor asset type may not cover all mono owned asset types in the future in which case:
    // A) we may need to add such asset types to the mapping and to WebAssemblyBootResourceType
    // B) or we could add generic "runtime" type to WebAssemblyBootResourceType as fallback
    // C) or we could return `undefined` and let the runtime to load the asset. In which case the progress will not be reported on it and blazor will not be able to cache it.
    const type = monoToBlazorAssetTypeMap[asset.behavior];
    if (type !== undefined) {
      const res = resourceLoader.loadResource(asset.name, asset.resolvedUrl!, asset.hash!, type);
      asset.pendingDownload = res;
      totalResources++;
      res.response.then(setProgress);
      return res;
    }
    return undefined;
  };

  // any runtime owned assets, with proper behavior already set
  for (const name in resources.runtimeAssets) {
    const asset = resources.runtimeAssets[name] as AssetEntry;
    asset.name = name;
    asset.resolvedUrl = `_framework/${name}`;
    assets.push(asset);
    if (asset.behavior === 'dotnetwasm') {
      // start the download as soon as possible
      downloadResource(asset);
    }
  }
  for (const name in resources.assembly) {
    const asset: AssetEntry = {
      name,
      resolvedUrl: `_framework/${name}`,
      hash: resources.assembly[name],
      behavior: 'assembly',
    };
    assets.push(asset);
    // start the download as soon as possible
    downloadResource(asset);
  }
  if (hasDebuggingEnabled() && resources.pdb) {
    for (const name in resources.pdb) {
      const asset: AssetEntry = {
        name,
        resolvedUrl: `_framework/${name}`,
        hash: resources.pdb[name],
        behavior: 'pdb',
      };
      assets.push(asset);
      downloadResource(asset);
    }
  }
  const applicationCulture = resourceLoader.startOptions.applicationCulture || (navigator.languages && navigator.languages[0]);
  const icuDataResourceName = getICUResourceName(resourceLoader.bootConfig, applicationCulture);
  let hasIcuData = false;
  for (const name in resources.runtime) {
    const behavior = behaviorByName(name) as any;
    if (behavior === 'icu') {
      if (resourceLoader.bootConfig.icuDataMode === ICUDataMode.Invariant) {
        continue;
      }
      if (name !== icuDataResourceName) {
        continue;
      }
      hasIcuData = true;
    } else if (behavior === 'js-module-dotnet') {
      continue;
    }
    if (resources.runtimeAssets.hasOwnProperty(name)) {
      continue;
    }
    const asset: AssetEntry = {
      name,
      resolvedUrl: `_framework/${name}`,
      hash: resources.runtime[name],
      behavior,
    };
    assets.push(asset);
    downloadResource(asset);
  }

  if (!hasIcuData) {
    config.globalizationMode = 'invariant';
  }

  if (resourceLoader.bootConfig.modifiableAssemblies) {
    // Configure the app to enable hot reload in Development.
    environmentVariables['DOTNET_MODIFIABLE_ASSEMBLIES'] = resourceLoader.bootConfig.modifiableAssemblies;
  }

  if (resourceLoader.bootConfig.icuDataMode === ICUDataMode.Sharded) {
    environmentVariables['__BLAZOR_SHARDED_ICU'] = '1';
  }

  if (resourceLoader.startOptions.applicationCulture) {
    // If a culture is specified via start options use that to initialize the Emscripten \  .NET culture.
    environmentVariables['LANG'] = `${resourceLoader.startOptions.applicationCulture}.UTF-8`;
  }

  if (resourceLoader.bootConfig.aspnetCoreBrowserTools) {
    // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
    environmentVariables['__ASPNETCORE_BROWSER_TOOLS'] = resourceLoader.bootConfig.aspnetCoreBrowserTools;
  }


  const moduleConfig = (window['Module'] || {}) as typeof Module;
  // TODO (moduleConfig as any).preloadPlugins = []; // why do we need this ?
  const dotnetModuleConfig: DotnetModuleConfig = {
    ...moduleConfig,
    configSrc: undefined,
    config,
    downloadResource,
    disableDotnet6Compatibility: false,
    print,
    printErr,
  };

  return dotnetModuleConfig;
}

async function createRuntimeInstance(resourceLoader: WebAssemblyResourceLoader): Promise<void> {
  const { dotnet } = await importDotnetJs(resourceLoader);
  const moduleConfig = prepareRuntimeConfig(resourceLoader);
  (dotnet as any).withModuleConfig(moduleConfig);

  const runtime = await dotnet.create();
  const { MONO: mono, BINDING: binding, Module: module, setModuleImports } = runtime;
  Module = module;
  BINDING = binding;
  MONO = mono;

  Blazor._internal.dotNetCriticalError = printErr;
  Blazor._internal.loadLazyAssembly = (assemblyNameToLoad) => loadLazyAssembly(resourceLoader, assemblyNameToLoad);
  Blazor._internal.loadSatelliteAssemblies = (culturesToLoad, loader) => loadSatelliteAssemblies(resourceLoader, culturesToLoad, loader);
  setModuleImports('blazor-internal', {
    Blazor: { _internal: Blazor._internal },
  });
  const exports = await runtime.getAssemblyExports('Microsoft.AspNetCore.Components.WebAssembly');
  Object.assign(Blazor._internal, {
    ...exports.Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime,
  });
  attachInteropInvoker();
  if (resourceLoader.bootConfig.debugBuild && resourceLoader.bootConfig.cacheBootResources) {
    resourceLoader.logToConsole();
  }
  resourceLoader.purgeUnusedCacheEntriesAsync(); // Don't await - it's fine to run in background
}

let resourcesLoaded = 0;
let totalResources = 0;
function setProgress() {
  resourcesLoaded++;
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

async function loadSatelliteAssemblies(resourceLoader: WebAssemblyResourceLoader, culturesToLoad: string[], loader: (wrapper: { dll: Uint8Array }) => void): Promise<void> {
  const satelliteResources = resourceLoader.bootConfig.resources.satelliteResources;
  if (!satelliteResources) {
    return;
  }
  await Promise.all(culturesToLoad!
    .filter(culture => satelliteResources.hasOwnProperty(culture))
    .map(culture => resourceLoader.loadResources(satelliteResources[culture], fileName => `_framework/${fileName}`, 'assembly'))
    .reduce((previous, next) => previous.concat(next), new Array<LoadingResource>())
    .map(async resource => {
      const response = await resource.response;
      const bytes = await response.arrayBuffer();
      const wrapper = { dll: new Uint8Array(bytes) };
      loader(wrapper);
    }));
}


async function loadLazyAssembly(resourceLoader: WebAssemblyResourceLoader, assemblyNameToLoad: string): Promise<{ dll: Uint8Array, pdb: Uint8Array | null }> {
  const resources = resourceLoader.bootConfig.resources;
  const lazyAssemblies = resources.lazyAssembly;
  if (!lazyAssemblies) {
    throw new Error("No assemblies have been marked as lazy-loadable. Use the 'BlazorWebAssemblyLazyLoad' item group in your project file to enable lazy loading an assembly.");
  }

  const assemblyMarkedAsLazy = lazyAssemblies.hasOwnProperty(assemblyNameToLoad);
  if (!assemblyMarkedAsLazy) {
    throw new Error(`${assemblyNameToLoad} must be marked with 'BlazorWebAssemblyLazyLoad' item group in your project file to allow lazy-loading.`);
  }
  const dllNameToLoad = changeExtension(assemblyNameToLoad, '.dll');
  const pdbNameToLoad = changeExtension(assemblyNameToLoad, '.pdb');
  const shouldLoadPdb = hasDebuggingEnabled() && resources.pdb && lazyAssemblies.hasOwnProperty(pdbNameToLoad);

  const dllBytesPromise = resourceLoader.loadResource(dllNameToLoad, `_framework/${dllNameToLoad}`, lazyAssemblies[dllNameToLoad], 'assembly').response.then(response => response.arrayBuffer());
  if (shouldLoadPdb) {
    const pdbBytesPromise = await resourceLoader.loadResource(pdbNameToLoad, `_framework/${pdbNameToLoad}`, lazyAssemblies[pdbNameToLoad], 'pdb').response.then(response => response.arrayBuffer());
    const [dllBytes, pdbBytes] = await Promise.all([dllBytesPromise, pdbBytesPromise]);
    return {
      dll: new Uint8Array(dllBytes),
      pdb: new Uint8Array(pdbBytes),
    };
  } else {
    const dllBytes = await dllBytesPromise;
    return {
      dll: new Uint8Array(dllBytes),
      pdb: null,
    };
  }
}

function getArrayDataPointer<T>(array: System_Array<T>): number {
  return <number><any>array + 12; // First byte from here is length, then following bytes are entries
}

function attachInteropInvoker(): void {
  DotNet.attachDispatcher({
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

      Blazor._internal.BeginInvokeDotNet!(
        callId ? callId.toString() : null,
        assemblyNameOrDotNetObjectId,
        methodIdentifier,
        argsJson,
      );
    },
    endInvokeJSFromDotNet: (asyncHandle, succeeded, serializedArgs): void => {
      Blazor._internal.EndInvokeJS!(serializedArgs);
    },
    sendByteArray: (id: number, data: Uint8Array): void => {
      Blazor._internal.ReceiveByteArrayFromJS!(id, data);
    },
    invokeDotNetFromJS: (assemblyName, methodIdentifier, dotNetObjectId, argsJson) => {
      assertHeapIsNotLocked();
      return Blazor._internal.InvokeDotNet!(
        assemblyName ? assemblyName : null,
        methodIdentifier,
        dotNetObjectId ?? 0,
        argsJson,
      ) as string;
    },
  });
}

function getICUResourceName(bootConfig: BootJsonData, culture: string | undefined): string {
  const combinedICUResourceName = 'icudt.dat';
  if (!culture || bootConfig.icuDataMode === ICUDataMode.All) {
    return combinedICUResourceName;
  }

  const prefix = culture.split('-')[0];
  if ([
    'en',
    'fr',
    'it',
    'de',
    'es',
  ].includes(prefix)) {
    return 'icudt_EFIGS.dat';
  } else if ([
    'zh',
    'ko',
    'ja',
  ].includes(prefix)) {
    return 'icudt_CJK.dat';
  } else {
    return 'icudt_no_CJK.dat';
  }
}

function changeExtension(filename: string, newExtensionWithLeadingDot: string) {
  const lastDotIndex = filename.lastIndexOf('.');
  if (lastDotIndex < 0) {
    throw new Error(`No extension to replace in '${filename}'`);
  }

  return filename.substr(0, lastDotIndex) + newExtensionWithLeadingDot;
}

function assertHeapIsNotLocked() {
  if (currentHeapLock) {
    throw new Error('Assertion failed - heap is currently locked');
  }
}

class MonoHeapLock implements HeapLock {
  // Within a given heap lock, it's safe to cache decoded strings since the memory can't change
  stringCache = new Map<number, string | null>();

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
}
