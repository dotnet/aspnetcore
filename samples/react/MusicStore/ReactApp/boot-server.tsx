import * as React from 'react';
import { Provider } from 'react-redux';
import { renderToString } from 'react-dom/server';
import { match, RouterContext } from 'react-router';
React;

import { routes } from './routes';
import configureStore from './configureStore';
import { ApplicationState }  from './store';

export default function (params: any, callback: (err: any, result: { html: string, store: Redux.Store }) => void) {
    match({ routes, location: params.location }, (error, redirectLocation, renderProps: any) => {
        try {
            if (error) {
                throw error;
            }

            const store = configureStore(params.history, params.state);
            const html = renderToString(
                <Provider store={ store }>
                    <RouterContext {...renderProps} />
                </Provider>
            );
        
            callback(null, { html, store });          
        } catch (error) {
            callback(error, null);
        }
    });
}
