import './css/site.css';
import 'bootstrap';
import * as ko from 'knockout';
import { createBrowserHistory } from 'history';
import './webpack-component-loader';
import AppRootComponent from './components/app-root/app-root';
const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href')!;
const basename = baseUrl.substring(0, baseUrl.length - 1); // History component needs no trailing slash

// Load and register the <app-root> component
ko.components.register('app-root', AppRootComponent);

// Tell Knockout to start up an instance of your application
ko.applyBindings({ history: createBrowserHistory({ basename }), basename });

// Basic hot reloading support. Automatically reloads and restarts the Knockout app each time
// you modify source files. This will not preserve any application state other than the URL.
if (module.hot) {
    module.hot.accept();
    module.hot.dispose(() => ko.cleanNode(document.body));
}
