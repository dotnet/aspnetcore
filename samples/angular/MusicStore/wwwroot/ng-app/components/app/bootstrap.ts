import * as ng from 'angular2/angular2';
import * as router from 'angular2/router';
import { Http, HTTP_PROVIDERS } from 'angular2/http';
import { CACHE_PRIMED_HTTP_PROVIDERS } from 'angular2-aspnet';
import { App } from './app';

ng.bootstrap(App, [router.ROUTER_BINDINGS, HTTP_PROVIDERS, CACHE_PRIMED_HTTP_PROVIDERS, ng.FormBuilder]);
