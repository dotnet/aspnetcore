import '@dotnet/jsinterop';
import './GlobalExports';
import * as Environment from './Environment';
import { monoPlatform } from './Platform/Mono/MonoPlatform';
import { renderBatch } from './Rendering/Renderer';
import { SharedMemoryRenderBatch } from './Rendering/RenderBatch/SharedMemoryRenderBatch';
import { Pointer, Platform, System_String } from './Platform/Platform';
import { shouldAutoStart } from './BootCommon';
import { setEventDispatcher } from './Rendering/RendererEventDispatcher';
import { WebAssemblyResourceLoader } from './Platform/WebAssemblyResourceLoader';

let started = false;

async function boot(options?: any): Promise<void> {

  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  setEventDispatcher((eventDescriptor, eventArgs) => DotNet.invokeMethodAsync('Microsoft.AspNetCore.Components.WebAssembly', 'DispatchEvent', eventDescriptor, JSON.stringify(eventArgs)));

  // Configure environment for execution under Mono WebAssembly with shared-memory rendering
  const platform = Environment.setPlatform(monoPlatform);
  window['Blazor'].platform = platform;
  window['Blazor']._internal.renderBatch = (browserRendererId: number, batchAddress: Pointer) => {
    renderBatch(browserRendererId, new SharedMemoryRenderBatch(batchAddress));
  };

  // Configure navigation via JS Interop
  window['Blazor']._internal.navigationManager.listenForNavigationEvents(async (uri: string, intercepted: boolean): Promise<void> => {
    await DotNet.invokeMethodAsync(
      'Microsoft.AspNetCore.Components.WebAssembly',
      'NotifyLocationChanged',
      uri,
      intercepted
    );
  });

  // Fetch the resources and prepare the Mono runtime
  const resourceLoader = await WebAssemblyResourceLoader.initAsync();
  await initializeConfigAsync(platform, resourceLoader);

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

async function initializeConfigAsync(platform: Platform, resourceLoader: WebAssemblyResourceLoader) : Promise<void> {
  const configFiles = resourceLoader.readConfigFilesAsync();
  const resolvedFiles = await Promise.all(configFiles.map(async c => {
    const content = new Uint8Array(await c.contentPromise);
    return { c.name, content };
  }));


  window['Blazor']._internal.getApplicationEnvironment = () => platform.toDotNetString(resourceLoader.applicationEnvironment);
  window['Blazor']._internal.getConfig = (dotNetFileName: System_String) : Pointer | undefined => {
    const fileName = platform.toJavaScriptString(string);
    const resolvedFile = resolvedFiles.find(f => f.name === fileName);
    return resolvedFile ? platform.toDotNetArray(resolvedFile.content) : undefined;
  };
}
