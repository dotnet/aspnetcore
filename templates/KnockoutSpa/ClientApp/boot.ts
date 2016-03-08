import 'bootstrap';
import 'bootstrap/dist/css/bootstrap.css';
import './css/site.css';
import * as ko from 'knockout';
import appLayout from './components/app-layout/app-layout';

ko.components.register('app-layout', appLayout);
ko.applyBindings();

// Basic hot reloading support. Automatically reloads and restarts the Knockout app each time
// you modify source files. This will not preserve any application state other than the URL.
declare var module: any;
if (module.hot) {
    module.hot.dispose(() => {
        ko.cleanNode(document.body);
        
        // TODO: Need a better API for this
        Object.getOwnPropertyNames((<any>ko).components._allRegisteredComponents).forEach(componentName => {
            ko.components.unregister(componentName);
        });
    });
    module.hot.accept();    
}
