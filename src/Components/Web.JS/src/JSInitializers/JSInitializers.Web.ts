// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Logger } from '../Platform/Logging/Logger';
import { WebStartOptions } from '../Platform/WebStartOptions';
import { discoverWebInitializers } from '../Services/ComponentDescriptorDiscovery';
import { JSInitializer } from './JSInitializers';

export async function fetchAndInvokeInitializers(options: Partial<WebStartOptions>, logger: Logger) : Promise<JSInitializer> {
  const initializersElement = discoverWebInitializers(document);
  if (!initializersElement) {
    return new JSInitializer(false, logger);
  }
  const initializers: string[] = JSON.parse(atob(initializersElement)) as string[] ?? [];
  const jsInitializer = new JSInitializer(false, logger);
  await jsInitializer.importInitializersAsync(initializers, [options]);
  return jsInitializer;
}
