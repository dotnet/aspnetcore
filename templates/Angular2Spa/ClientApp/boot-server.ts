import 'angular2-universal-polyfills';
import * as ngCore from 'angular2/core';
import * as ngRouter from 'angular2/router';
import * as ngUniversal from 'angular2-universal-preview';
import { App } from './components/app/app';

export default function (params: any): Promise<{ html: string, globals?: any }> {
    const serverBindings = [
        ngRouter.ROUTER_BINDINGS,
        ngUniversal.HTTP_PROVIDERS,
        ngUniversal.NODE_LOCATION_PROVIDERS,
        ngCore.provide(ngRouter.APP_BASE_HREF, { useValue: '/' }),
        ngCore.provide(ngUniversal.BASE_URL, { useValue: params.absoluteUrl }),
        ngCore.provide(ngUniversal.REQUEST_URL, { useValue: params.url })
    ];

    return ngUniversal.renderDocument('<app />', App, serverBindings).then(html => {
        return { html };
    });
}
