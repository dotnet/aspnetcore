import 'angular2-universal/polyfills';
import * as ngCore from '@angular/core';
import * as ngRouter from '@angular/router-deprecated';
import * as ngUniversal from 'angular2-universal';
import { BASE_URL, ORIGIN_URL, REQUEST_URL } from 'angular2-universal/common';
import { App } from './components/app/app';

export default function (params: any): Promise<{ html: string, globals?: any }> {
    const serverBindings = [
        ngCore.provide(BASE_URL, { useValue: '/' }),
        ngCore.provide(ORIGIN_URL, { useValue: params.origin }),
        ngCore.provide(REQUEST_URL, { useValue: params.url }),
        ...ngUniversal.NODE_PLATFORM_PIPES,
        ...ngUniversal.NODE_ROUTER_PROVIDERS,
        ...ngUniversal.NODE_HTTP_PROVIDERS,
    ];

    let bootloader = ngUniversal.bootloader({
        directives: [App],
        componentProviders: serverBindings,
        async: true,
        preboot: false,
        // TODO: Render just the <app> component instead of wrapping it inside an extra HTML document
        // Waiting on https://github.com/angular/universal/issues/347
        template: '<!DOCTYPE html>\n<html><head></head><body><app></app></body></html>'
    });

    return bootloader.serializeApplication().then(html => {
        bootloader.dispose();
        return { html };
    });
}
