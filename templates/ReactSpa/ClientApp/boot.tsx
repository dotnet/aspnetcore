import './css/site.css';
import 'bootstrap';
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { BrowserRouter as Router } from 'react-router-dom';
import routes from './routes';

// This code starts up the React app when it runs in a browser. It sets up the routing configuration
// and injects the app into a DOM element.
ReactDOM.render(
    <Router children={ routes } />,
    document.getElementById('react-app')
);
