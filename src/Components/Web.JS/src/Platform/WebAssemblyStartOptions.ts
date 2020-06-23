export interface WebAssemblyStartOptions {
  /**
   * Overrides the built-in boot resource loading mechanism so that boot resources can be fetched
   * from a custom source, such as an external CDN.
   * @param type The type of the resource to be loaded.
   * @param name The name of the resource to be loaded.
   * @param defaultUri The URI from which the framework would fetch the resource by default. The URI may be relative or absolute.
   * @param integrity The integrity string representing the expected content in the response.
   * @returns A URI string or a Response promise to override the loading process, or null/undefined to allow the default loading behavior.
   */
  loadBootResource(type: WebAssemblyBootResourceType, name: string, defaultUri: string, integrity: string) : string | Promise<Response> | null | undefined;
}

// This type doesn't have to align with anything in BootConfig.
// Instead, this represents the public API through which certain aspects
// of boot resource loading can be customized.
export type WebAssemblyBootResourceType = 'assembly' | 'pdb' | 'dotnetjs' | 'dotnetwasm' | 'dynamicAssembly' | 'timezonedata';
