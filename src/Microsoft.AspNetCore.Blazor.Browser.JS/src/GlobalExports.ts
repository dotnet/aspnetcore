import { platform } from './Environment'
import { registerFunction } from './Interop/RegisteredFunction';
import { navigateTo } from './Services/UriHelper';
import { invokeDotNetMethod, invokeDotNetMethodAsync } from './Interop/InvokeDotNetMethodWithJsonMarshalling';

if (typeof window !== 'undefined') {
  // When the library is loaded in a browser via a <script> element, make the
  // following APIs available in global scope for invocation from JS
  window['Blazor'] = {
    platform,
    registerFunction,
    navigateTo,
    invokeDotNetMethod,
    invokeDotNetMethodAsync
  };
}
