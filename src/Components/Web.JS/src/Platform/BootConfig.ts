import { WebAssemblyBootResourceType } from "./WebAssemblyStartOptions";

export class BootConfigResult {
  private constructor(public bootConfig: BootJsonData, public applicationEnvironment: string) {
  }

  static async initAsync(loadBootResource?: (type: WebAssemblyBootResourceType, name: string, defaultUri: string, integrity: string) => string | Promise<Response> | null | undefined, environment?: string): Promise<BootConfigResult> {
    let loaderResponse = loadBootResource !== undefined ?
      loadBootResource('manifest', 'blazor.boot.json', '_framework/blazor.boot.json', '') :
      this.defaultLoadBlazorBootJson('_framework/blazor.boot.json');

    const bootConfigResponse = loaderResponse instanceof Promise ?
      await loaderResponse :
      await BootConfigResult.defaultLoadBlazorBootJson(loaderResponse ?? '_framework/blazor.boot.json');

    // While we can expect an ASP.NET Core hosted application to include the environment, other
    // hosts may not. Assume 'Production' in the absence of any specified value.
    const applicationEnvironment = environment || bootConfigResponse.headers.get('Blazor-Environment') || 'Production';
    const bootConfig: BootJsonData = await bootConfigResponse.json();
    bootConfig.modifiableAssemblies = bootConfigResponse.headers.get('DOTNET-MODIFIABLE-ASSEMBLIES');

    return new BootConfigResult(bootConfig, applicationEnvironment);
  };

  public static async defaultLoadBlazorBootJson(url: string) : Promise<Response> {
    return fetch(url, {
      method: 'GET',
      credentials: 'include',
      cache: 'no-cache'
    });
  }
}


// Keep in sync with bootJsonData from the BlazorWebAssemblySDK
export interface BootJsonData {
  readonly entryAssembly: string;
  readonly resources: ResourceGroups;
  /** Gets a value that determines if this boot config was produced from a non-published build (i.e. dotnet build or dotnet run) */
  readonly debugBuild: boolean;
  readonly linkerEnabled: boolean;
  readonly cacheBootResources: boolean;
  readonly config: string[];
  readonly icuDataMode: ICUDataMode;
  readonly libraryInitializers?: ResourceList,
  readonly extensions?: BootJsonDataExtension

  // These properties are tacked on, and not found in the boot.json file
  modifiableAssemblies: string | null;
}

export type BootJsonDataExtension = { [extensionName: string]: ResourceList };

export interface ResourceGroups {
  readonly assembly: ResourceList;
  readonly lazyAssembly: ResourceList;
  readonly pdb?: ResourceList;
  readonly runtime: ResourceList;
  readonly satelliteResources?: { [cultureName: string]: ResourceList };
}

export type ResourceList = { [name: string]: string };

export enum ICUDataMode {
  Sharded,
  All,
  Invariant
}
