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
import { RuntimeAPI, CreateDotnetRuntimeType, DotnetModuleConfig, EmscriptenModule, AssetEntry } from 'dotnet';
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

    await createEmscriptenModuleInstance(resourceLoader);
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

  beginHeapLock: function() {
    assertHeapIsNotLocked();
    currentHeapLock = new MonoHeapLock();
    return currentHeapLock;
  },

  invokeWhenHeapUnlocked: function(callback) {
    // This is somewhat like a sync context. If we're not locked, just pass through the call directly.
    if (!currentHeapLock) {
      callback();
    } else {
      currentHeapLock.enqueuePostReleaseAction(callback);
    }
  },
};

async function importDotnetJs(resourceLoader: WebAssemblyResourceLoader): Promise<CreateDotnetRuntimeType> {
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
  const { default: createDotnetRuntime } = await import(/* webpackIgnore: true */ absoluteSrc);
  return await createDotnetRuntime;
}

async function createEmscriptenModuleInstance(resourceLoader: WebAssemblyResourceLoader): Promise<RuntimeAPI> {
  let runtimeReadyResolve: (data: RuntimeAPI) => void = undefined as any;
  let runtimeReadyReject: (reason?: any) => void = undefined as any;
  const runtimeReady = new Promise<RuntimeAPI>((resolve, reject) => {
    runtimeReadyResolve = resolve;
    runtimeReadyReject = reject;
  });

  const dotnetJsBeingLoaded = importDotnetJs(resourceLoader);
  const resources = resourceLoader.bootConfig.resources;
  const moduleConfig = (window['Module'] || {}) as typeof Module;
  const suppressMessages = ['DEBUGGING ENABLED'];

  const print = line => (suppressMessages.indexOf(line) < 0 && console.log(line));

  const printErr = line => {
    // If anything writes to stderr, treat it as a critical exception. The underlying runtime writes
    // to stderr if a truly critical problem occurs outside .NET code. Note that .NET unhandled
    // exceptions also reach this, but via a different code path - see dotNetCriticalError below.
    console.error(line || '(null)');
    showErrorNotification();
  };
  const existingPreRun = moduleConfig.preRun || [] as any;
  const existingPostRun = moduleConfig.postRun || [] as any;
  (moduleConfig as any).preloadPlugins = [];

  let resourcesLoaded = 0;
  function setProgress(){
    resourcesLoaded++;
    const percentage = resourcesLoaded / totalResources.length * 100;
    document.documentElement.style.setProperty('--blazor-load-percentage', `${percentage}%`);
    document.documentElement.style.setProperty('--blazor-load-percentage-text', `"${Math.floor(percentage)}%"`);
  }

  const monoToBlazorAssetTypeMap: { [key: string]: WebAssemblyBootResourceType | undefined } = {
    'assembly': 'assembly',
    'pdb': 'pdb',
    'icu': 'globalization',
    'dotnetwasm': 'dotnetwasm',
  };

  // it would not `loadResource` on types for which there is no typesMap mapping
  const downloadResource = (asset: AssetEntry): LoadingResource | undefined => {
    // this whole condition could be removed after the resourceLoader could cache in-flight requests
    if (asset.behavior === 'dotnetwasm') {
      return runtimeAssetsBeingLoaded
        .filter(request => request.name === asset.name)[0];
    }
    const type = monoToBlazorAssetTypeMap[asset.behavior];
    if (type !== undefined) {
      return resourceLoader.loadResource(asset.name, asset.resolvedUrl!, asset.hash!, type);
    }
    return undefined;
  };

  const runtimeAssets = resourceLoader.bootConfig.resources.runtimeAssets;
  // pass part of responsibility for asset loading to runtime
  const assets: AssetEntry[] = Object.keys(runtimeAssets).map(name => {
    const asset = runtimeAssets[name] as AssetEntry;
    asset.name = name;
    asset.resolvedUrl = `_framework/${name}`;
    return asset;
  });

  // blazor could start downloading bit earlier than the runtime would
  const runtimeAssetsBeingLoaded = assets
    .filter(asset => asset.behavior === 'dotnetwasm')
    .map(asset => {
      asset.pendingDownload = resourceLoader.loadResource(asset.name, asset.resolvedUrl!, asset.hash!, 'dotnetwasm');
      return asset.pendingDownload;
    });

  // Begin loading the .dll/.pdb/.wasm files, but don't block here. Let other loading processes run in parallel.
  const assembliesBeingLoaded = resourceLoader.loadResources(resources.assembly, filename => `_framework/${filename}`, 'assembly');
  const pdbsBeingLoaded = hasDebuggingEnabled()
    ? resourceLoader.loadResources(resources.pdb || {}, filename => `_framework/${filename}`, 'pdb')
    : [];
  const totalResources = assembliesBeingLoaded.concat(pdbsBeingLoaded, runtimeAssetsBeingLoaded);

  const dotnetTimeZoneResourceName = 'dotnet.timezones.blat';
  let timeZoneResource: LoadingResource | undefined;
  if (resourceLoader.bootConfig.resources.runtime.hasOwnProperty(dotnetTimeZoneResourceName)) {
    timeZoneResource = resourceLoader.loadResource(
      dotnetTimeZoneResourceName,
      `_framework/${dotnetTimeZoneResourceName}`,
      resourceLoader.bootConfig.resources.runtime[dotnetTimeZoneResourceName],
      'globalization'
    );
    totalResources.push(timeZoneResource);
    timeZoneResource.response.then(_ => setProgress());
  }

  let icuDataResource: LoadingResource | undefined;
  if (resourceLoader.bootConfig.icuDataMode !== ICUDataMode.Invariant) {
    const applicationCulture = resourceLoader.startOptions.applicationCulture || (navigator.languages && navigator.languages[0]);
    const icuDataResourceName = getICUResourceName(resourceLoader.bootConfig, applicationCulture);
    icuDataResource = resourceLoader.loadResource(
      icuDataResourceName,
      `_framework/${icuDataResourceName}`,
      resourceLoader.bootConfig.resources.runtime[icuDataResourceName],
      'globalization'
    );
    totalResources.push(icuDataResource);
    icuDataResource.response.then(_ => setProgress());
  }

  totalResources.forEach(loadingResource => loadingResource.response.then(_ => setProgress()));
  const createDotnetRuntime = await dotnetJsBeingLoaded;

  await createDotnetRuntime((api) => {
    const { MONO: mono, BINDING: binding, Module: module } = api;
    Module = module;
    BINDING = binding;
    MONO = mono;
    const onRuntimeInitialized = () => {
      if (!icuDataResource) {
        // Use invariant culture if the app does not carry icu data.
        MONO.mono_wasm_setenv('DOTNET_SYSTEM_GLOBALIZATION_INVARIANT', '1');
      }
    };

    const preRun = () => {
      if (timeZoneResource) {
        loadTimezone(timeZoneResource);
      }

      if (icuDataResource) {
        loadICUData(icuDataResource);
      }

      // Fetch the assemblies and PDBs in the background, telling Mono to wait until they are loaded
      // Mono requires the assembly filenames to have a '.dll' extension, so supply such names regardless
      // of the extensions in the URLs. This allows loading assemblies with arbitrary filenames.
      assembliesBeingLoaded.forEach(r => addResourceAsAssembly(r, changeExtension(r.name, '.dll')));
      pdbsBeingLoaded.forEach(r => addResourceAsAssembly(r, r.name));

      Blazor._internal.dotNetCriticalError = printErr;
      Blazor._internal.loadLazyAssembly = loadLazyAssembly;

      // Wire-up callbacks for satellite assemblies. Blazor will call these as part of the application
      // startup sequence to load satellite assemblies for the application's culture.
      Blazor._internal.loadSatelliteAssemblies = loadSatelliteAssemblies;
    };

    async function loadSatelliteAssemblies(culturesToLoad: string[], loader: (wrapper: { dll: Uint8Array }) => void): Promise<void> {
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

    async function loadLazyAssembly(assemblyNameToLoad: string): Promise<{ dll: Uint8Array, pdb: Uint8Array | null }> {
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

    const postRun = () => {
      if (resourceLoader.bootConfig.debugBuild && resourceLoader.bootConfig.cacheBootResources) {
        resourceLoader.logToConsole();
      }
      resourceLoader.purgeUnusedCacheEntriesAsync(); // Don't await - it's fine to run in background

      if (resourceLoader.bootConfig.icuDataMode === ICUDataMode.Sharded) {
        MONO.mono_wasm_setenv('__BLAZOR_SHARDED_ICU', '1');

        if (resourceLoader.startOptions.applicationCulture) {
          // If a culture is specified via start options use that to initialize the Emscripten \  .NET culture.
          MONO.mono_wasm_setenv('LANG', `${resourceLoader.startOptions.applicationCulture}.UTF-8`);
        }
      }
      let timeZone = 'UTC';
      try {
        timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        // eslint-disable-next-line no-empty
      } catch { }
      MONO.mono_wasm_setenv('TZ', timeZone || 'UTC');
      if (resourceLoader.bootConfig.modifiableAssemblies) {
        // Configure the app to enable hot reload in Development.
        MONO.mono_wasm_setenv('DOTNET_MODIFIABLE_ASSEMBLIES', resourceLoader.bootConfig.modifiableAssemblies);
      }

      if (resourceLoader.bootConfig.aspnetCoreBrowserTools) {
        // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
        MONO.mono_wasm_setenv('__ASPNETCORE_BROWSER_TOOLS', resourceLoader.bootConfig.aspnetCoreBrowserTools);
      }

      // -1 enables debugging with logging disabled. 0 disables debugging entirely.
      MONO.mono_wasm_load_runtime('unused', hasDebuggingEnabled() ? -1 : 0);
      MONO.mono_wasm_runtime_ready();
      try {
        BINDING.bind_static_method('invalid-fqn', '');
      } catch (e) {
        // HOTFIX: until https://github.com/dotnet/runtime/pull/72275
        // this would always throw, but it will initialize runtime interop as side-effect
      }

      // makes Blazor._internal visible to [JSImport] as "blazor-internal" module
      api.setModuleImports('blazor-internal', {
        Blazor: { _internal: Blazor._internal },
      });

      attachInteropInvoker();
      runtimeReadyResolve(api);
    };

    async function addResourceAsAssembly(dependency: LoadingResource, loadAsName: string) {
      const runDependencyId = `blazor:${dependency.name}`;
      Module.addRunDependency(runDependencyId);

      try {
        // Wait for the data to be loaded and verified
        const dataBuffer = await dependency.response.then(r => r.arrayBuffer());

        // Load it into the Mono runtime
        const data = new Uint8Array(dataBuffer);
        const heapAddress = Module._malloc(data.length);
        const heapMemory = new Uint8Array(Module.HEAPU8.buffer, heapAddress as any, data.length);
        heapMemory.set(data);
        MONO.mono_wasm_add_assembly(loadAsName, heapAddress, data.length);
        MONO.loaded_files.push(dependency.url);
      } catch (errorInfo) {
        runtimeReadyReject(errorInfo);
        return;
      }

      Module.removeRunDependency(runDependencyId);
    }

    const dotnetModuleConfig: DotnetModuleConfig = {
      ...moduleConfig,
      config: {
        assets,
        debugLevel: hasDebuggingEnabled() ? -1 : 0,
      },
      downloadResource,
      disableDotnet6Compatibility: false,
      preRun: [preRun, ...existingPreRun],
      postRun: [postRun, ...existingPostRun],
      print,
      printErr,
      onRuntimeInitialized,
    };

    return dotnetModuleConfig;
  });

  return await runtimeReady;
}

function getArrayDataPointer<T>(array: System_Array<T>): number {
  return <number><any>array + 12; // First byte from here is length, then following bytes are entries
}

function bindStaticMethod(assembly: string, typeName: string, method: string) {
  // Fully qualified name looks like this: "[debugger-test] Math:IntAdd"
  const fqn = `[${assembly}] ${typeName}:${method}`;
  return BINDING.bind_static_method(fqn);
}

export let byteArrayBeingTransferred: Uint8Array | null = null;
function attachInteropInvoker(): void {
  const dotNetDispatcherInvokeMethodHandle = bindStaticMethod('Microsoft.AspNetCore.Components.WebAssembly', 'Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime', 'InvokeDotNet');
  const dotNetDispatcherBeginInvokeMethodHandle = bindStaticMethod('Microsoft.AspNetCore.Components.WebAssembly', 'Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime', 'BeginInvokeDotNet');
  const dotNetDispatcherEndInvokeJSMethodHandle = bindStaticMethod('Microsoft.AspNetCore.Components.WebAssembly', 'Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime', 'EndInvokeJS');
  const dotNetDispatcherNotifyByteArrayAvailableMethodHandle = bindStaticMethod('Microsoft.AspNetCore.Components.WebAssembly', 'Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime', 'NotifyByteArrayAvailable');

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

      dotNetDispatcherBeginInvokeMethodHandle(
        callId ? callId.toString() : null,
        assemblyNameOrDotNetObjectId,
        methodIdentifier,
        argsJson,
      );
    },
    endInvokeJSFromDotNet: (asyncHandle, succeeded, serializedArgs): void => {
      dotNetDispatcherEndInvokeJSMethodHandle(serializedArgs);
    },
    sendByteArray: (id: number, data: Uint8Array): void => {
      byteArrayBeingTransferred = data;
      dotNetDispatcherNotifyByteArrayAvailableMethodHandle(id);
    },
    invokeDotNetFromJS: (assemblyName, methodIdentifier, dotNetObjectId, argsJson) => {
      assertHeapIsNotLocked();
      return dotNetDispatcherInvokeMethodHandle(
        assemblyName ? assemblyName : null,
        methodIdentifier,
        dotNetObjectId ? dotNetObjectId.toString() : null,
        argsJson,
      ) as string;
    },
  });
}

async function loadTimezone(timeZoneResource: LoadingResource): Promise<void> {
  const runDependencyId = 'blazor:timezonedata';
  Module.addRunDependency(runDependencyId);

  const request = await timeZoneResource.response;
  const arrayBuffer = await request.arrayBuffer();

  Module['FS_createPath']('/', 'usr', true, true);
  Module['FS_createPath']('/usr/', 'share', true, true);
  Module['FS_createPath']('/usr/share/', 'zoneinfo', true, true);
  MONO.mono_wasm_load_data_archive(new Uint8Array(arrayBuffer), '/usr/share/zoneinfo/');

  Module.removeRunDependency(runDependencyId);
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

async function loadICUData(icuDataResource: LoadingResource): Promise<void> {
  const runDependencyId = 'blazor:icudata';
  Module.addRunDependency(runDependencyId);

  const request = await icuDataResource.response;
  const array = new Uint8Array(await request.arrayBuffer());

  const offset = MONO.mono_wasm_load_bytes_into_heap(array);
  if (!MONO.mono_wasm_load_icu_data(offset)) {
    throw new Error('Error loading ICU asset.');
  }
  Module.removeRunDependency(runDependencyId);
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
