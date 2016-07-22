import 'es6-shim';
require('zone.js');
import 'bootstrap';
import 'reflect-metadata';
import './styles/site.css';

import { bootstrap } from '@angular/platform-browser-dynamic';
import { FormBuilder } from '@angular/common';
import { provideRouter } from '@angular/router';
import { HTTP_PROVIDERS } from '@angular/http';
import { App } from './components/app/app';
import { routes } from './routes';

bootstrap(App, [
    ...HTTP_PROVIDERS,
    FormBuilder,
    provideRouter(routes)
]);

// Basic hot reloading support. Automatically reloads and restarts the Angular 2 app each time
// you modify source files. This will not preserve any application state other than the URL.
declare var module: any;
if (module.hot) {
    module.hot.accept();
}
