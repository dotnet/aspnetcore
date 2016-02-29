import 'angular2-universal-preview/dist/server/universal-polyfill.js';
import * as ngCore from 'angular2/core';
import * as ngRouter from 'angular2/router';
import * as ngUniversal from 'angular2-universal-preview';
import { BASE_URL } from 'angular2-universal-preview/dist/server/src/http/node_http';
import * as ngUniversalRender from 'angular2-universal-preview/dist/server/src/render';

// TODO: Make this ugly code go away, e.g., by somehow loading via Webpack
function loadAsString(module, filename) {
    module.exports = require('fs').readFileSync(filename, 'utf8');
}
(require as any).extensions['.html'] = loadAsString;
(require as any).extensions['.css'] = loadAsString;
let App: any = require('./components/app/app').App;

export default function (params: any): Promise<{ html: string }> {
    return new Promise<{ html: string, globals: { [key: string]: any } }>((resolve, reject) => {
        const serverBindings = [
            ngRouter.ROUTER_BINDINGS,
            ngUniversal.HTTP_PROVIDERS,
            ngCore.provide(BASE_URL, { useValue: params.absoluteUrl }),
            ngCore.provide(ngUniversal.REQUEST_URL, { useValue: params.url }),
            ngCore.provide(ngRouter.APP_BASE_HREF, { useValue: '/' }),
            ngUniversal.SERVER_LOCATION_PROVIDERS
        ];

        ngUniversalRender.renderToString(App, serverBindings).then(
            html => resolve({ html, globals: {} }), 
            reject // Also propagate any errors back into the host application
        );
    });
}
