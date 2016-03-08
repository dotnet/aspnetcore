import * as ko from 'knockout';
import { Route } from './router';

import navMenu from './components/nav-menu/nav-menu';
import homePage from './components/home-page/home-page';
import counterExample from './components/counter-example/counter-example';
import fetchData from './components/fetch-data/fetch-data';

ko.components.register('nav-menu', navMenu);
ko.components.register('home-page', homePage);
ko.components.register('counter-example', counterExample);
ko.components.register('fetch-data', fetchData);

export const routes: Route[] = [
    { url: '',              params: { page: 'home-page' } },
    { url: 'counter',       params: { page: 'counter-example' } },
    { url: 'fetch-data',    params: { page: 'fetch-data' } }
];
