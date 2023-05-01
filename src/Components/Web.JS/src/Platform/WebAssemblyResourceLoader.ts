// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { BootJsonData } from 'dotnet';
import { WebAssemblyBootResourceType } from './WebAssemblyStartOptions';

export interface WebAssemblyResourceLoader {
  readonly bootConfig: BootJsonData;
  loadResources(resources: ResourceList, url: (name: string) => string, resourceType: WebAssemblyBootResourceType): LoadingResource[];
  loadResource(name: string, url: string, contentHash: string, resourceType: WebAssemblyBootResourceType): LoadingResource;
}

export type ResourceList = { [name: string]: string };

export interface LoadingResource {
  name: string;
  url: string;
  response: Promise<Response>;
}
