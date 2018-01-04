import { platform } from './Environment'
import { registerFunction } from './RegisteredFunction';

// This file defines an export that, when the library is loaded in a browser via a
// <script> element, will be attached to the global namespace
const blazorInstance = {
  platform: platform,
  registerFunction: registerFunction
};

if (typeof window !== 'undefined') {
  window['Blazor'] = blazorInstance;
}
