export async function fetchBootConfigAsync() {
  // Later we might make the location of this configurable (e.g., as an attribute on the <script>
  // element that's importing this file), but currently there isn't a use case for that.
  const bootConfigResponse = await fetch('_framework/blazor.boot.json', { method: 'Get', credentials: 'include' });
  return bootConfigResponse.json() as Promise<BootJsonData>;
}

export function loadEmbeddedResourcesAsync(bootConfig: BootJsonData): Promise<any> {
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
  return Promise.all(cssLoadingPromises.concat(jsLoadingPromises));
}

function loadResourceFromElement(element: HTMLElement) {
  return new Promise((resolve, reject) => {
    element.onload = resolve;
    element.onerror = reject;
    document.head!.appendChild(element);
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

// Tells you if the script was added without <script src="..." autostart="false"></script>
export function shouldAutoStart() {
  return !!(document &&
    document.currentScript &&
    document.currentScript.getAttribute('autostart') !== 'false');
}
