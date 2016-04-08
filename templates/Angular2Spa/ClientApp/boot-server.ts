import 'angular2-universal/polyfills';
import * as ngCore from 'angular2/core';
import * as ngRouter from 'angular2/router';
import * as ngUniversal from 'angular2-universal';
import { App } from './components/app/app';

export default function (params: any): Promise<{ html: string, globals?: any }> {
    const serverBindings = [
        ngCore.provide(ngUniversal.BASE_URL, { useValue: params.absoluteUrl }),
        ngCore.provide(ngUniversal.REQUEST_URL, { useValue: params.url }),
        ngCore.provide(ngRouter.APP_BASE_HREF, { useValue: '/' }),
        ngUniversal.NODE_HTTP_PROVIDERS,
        ngUniversal.NODE_ROUTER_PROVIDERS
    ];
    
    return ngUniversal.bootloader({
        directives: [App],
        providers: serverBindings,
        async: true,
        preboot: false,
        // TODO: Render just the <app> component instead of wrapping it inside an extra HTML document
        // Waiting on https://github.com/angular/universal/issues/347
        template: '<!DOCTYPE html>\n<html><head></head><body><app></app></body></html>'
    }).serializeApplication().then(html => {
        return { html };
    });
}
