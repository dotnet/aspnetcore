import '../../Microsoft.JSInterop/JavaScriptRuntime/src/Microsoft.JSInterop';
import './GlobalExports';
import * as Environment from './Environment';
import { monoPlatform } from './Platform/Mono/MonoPlatform';
import { getAssemblyNameFromUrl } from './Platform/Url';
import { renderBatch } from './Rendering/Renderer';
import { RenderBatch } from './Rendering/RenderBatch/RenderBatch';
import { SharedMemoryRenderBatch } from './Rendering/RenderBatch/SharedMemoryRenderBatch';
import { Pointer } from './Platform/Platform';

async function boot() {
  // Configure environment for execution under Mono WebAssembly with shared-memory rendering
  const platform = Environment.setPlatform(monoPlatform);
  window['Blazor']._internal.renderBatch = (browserRendererId: number, batchAddress: Pointer) => {
    renderBatch(browserRendererId, new SharedMemoryRenderBatch(batchAddress));
  };

  // Fetch the boot JSON file
  // Later we might make the location of this configurable (e.g., as an attribute on the <script>
  // element that's importing this file), but currently there isn't a use case for that.
  const bootConfigResponse = await fetch('_framework/blazor.boot.json');
  const bootConfig: BootJsonData = await bootConfigResponse.json();
  const embeddedResourcePromises = startLoadingEmbeddedResources(bootConfig);

  if (!bootConfig.linkerEnabled) {
    console.info('Blazor is running in dev mode without IL stripping. To make the bundle size significantly smaller, publish the application or see https://go.microsoft.com/fwlink/?linkid=870414');
  }

  // Determine the URLs of the assemblies we want to load, then begin fetching them all
  const loadAssemblyUrls = [bootConfig.main]
    .concat(bootConfig.assemblyReferences)
    .map(filename => `_framework/_bin/${filename}`);

  try {
    await platform.start(loadAssemblyUrls);
  } catch (ex) {
    throw new Error(`Failed to start platform. Reason: ${ex}`);
  }

  // Before we start running .NET code, be sure embedded content resources are all loaded
  await Promise.all(embeddedResourcePromises)

  // Start up the application
  const mainAssemblyName = getAssemblyNameFromUrl(bootConfig.main);
  platform.callEntryPoint(mainAssemblyName, bootConfig.entryPoint, []);
}

function startLoadingEmbeddedResources(bootConfig: BootJsonData) {
  const cssLoadingPromises = bootConfig.cssReferences.map(cssReference => {
    const linkElement = document.createElement('link');
    linkElement.rel = 'stylesheet';
    linkElement.href = cssReference;
    return loadResourceFromElement(linkElement);
  });
  const jsLoadingPromises = bootConfig.jsReferences.map(jsReference => {
    const scriptElement = document.createElement('script');
    scriptElement.src = jsReference;
    return loadResourceFromElement(scriptElement);
  });
  return cssLoadingPromises.concat(jsLoadingPromises);
}

function loadResourceFromElement(element: HTMLElement) {
  return new Promise((resolve, reject) => {
    element.onload = resolve;
    element.onerror = reject;
    document.head.appendChild(element);
  });
}

// Keep in sync with BootJsonData in Microsoft.AspNetCore.Blazor.Build
interface BootJsonData {
  main: string;
  entryPoint: string;
  assemblyReferences: string[];
  cssReferences: string[];
  jsReferences: string[];
  linkerEnabled: boolean;
}

boot();
