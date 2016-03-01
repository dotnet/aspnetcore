import 'angular2-universal-preview/dist/server/universal-polyfill.js';
import * as ngCore from 'angular2/core';
import * as ngRouter from 'angular2/router';
import * as ngUniversal from 'angular2-universal-preview';
import { BASE_URL } from 'angular2-universal-preview/dist/server/src/http/node_http';
import * as ngUniversalRender from 'angular2-universal-preview/dist/server/src/render';
import { App } from './components/app/app';

export default function (params: any): Promise<{ html: string, globals?: any }> {
    const serverBindings = [
        ngRouter.ROUTER_BINDINGS,
        ngUniversal.HTTP_PROVIDERS,
        ngCore.provide(BASE_URL, { useValue: params.absoluteUrl }),
        ngCore.provide(ngUniversal.REQUEST_URL, { useValue: params.url }),
        ngCore.provide(ngRouter.APP_BASE_HREF, { useValue: '/' }),
        ngUniversal.SERVER_LOCATION_PROVIDERS
    ];

    return ngUniversalRender.renderToString(App, serverBindings).then(html => {
        return { html, globals: {}};
    });
}
