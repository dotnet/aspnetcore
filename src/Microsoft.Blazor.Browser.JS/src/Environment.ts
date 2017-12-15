// Expose an export called 'platform' of the interface type 'Platform',
// so that consumers can be agnostic about which implementation they use.
// Basic alternative to having an actual DI container.
import { Platform } from './Platform/Platform';
import { monoPlatform } from './Platform/Mono/MonoPlatform';
export const platform: Platform = monoPlatform;
