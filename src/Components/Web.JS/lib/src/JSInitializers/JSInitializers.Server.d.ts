import { CircuitStartOptions } from '../Platform/Circuits/CircuitStartOptions';
import { JSInitializer } from './JSInitializers';
export declare function fetchAndInvokeInitializers(options: Partial<CircuitStartOptions>): Promise<JSInitializer>;
