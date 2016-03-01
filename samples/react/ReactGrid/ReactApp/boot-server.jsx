import React from 'react';
import { renderToString } from 'react-dom/server';
import { match, RouterContext } from 'react-router';
import { routes } from './components/ReactApp';
React;

export default function renderApp (params) {
    return new Promise((resolve, reject) => {
        // Match the incoming request against the list of client-side routes
        match({ routes, location: params.location }, (error, redirectLocation, renderProps) => {
            if (error) {
                throw error;
            }

            // Build an instance of the application
            const app = <RouterContext {...renderProps} />;

            // Render it as an HTML string which can be injected into the response
            const html = renderToString(app);
            resolve({ html });
        });
    });
}
