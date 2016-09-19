import 'angular2-universal-polyfills/browser';
import { enableProdMode } from '@angular/core';
import { platformUniversalDynamic } from 'angular2-universal';
import { AppModule } from './app/app.module';

// Include styles in the bundle
import 'bootstrap';
import './styles/site.css';

// Enable either Hot Module Reloading or production mode
const hotModuleReplacement = module['hot'];
if (hotModuleReplacement) {
    hotModuleReplacement.accept();
    hotModuleReplacement.dispose(() => { platform.destroy(); });
} else {
    enableProdMode();
}

// Boot the application
const platform = platformUniversalDynamic();
document.addEventListener('DOMContentLoaded', () => {
    platform.bootstrapModule(AppModule);
});
