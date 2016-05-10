import * as ng from '@angular/core';
import * as router from '@angular/router-deprecated';
import { Http, HTTP_BINDINGS } from '@angular/http';
import { NavMenu } from '../nav-menu/nav-menu';
import { Home } from '../home/home';
import { FetchData } from '../fetch-data/fetch-data';
import { Counter } from '../counter/counter';

@ng.Component({
    selector: 'app',
    template: require('./app.html'),
    directives: [NavMenu, router.ROUTER_DIRECTIVES]
})
@router.RouteConfig([
    { path: '/', component: Home, name: 'Home' },
    { path: '/counter', component: Counter, name: 'Counter' },
    { path: '/fetch-data', component: FetchData, name: 'FetchData' }
])
export class App {
}
