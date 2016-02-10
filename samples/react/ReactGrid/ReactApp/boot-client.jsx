import React from 'react';
import ReactDOM from 'react-dom';
import { browserHistory } from 'react-router';
import { ReactApp } from './components/ReactApp.jsx';
import 'bootstrap/dist/css/bootstrap.css';

// In the browser, we render into a DOM node and hook up to the browser's history APIs
ReactDOM.render(<ReactApp history={ browserHistory } />, document.getElementById('react-app'));
