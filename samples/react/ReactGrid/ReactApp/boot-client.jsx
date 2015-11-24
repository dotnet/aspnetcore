import React from 'react';
import ReactDOM from 'react-dom';
import createBrowserHistory from 'history/lib/createBrowserHistory';
import ReactApp from './components/ReactApp.jsx';
import 'bootstrap/dist/css/bootstrap.css';

// In the browser, we render into a DOM node and hook up to the browser's history APIs
var history = createBrowserHistory();
ReactDOM.render(<ReactApp history={ history } />, document.getElementById('react-app'));
