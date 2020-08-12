import { DotNet } from '@microsoft/dotnet-js-interop';
import './GlobalExports';
import * as Environment from './Environment';
import { monoPlatform } from './Platform/Mono/MonoPlatform';
import { renderBatch } from './Rendering/Renderer';
import { SharedMemoryRenderBatch } from './Rendering/RenderBatch/SharedMemoryRenderBatch';
import { shouldAutoStart } from './BootCommon';
import { setEventDispatcher } from './Rendering/RendererEventDispatcher';
import { WebAssemblyResourceLoader } from './Platform/WebAssemblyResourceLoader';
import { WebAssemblyConfigLoader } from './Platform/WebAssemblyConfigLoader';
import { BootConfigResult } from './Platform/BootConfig';
import { Pointer } from './Platform/Platform';
import { WebAssemblyStartOptions } from './Platform/WebAssemblyStartOptions';

let started = false;

async function boot(options?: Partial<WebAssemblyStartOptions>): Promise<void> {

  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  setEventDispatcher((eventDescriptor, eventArgs) => {
    // It's extremely unusual, but an event can be raised while we're in the middle of synchronously applying a
    // renderbatch. For example, a renderbatch might mutate the DOM in such a way as to cause an <input> to lose
    // focus, in turn triggering a 'change' event. It may also be possible to listen to other DOM mutation events
    // that are themselves triggered by the application of a renderbatch.
    monoPlatform.invokeWhenHeapUnlocked(() => DotNet.invokeMethodAsync('Microsoft.AspNetCore.Components.WebAssembly', 'DispatchEvent', eventDescriptor, JSON.stringify(eventArgs)));
  });

  // Configure environment for execution under Mono WebAssembly with shared-memory rendering
  const platform = Environment.setPlatform(monoPlatform);
  window['Blazor'].platform = platform;
  window['Blazor']._internal.renderBatch = (browserRendererId: number, batchAddress: Pointer) => {
    // We're going to read directly from the .NET memory heap, so indicate to the platform
    // that we don't want anything to modify the memory contents during this time. Currently this
    // is only guaranteed by the fact that .NET code doesn't run during this time, but in the
    // future (when multithreading is implemented) we might need the .NET runtime to understand
    // that GC compaction isn't allowed during this critical section.
    const heapLock = monoPlatform.beginHeapLock();
    try {
      renderBatch(browserRendererId, new SharedMemoryRenderBatch(batchAddress));
    } finally {
      heapLock.release();
    }
  };

  // Configure navigation via JS Interop
  const getBaseUri = window['Blazor']._internal.navigationManager.getBaseURI;
  const getLocationHref = window['Blazor']._internal.navigationManager.getLocationHref;
  window['Blazor']._internal.navigationManager.getUnmarshalledBaseURI = () => BINDING.js_string_to_mono_string(getBaseUri());
  window['Blazor']._internal.navigationManager.getUnmarshalledLocationHref = () => BINDING.js_string_to_mono_string(getLocationHref());

  window['Blazor']._internal.navigationManager.listenForNavigationEvents(async (uri: string, intercepted: boolean): Promise<void> => {
    await DotNet.invokeMethodAsync(
      'Microsoft.AspNetCore.Components.WebAssembly',
      'NotifyLocationChanged',
      uri,
      intercepted
    );
  });

  // Fetch the resources and prepare the Mono runtime
  const bootConfigResult = await BootConfigResult.initAsync();

  const [resourceLoader] = await Promise.all([
    WebAssemblyResourceLoader.initAsync(bootConfigResult.bootConfig, options || {}),
    WebAssemblyConfigLoader.initAsync(bootConfigResult)]);

  try {
    await platform.start(resourceLoader);
  } catch (ex) {
    throw new Error(`Failed to start platform. Reason: ${ex}`);
  }

  // Start up the application
  platform.callEntryPoint(resourceLoader.bootConfig.entryAssembly);
}

window['Blazor'].start = boot;
if (shouldAutoStart()) {
  boot().catch(error => {
    if (typeof Module !== 'undefined' && Module.printErr) {
      // Logs it, and causes the error UI to appear
      Module.printErr(error);
    } else {
      // The error must have happened so early we didn't yet set up the error UI, so just log to console
      console.error(error);
    }
  });
}
