// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable array-element-newline */
import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import { WebAssemblyStartOptions } from './Platform/WebAssemblyStartOptions';
import { setWebAssemblyOptions, startWebAssembly } from './Boot.WebAssembly.Common';
import { WebAssemblyComponentDescriptor, discoverComponents } from './Services/ComponentDescriptorDiscovery';
import { DotNet } from '@microsoft/dotnet-js-interop';
import { InitialRootComponentsList } from './Services/InitialRootComponentsList';
import { JSEventRegistry } from './Services/JSEventRegistry';
import { printErr } from './Platform/Mono/MonoPlatform';

let started = false;

async function boot(options?: Partial<WebAssemblyStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  setWebAssemblyOptions(Promise.resolve(options || {}));

  JSEventRegistry.create(Blazor);
  const webAssemblyComponents = discoverComponents(document, 'webassembly') as WebAssemblyComponentDescriptor[];

  const components = new InitialRootComponentsList(webAssemblyComponents);
  await startWebAssembly(components, findEnvironment(webAssemblyComponents));
}

function findEnvironment(webAssemblyComponents: WebAssemblyComponentDescriptor[]): string | undefined {
  for (let index = 0; index < webAssemblyComponents.length; index++) {
    const component = webAssemblyComponents[index];
    if (component.environment) {
      return component.environment;
    }
  }

  return undefined;
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot().catch(printErr);
}
