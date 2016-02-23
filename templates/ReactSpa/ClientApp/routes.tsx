import * as React from 'react';
import { Router, Route, HistoryBase } from 'react-router';
import { NavMenu } from './components/NavMenu';
import { Home } from './components/Home';
import { About } from './components/About';
import { Counter } from './components/Counter';

class Layout extends React.Component<{ body: React.ReactElement<any> }, void> {
    public render() {
        return <div>
            <NavMenu />
            <div className="container body-content">
                { this.props.body }
                <hr />
                <footer>
                    <p>&copy; 2016 - WebApplicationBasic</p>
                </footer>
            </div>
        </div>;
    }
}

export const routes = <Route component={ Layout }>
    <Route path="/" components={{ body: Home }} />
    <Route path="/about" components={{ body: About }} />
    <Route path="/counter" components={{ body: Counter }} />
</Route>;
