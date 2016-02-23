import 'bootstrap/dist/css/bootstrap.css';

import { bootstrap } from 'angular2/platform/browser';
import { FormBuilder } from 'angular2/common';
import * as router from 'angular2/router';
import { Http, HTTP_PROVIDERS } from 'angular2/http';
import { App } from './components/app/app';

bootstrap(App, [router.ROUTER_BINDINGS, HTTP_PROVIDERS, FormBuilder]);
