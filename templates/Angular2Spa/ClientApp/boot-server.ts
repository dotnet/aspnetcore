// the polyfills must be the first thing imported in node.js
import 'angular2-universal-polyfills';

// Angular 2
import { enableProdMode } from '@angular/core';
// Angular2 Universal
import { platformNodeDynamic } from 'angular2-universal';

// Application imports
import { MainModule } from './main.node';
import { App } from './components';
import { routes } from './routes';

// enable prod for faster renders
enableProdMode();

declare var Zone: any;

export default function (params: any) : Promise<{ html: string, globals?: any }> {

    const doc = `
        <!DOCTYPE html>\n
        <html>
            <head></head>
            <body>
                <app></app>
            </body>
        </html>
    `;

    // hold platform reference
    var platformRef =  platformNodeDynamic();

    var platformConfig = {
        ngModule: MainModule,
        document: doc,
        preboot: false,
        baseUrl: '/',
        requestUrl: params.url,
        originUrl: params.origin
    };

    // defaults
    var cancel = false;

    const _config = Object.assign({
      get cancel() { return cancel; },
      cancelHandler() { return Zone.current.get('cancel') }
    }, platformConfig);

    // for each user
    const zone = Zone.current.fork({
        name: 'UNIVERSAL request',
        properties: _config
    });

    
    return Promise.resolve(
        zone.run(() => {
            return platformRef.serializeModule(Zone.current.get('ngModule'))
        })
    ).then(html => {

        if (typeof html !== 'string' ) {
            return { html : doc };
        }
        return { html };

    }).catch(err => {
        
        console.log(err);
        return { html : doc };

    });

}
