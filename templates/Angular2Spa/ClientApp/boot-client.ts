import 'angular2-universal-polyfills/browser';
import 'bootstrap';
import './styles/site.css';
import { enableProdMode } from '@angular/core';
import { platformUniversalDynamic } from 'angular2-universal';
import { AppModule } from './app/app.module';

enableProdMode();
platformUniversalDynamic().bootstrapModule(AppModule);

// Basic hot reloading support. Automatically reloads and restarts the Angular 2 app each time
// you modify source files. This will not preserve any application state other than the URL.
declare var module: any;
if (module.hot) {
    module.hot.accept();
}
