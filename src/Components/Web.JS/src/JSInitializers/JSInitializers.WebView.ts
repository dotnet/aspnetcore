// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { JSAsset, JSInitializer } from './JSInitializers';

export async function fetchAndInvokeInitializers() : Promise<JSInitializer> {
  const jsInitializersResponse = await fetch('_framework/blazor.modules.json', {
    method: 'GET',
    credentials: 'include',
    cache: 'no-cache',
  });

  const initializers = (await jsInitializersResponse.json()).map(name => ({
    name,
  })) as JSAsset[];
  const jsInitializer = new JSInitializer();
  await jsInitializer.importInitializersAsync(initializers, []);
  return jsInitializer;
}
