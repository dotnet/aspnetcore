import { navigateTo, internalFunctions as navigationManagerInternalFunctions } from './Services/NavigationManager';
import { internalFunctions as httpInternalFunctions } from './Services/Http';
import { attachRootComponentToElement } from './Rendering/Renderer';

// Make the following APIs available in global scope for invocation from JS
window['Blazor'] = {
  navigateTo,

  _internal: {
    attachRootComponentToElement,
    http: httpInternalFunctions,
    navigationManager: navigationManagerInternalFunctions,
  },
};
