import React from 'react';
import { Router, Route } from 'react-router';
import { PeopleGrid } from './PeopleGrid.jsx';
import { PersonEditor } from './PersonEditor.jsx';

export default class ReactApp extends React.Component {
    render() {
        return (
            <Router history={this.props.history}>
                <Route path="/" component={PeopleGrid} />
                <Route path="/:pageIndex" component={PeopleGrid} />
                <Route path="/edit/:personId" component={PersonEditor} />
            </Router>
        );
    }
}
