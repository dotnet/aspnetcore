import * as ko from 'knockout';
import { Route, Router } from '../../router';

// Declare the client-side routing configuration
const routes: Route[] = [
    { url: '',              params: { page: 'home-page' } },
    { url: 'counter',       params: { page: 'counter-example' } },
    { url: 'fetch-data',    params: { page: 'fetch-data' } }
];

class AppRootViewModel {
    public route: KnockoutObservable<Route>;
    private _router: Router;
    
    constructor(params: { history: HistoryModule.History }) {
        // Activate the client-side router
        this._router = new Router(params.history, routes)
        this.route = this._router.currentRoute;
        
        // Load and register all the KO components needed to handle the routes
        ko.components.register('nav-menu', require('../nav-menu/nav-menu').default);
        ko.components.register('home-page', require('../home-page/home-page').default);
        ko.components.register('counter-example', require('../counter-example/counter-example').default);
        ko.components.register('fetch-data', require('../fetch-data/fetch-data').default);
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
