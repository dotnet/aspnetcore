import * as ko from 'knockout';
import * as crossroads from 'crossroads';
import * as hasher from 'hasher';
import { routes } from './routes';

// This module configures crossroads.js, a routing library. If you prefer, you
// can use any other routing library (or none at all) as Knockout is designed to
// compose cleanly with external libraries.
//
// You *don't* have to follow the pattern established here (each route entry
// specifies a 'page', which is a Knockout component) - there's nothing built into
// Knockout that requires or even knows about this technique. It's just one of
// many possible ways of setting up client-side routes.

export class Router {
    public currentRoute = ko.observable<Route>({});
    
    constructor(routes: Route[]) {
        // Configure Crossroads route handlers
        routes.forEach(route => {
            crossroads.addRoute(route.url, (requestParams) => {
                this.currentRoute(ko.utils.extend(requestParams, route.params));
            });
        });

        // Activate Crossroads
        crossroads.normalizeFn = crossroads.NORM_AS_OBJECT;
        hasher.initialized.add(hash => crossroads.parse(hash));
        hasher.changed.add(hash => crossroads.parse(hash));
        hasher.init();
    }
}

export interface Route {
    url?: string;
    params?: any;
}

export function instance() {
    // Ensure there's only one instance. This is needed to support hot module replacement.
    const windowOrDefault: any = typeof window === 'undefined' ? {} : window;
    windowOrDefault._router = windowOrDefault._router || new Router(routes);
    return windowOrDefault._router;
}
