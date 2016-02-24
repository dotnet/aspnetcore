import * as React from 'react';
import { renderToString } from 'react-dom/server';
import { match, RouterContext } from 'react-router';
import createMemoryHistory from 'history/lib/createMemoryHistory';
import { routes } from './routes';

// The 'asp-prerender-module' tag helper invokes the following function when the React app is to
// be prerendered on the server. It runs asynchronously, and issues a callback with the React app's
// initial HTML and any other state variables.

export default function (params: any): Promise<{ html: string }> {
    return new Promise<{ html: string, globals: { [key: string]: any } }>((resolve, reject) => {
        // Match the incoming request against the list of client-side routes, and reject if there was no match
        match({ routes, location: params.location }, (error, redirectLocation, renderProps: any) => {
            if (error) {
                throw error;
            }

            // Build an instance of the application and perform an initial render.
            // This will cause any async tasks (e.g., data access) to begin.
            const history = createMemoryHistory(params.url);
            const app = <RouterContext {...renderProps} />;
            renderToString(app);

            // Once the tasks are done, we can perform the final render
            params.domainTasks.then(() => {
                resolve({
                    html: renderToString(app),
                    globals: { /* Supply any other JSON-serializable data you want to make available on the client */ }
                });
            }, reject); // Also propagate any errors back into the host application
        });
    });
}
