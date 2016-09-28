import './css/site.css';

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { browserHistory, Router } from 'react-router';
import routes from './routes';

// This code starts up the React app when it runs in a browser. It sets up the routing configuration
// and injects the app into a DOM element.
ReactDOM.render(
    <Router history={ browserHistory } children={ routes } />,
    document.getElementById('react-app')
);
