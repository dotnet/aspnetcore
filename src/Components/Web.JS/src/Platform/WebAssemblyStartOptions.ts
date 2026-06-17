// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotnetHostBuilder, AssetBehaviors } from '@microsoft/dotnet-runtime';
import { IBlazor } from '../GlobalExports';

export interface WebAssemblyStartOptions {
  /**
   * Overrides the built-in boot resource loading mechanism so that boot resources can be fetched
   * from a custom source, such as an external CDN.
   * @param type The type of the resource to be loaded.
   * @param name The name of the resource to be loaded.
   * @param defaultUri The URI from which the framework would fetch the resource by default. The URI may be relative or absolute.
   * @param integrity The integrity string representing the expected content in the response.
   * @param behavior The detailed behavior/type of the resource to be loaded.
   * @returns A URI string or a Response promise to override the loading process, or null/undefined to allow the default loading behavior.
   * When returned string is not qualified with `./` or absolute URL, it will be resolved against document.baseURI.
   */
  loadBootResource(type: WebAssemblyBootResourceType, name: string, defaultUri: string, integrity: string, behavior: AssetBehaviors): string | Promise<Response> | null | undefined;

  /**
   * Override built-in environment setting on start.
   */
  environment?: string;

  /**
   * Gets the application culture. This is a name specified in the BCP 47 format. See https://tools.ietf.org/html/bcp47
   */
  applicationCulture?: string;

  initializers?: WebAssemblyInitializers;

  /**
   * Allows to override .NET runtime configuration.
   */
  configureRuntime(builder: DotnetHostBuilder): void;
}

// This type doesn't have to align with anything in BootConfig.
// Instead, this represents the public API through which certain aspects
// of boot resource loading can be customized.
export type WebAssemblyBootResourceType = 'assembly' | 'pdb' | 'dotnetjs' | 'dotnetwasm' | 'globalization' | 'manifest' | 'configuration';

export type BeforeBlazorWebAssemblyStartedCallback = (options: Partial<WebAssemblyStartOptions>) => Promise<void>;
export type AfterBlazorWebAssemblyStartedCallback = (blazor: IBlazor) => Promise<void>;

export type WebAssemblyInitializers = {
  beforeStart: BeforeBlazorWebAssemblyStartedCallback [],
  afterStarted: AfterBlazorWebAssemblyStartedCallback [],
}
