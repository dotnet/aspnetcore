import * as ko from 'knockout';
import { Route, Router } from '../../router';
import navMenu from '../nav-menu/nav-menu';

// Declare the client-side routing configuration
const routes: Route[] = [
    { url: '',              params: { page: 'home-page' } },
    { url: 'counter',       params: { page: 'counter-example' } },
    { url: 'fetch-data',    params: { page: 'fetch-data' } }
];

class AppRootViewModel {
    public route: KnockoutObservable<Route>;
    private _router: Router;

    constructor(params: { history: History.History }) {
        // Activate the client-side router
        this._router = new Router(params.history, routes)
        this.route = this._router.currentRoute;

        // Load and register all the KO components needed to handle the routes
        // The optional 'bundle?lazy!' prefix is a Webpack feature that causes the referenced modules
        // to be split into separate files that are then loaded on demand.
        // For docs, see https://github.com/webpack/bundle-loader
        ko.components.register('nav-menu', navMenu);
        ko.components.register('home-page', require('bundle?lazy!../home-page/home-page'));
        ko.components.register('counter-example', require('bundle?lazy!../counter-example/counter-example'));
        ko.components.register('fetch-data', require('bundle?lazy!../fetch-data/fetch-data'));
    }

    // To support hot module replacement, this method unregisters the router and KO components.
    // In production scenarios where hot module replacement is disabled, this would not be invoked.
    public dispose() {
        this._router.dispose();

        // TODO: Need a better API for this
        Object.getOwnPropertyNames((<any>ko).components._allRegisteredComponents).forEach(componentName => {
            ko.components.unregister(componentName);
        });
    }
}

export default { viewModel: AppRootViewModel, template: require('./app-root.html') };
