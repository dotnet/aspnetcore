import 'bootstrap/dist/css/bootstrap.css';
import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
////#if (IndividualLocalAuth)
// Uncomment the lines below to register the generated service worker.
// By default create-react-app includes a service worker to improve the
// performance of the application by caching static assets. This service
// worker, can however try and handle paths for the identity UI, so it is
// disabled by default when Identity is being used.
//
//import registerServiceWorker from './registerServiceWorker';
////#else
import registerServiceWorker from './registerServiceWorker';
////#endif


const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

ReactDOM.render(
  <BrowserRouter basename={baseUrl}>
    <App />
  </BrowserRouter>,
  rootElement);

////#if (IndividualLocalAuth)
// Uncomment the lines below to register the generated service worker.
// By default create-react-app includes a service worker to improve the
// performance of the application by caching static assets. This service
// worker, can however try and handle paths for the identity UI, so it is
// disabled by default when Identity is being used.
//
//registerServiceWorker();
////#else
registerServiceWorker();
////#endif
