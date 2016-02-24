import * as React from 'react';
import { Router, Route, HistoryBase } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { About } from './components/About';
import { Counter } from './components/Counter';

export const routes = <Route component={ Layout }>
    <Route path="/" components={{ body: Home }} />
    <Route path="/about" components={{ body: About }} />
    <Route path="/counter" components={{ body: Counter }} />
</Route>;
