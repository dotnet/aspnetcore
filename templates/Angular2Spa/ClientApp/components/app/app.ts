import * as ng from 'angular2/core';
import * as router from 'angular2/router';
import { Http, HTTP_BINDINGS } from 'angular2/http';
import { Home } from '../home/home.ts';
import { About } from '../about/about';
import { Counter } from '../counter/counter';

@ng.Component({
    selector: 'app'
})
@router.RouteConfig([
    { path: '/', component: Home, name: 'Home' },
    { path: '/about', component: About, name: 'About' },
    { path: '/counter', component: Counter, name: 'Counter' }
])
@ng.View({
    template: require('./app.html'),
    styles: [require('./app.css')],
    directives: [router.ROUTER_DIRECTIVES]
})
export class App {
}
