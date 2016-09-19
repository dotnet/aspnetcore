import { Routes } from '@angular/router';

import { Home, FetchData, Counter } from './components';

export const routes: Routes = [
    { path: '', redirectTo: 'home', pathMatch: 'full' },
    { path: 'home', component: Home },
    { path: 'counter', component: Counter },
    { path: 'fetch-data', component: FetchData },
    { path: '**', redirectTo: 'home' }
];
