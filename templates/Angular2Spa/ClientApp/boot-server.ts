import 'angular2-universal-polyfills';
import 'angular2-universal-patch';
import 'zone.js';
import { createServerRenderer, RenderResult } from 'aspnet-prerendering';
import { enableProdMode } from '@angular/core';
import { platformNodeDynamic } from 'angular2-universal';
import { AppModule } from './app/app.module';

enableProdMode();
const platform = platformNodeDynamic();

export default createServerRenderer(params => {
    return new Promise<RenderResult>((resolve, reject) => {
        const requestZone = Zone.current.fork({
            name: 'angular-universal request',
            properties: {
                baseUrl: '/',
                requestUrl: params.url,
                originUrl: params.origin,
                preboot: false,
                document: '<app></app>'
            },
            onHandleError: (parentZone, currentZone, targetZone, error) => {
                // If any error occurs while rendering the module, reject the whole operation
                reject(error);
                return true;
            }
        });

        return requestZone.run<Promise<string>>(() => platform.serializeModule(AppModule)).then(html => {
            resolve({ html: html });
        }, reject);
    });
});
