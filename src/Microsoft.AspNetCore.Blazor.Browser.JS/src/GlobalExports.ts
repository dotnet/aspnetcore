import { platform } from './Environment';
import { navigateTo, internalFunctions as uriHelperInternalFunctions } from './Services/UriHelper';
import { internalFunctions as httpInternalFunctions } from './Services/Http';
import { attachRootComponentToElement, renderBatch } from './Rendering/Renderer';

if (typeof window !== 'undefined') {
  // When the library is loaded in a browser via a <script> element, make the
  // following APIs available in global scope for invocation from JS
  window['Blazor'] = {
    platform,
    navigateTo,

    _internal: {
      attachRootComponentToElement,
      renderBatch,
      http: httpInternalFunctions,
      uriHelper: uriHelperInternalFunctions
    }
  };
}
