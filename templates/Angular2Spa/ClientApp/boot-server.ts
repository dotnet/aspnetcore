import 'angular2-universal/polyfills';
import * as ngCore from '@angular/core';
import { APP_BASE_HREF } from '@angular/common';
import { provideRouter } from '@angular/router';
import * as ngUniversal from 'angular2-universal';
import { BASE_URL, ORIGIN_URL, REQUEST_URL } from 'angular2-universal/common';
import { App } from './components/app/app';
import { routes } from './routes';

const bootloader = ngUniversal.bootloader({
    async: true,
    preboot: false,
    platformProviders: [
        ngCore.provide(APP_BASE_HREF, { useValue: '/' }),
    ]
});

export default function (params: any): Promise<{ html: string, globals?: any }> {
    const config: ngUniversal.AppConfig = {
        directives: [App],
        providers: [
            ngCore.provide(ORIGIN_URL, { useValue: params.origin }),
            ngCore.provide(REQUEST_URL, { useValue: params.url }),
            ...ngUniversal.NODE_HTTP_PROVIDERS,
            provideRouter(routes),
            ...ngUniversal.NODE_LOCATION_PROVIDERS,
        ],
        // TODO: Render just the <app> component instead of wrapping it inside an extra HTML document
        // Waiting on https://github.com/angular/universal/issues/347
        template: '<!DOCTYPE html>\n<html><head></head><body><app></app></body></html>'
    };

    return bootloader.serializeApplication(config).then(html => {
        return { html };
    });
}
