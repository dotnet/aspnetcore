import * as ng from 'angular2/angular2';
import * as router from 'angular2/router';
import { Http, HTTP_BINDINGS } from 'angular2/http';
import { App } from './app';

ng.bootstrap(App, [router.ROUTER_BINDINGS, HTTP_BINDINGS, ng.FormBuilder]);
