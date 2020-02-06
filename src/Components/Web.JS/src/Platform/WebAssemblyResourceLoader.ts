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

type ResourceList = { [name: string]: string };
