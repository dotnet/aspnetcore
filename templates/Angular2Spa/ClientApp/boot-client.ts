import 'angular2/bundles/angular2-polyfills.js';
import 'bootstrap';
import './styles/site.css';

import { bootstrap } from 'angular2/platform/browser';
import { FormBuilder } from 'angular2/common';
import * as router from 'angular2/router';
import { Http, HTTP_PROVIDERS } from 'angular2/http';
import { App } from './components/app/app';

bootstrap(App, [router.ROUTER_BINDINGS, HTTP_PROVIDERS, FormBuilder]);

// Basic hot reloading support. Automatically reloads and restarts the Angular 2 app each time
// you modify source files. This will not preserve any application state other than the URL.
declare var module: any;
if (module.hot) {
    module.hot.accept();
}
