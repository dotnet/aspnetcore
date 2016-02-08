import * as React from 'react';
import { Provider } from 'react-redux';
import { renderToString } from 'react-dom/server';
import { match, RouterContext } from 'react-router';
import createMemoryHistory from 'history/lib/createMemoryHistory';
React;

import { routes } from './routes';
import configureStore from './configureStore';
import { ApplicationState }  from './store';

export default function (params: any, callback: (err: any, result: { html: string, state: any }) => void) {
    const { location } = params;
    match({ routes, location }, (error, redirectLocation, renderProps: any) => {
        try {
            if (error) {
                throw error;
            }

            const history = createMemoryHistory(params.url);
            const store = params.state as Redux.Store || configureStore(history);
            let html = renderToString(
                <Provider store={ store }>
                    <RouterContext {...renderProps} />
                </Provider>
            );
            
            // Also serialise the Redux state so the client can pick up where the server left off
            html += `<script>window.__redux_state = ${ JSON.stringify(store.getState()) }</script>`;
        
            callback(null, { html, state: store });
        } catch (error) {
            callback(error, null);
        }
    });
}
