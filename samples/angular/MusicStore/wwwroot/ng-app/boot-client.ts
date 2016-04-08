import { bootstrap } from 'angular2/platform/browser';
import { FormBuilder } from 'angular2/common';
import * as router from 'angular2/router';
import { Http, HTTP_PROVIDERS } from 'angular2/http';
import { CACHE_PRIMED_HTTP_PROVIDERS } from 'angular2-aspnet';
import { App } from './components/app/app';

bootstrap(App, [router.ROUTER_BINDINGS, HTTP_PROVIDERS, CACHE_PRIMED_HTTP_PROVIDERS, FormBuilder]);
