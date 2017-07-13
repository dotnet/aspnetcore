import * as ko from 'knockout';
import * as $ from 'jquery';
import * as History from 'history';
import * as crossroads from 'crossroads';

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
    private disposeHistory: () => void;
    private clickEventListener: EventListener;

    constructor(private history: History.History, routes: Route[], basename: string) {
        // Reset and configure Crossroads so it matches routes and updates this.currentRoute
        crossroads.removeAllRoutes();
        crossroads.resetState();
        (crossroads as any).normalizeFn = crossroads.NORM_AS_OBJECT;
        routes.forEach(route => {
            crossroads.addRoute(route.url, (requestParams: any) => {
                this.currentRoute(ko.utils.extend(requestParams, route.params));
            });
        });

        // Make history.js watch for navigation and notify Crossroads
        this.disposeHistory = history.listen(location => crossroads.parse(location.pathname));
        this.clickEventListener = evt => {
            let target: any = evt.currentTarget;
            if (target && target.tagName === 'A') {
                let href = target.getAttribute('href');
                if (href && href.indexOf(basename + '/') === 0) {
                    const hrefAfterBasename = href.substring(basename.length);
                    history.push(hrefAfterBasename);
                    evt.preventDefault();
                }
            }
        };
        $(document).on('click', 'a', this.clickEventListener);

        // Initialize Crossroads with starting location
        crossroads.parse(history.location.pathname);
    }

    public link(url: string): string {
        return this.history.createHref({ pathname: url });
    }

    public dispose() {
        this.disposeHistory();
        $(document).off('click', 'a', this.clickEventListener);
    }
}

export interface Route {
    url?: string;
    params?: any;
}
