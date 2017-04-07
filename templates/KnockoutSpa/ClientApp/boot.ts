import './css/site.css';
import 'bootstrap';
import * as ko from 'knockout';
import './webpack-component-loader';
import AppRootComponent from './components/app-root/app-root';
const createHistory = require('history').createBrowserHistory;

// Load and register the <app-root> component
ko.components.register('app-root', AppRootComponent);

// Tell Knockout to start up an instance of your application
ko.applyBindings({ history: createHistory() });

// Basic hot reloading support. Automatically reloads and restarts the Knockout app each time
// you modify source files. This will not preserve any application state other than the URL.
declare var module: any;
if (module.hot) {
    module.hot.accept();
    module.hot.dispose(() => ko.cleanNode(document.body));
}
