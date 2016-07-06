import * as ko from 'knockout';
import { Route } from '../../router';

interface NavMenuParams {
    route: KnockoutObservable<Route>;
}

class NavMenuViewModel {
    public route: KnockoutObservable<Route>;

    constructor(params: NavMenuParams) {
        // This viewmodel doesn't do anything except pass through the 'route' parameter to the view.
        // You could remove this viewmodel entirely, and define 'nav-menu' as a template-only component.
        // But in most apps, you'll want some viewmodel logic to determine what navigation options appear.
        this.route = params.route;
    }
}

export default { viewModel: NavMenuViewModel, template: require('./nav-menu.html') };
