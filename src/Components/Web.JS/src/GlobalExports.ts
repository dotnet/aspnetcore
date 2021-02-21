import { navigateTo, internalFunctions as navigationManagerInternalFunctions } from './Services/NavigationManager';
import { domFunctions } from './DomWrapper';
import { Virtualize } from './Virtualize';
import { registerCustomEventType } from './Rendering/Events/EventTypes';

// Make the following APIs available in global scope for invocation from JS
window['Blazor'] = {
  navigateTo,
  registerCustomEventType,

  _internal: {
    navigationManager: navigationManagerInternalFunctions,
    domWrapper: domFunctions,
    Virtualize,
  },
};
