import 'angular2-universal/polyfills';
import * as ngCore from '@angular/core';
import * as ngUniversal from 'angular2-universal';
import { BASE_URL, ORIGIN_URL, REQUEST_URL } from 'angular2-universal/common';
import { App } from './components/app/app';

const bootloader = ngUniversal.bootloader({
    async: true,
    preboot: false,
    platformProviders: [
        ngCore.provide(BASE_URL, { useValue: '/' })
    ]
});

export default function (params: any): Promise<{ html: string, globals?: any }> {
    const config: ngUniversal.AppConfig = {
        directives: [App],
        providers: [
            ngCore.provide(REQUEST_URL, { useValue: params.url }),
            ngCore.provide(ORIGIN_URL, { useValue: params.origin }),
            ...ngUniversal.NODE_PLATFORM_PIPES,
            ...ngUniversal.NODE_ROUTER_PROVIDERS,
            ...ngUniversal.NODE_HTTP_PROVIDERS,
        ],
        // TODO: Render just the <app> component instead of wrapping it inside an extra HTML document
        // Waiting on https://github.com/angular/universal/issues/347
        template: '<!DOCTYPE html>\n<html><head></head><body><app></app></body></html>'
    };

    return bootloader.serializeApplication(config).then(html => {
        return { html };
    });
}
