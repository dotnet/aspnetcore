// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Expose an export called 'platform' of the interface type 'Platform',
// so that consumers can be agnostic about which implementation they use.
// Basic alternative to having an actual DI container.
import { Platform } from './Platform/Platform';

export let platform: Platform;

export function setPlatform(platformInstance: Platform): Platform {
  platform = platformInstance;
  return platform;
}
