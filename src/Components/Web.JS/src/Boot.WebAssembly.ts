// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable array-element-newline */
import { Blazor } from './GlobalExports';
import { Module } from './Platform/Mono/MonoPlatform';
import { shouldAutoStart } from './BootCommon';
import { WebAssemblyStartOptions } from './Platform/WebAssemblyStartOptions';
import { startWebAssembly } from './Boot.WebAssembly.Common';
import { WebAssemblyComponentDescriptor, discoverComponents } from './Services/ComponentDescriptorDiscovery';
import { DotNet } from '@microsoft/dotnet-js-interop';

let started = false;

async function boot(options?: Partial<WebAssemblyStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  const webAssemblyComponents = discoverComponents(document, 'webassembly') as WebAssemblyComponentDescriptor[];
  await startWebAssembly(options, webAssemblyComponents);
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot().catch(error => {
    if (typeof Module !== 'undefined' && Module.printErr) {
      // Logs it, and causes the error UI to appear
      Module.printErr(error);
    } else {
      // The error must have happened so early we didn't yet set up the error UI, so just log to console
      console.error(error);
    }
  });
}
