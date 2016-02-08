import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { browserHistory, Router } from 'react-router';
import { Provider } from 'react-redux';
React; // Need this reference otherwise TypeScript doesn't think we're using it and ignores the import

import './styles/styles.css';
import 'bootstrap/dist/css/bootstrap.css';
import configureStore from './configureStore';
import { routes } from './routes';

const store = configureStore(browserHistory);

ReactDOM.render(
    <Provider store={ store }>
        <Router history={ browserHistory } children={ routes } />
    </Provider>,
    document.getElementById('react-app')
);
