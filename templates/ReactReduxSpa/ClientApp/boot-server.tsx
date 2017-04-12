import * as React from 'react';
import { Provider } from 'react-redux';
import { renderToString } from 'react-dom/server';
import { StaticRouter } from 'react-router-dom';
import { replace } from "react-router-redux";
import { createMemoryHistory } from 'history';
import { createServerRenderer, RenderResult } from 'aspnet-prerendering';
import routes from './routes';
import configureStore from './configureStore';

export default createServerRenderer(params => {
    return new Promise<RenderResult>((resolve, reject) => {
        // Create memory history to use in the Redux store
        const history = createMemoryHistory();
        const store = configureStore(history);

        // Dispatch the current location so that the router knows where to go
        store.dispatch(replace(params.location));

        const context : any = {};

        const app = (
            <Provider store={ store }>
                <StaticRouter context={ context } location={ params.location.path } children={ routes } />
            </Provider>
        );

        // Perform an initial render that will cause any async tasks (e.g., data access) to begin
        renderToString(app);

        // If there's a redirection, just send this information back to the host application (Maybe improve this?)
        if (context.url) {
            resolve({ redirectUrl: context.url });
            return;
        }
        
        // Once the tasks are done, we can perform the final render
        // We also send the redux store state, so the client can continue execution where the server left off
        params.domainTasks.then(() => {
            resolve({
                html: renderToString(app),
                globals: { initialReduxState: store.getState() }
            });
        }, reject); // Also propagate any errors back into the host application
    });
});
