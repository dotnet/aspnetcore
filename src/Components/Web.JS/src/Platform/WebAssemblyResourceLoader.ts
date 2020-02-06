export class WebAssemblyResourceLoader {

  static async initAsync(): Promise<WebAssemblyResourceLoader> {
    const bootConfigResponse = await fetch('_framework/blazor.boot.json', {
      method: 'GET',
      credentials: 'include'
    });

    return new WebAssemblyResourceLoader(await bootConfigResponse.json());
  }

  constructor (public readonly bootConfig: BootJsonData)
  {
  }

  loadResources(resources: ResourceList, url: (name: string) => string): LoadingResource[] {
    return Object.keys(resources)
      .map(name => loadResource(name, url(name), resources[name]));
  }
}

function loadResource(name: string, url: string, contentHash: string): LoadingResource {
  const data = (async () => {
    const response = await fetch(url, { cache: 'no-cache' });
    return await response.arrayBuffer();
  })();

  return { name, url, data };
}

// Keep in sync with bootJsonData in Microsoft.AspNetCore.Blazor.Build
interface BootJsonData {
  readonly entryAssembly: string;
  readonly resources: ResourceGroups;
  readonly linkerEnabled: boolean;
}

interface ResourceGroups {
  readonly assembly: ResourceList;
  readonly pdb?: ResourceList;
}

export interface LoadingResource {
  name: string;
  url: string;
  data: Promise<ArrayBuffer>;
}

type ResourceList = { [name: string]: string };
