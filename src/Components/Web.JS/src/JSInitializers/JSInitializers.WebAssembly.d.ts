import { BootJsonData } from '../Platform/BootConfig';
import { WebAssemblyStartOptions } from '../Platform/WebAssemblyStartOptions';
import { JSInitializer } from './JSInitializers';
export declare function fetchAndInvokeInitializers(bootConfig: BootJsonData, options: Partial<WebAssemblyStartOptions>): Promise<JSInitializer>;
