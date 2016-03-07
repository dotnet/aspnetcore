import { createStore, applyMiddleware, compose, combineReducers } from 'redux';
import * as thunkModule from 'redux-thunk';
import { routerReducer } from 'react-router-redux';
import * as Store from './store';
import { typedToPlain } from 'redux-typed';

export default function configureStore(initialState?: Store.ApplicationState) {
    // Build middleware. These are functions that can process the actions before they reach the store.
    const thunk = (thunkModule as any).default; // Workaround for TypeScript not importing thunk module as expected
    const windowIfDefined = typeof window === 'undefined' ? null : window as any;
    const devToolsExtension = windowIfDefined && windowIfDefined.devToolsExtension; // If devTools is installed, connect to it
    const createStoreWithMiddleware = compose(
        applyMiddleware(thunk, typedToPlain),
        devToolsExtension ? devToolsExtension() : f => f
    )(createStore);

    // Combine all reducers and instantiate the app-wide store instance
    const allReducers = buildRootReducer(Store.reducers);
    const store = createStoreWithMiddleware(allReducers, initialState) as Redux.Store;

    // Enable Webpack hot module replacement for reducers
    if (module.hot) {
        module.hot.accept('./store', () => {
            const nextRootReducer = require<typeof Store>('./store');
            store.replaceReducer(buildRootReducer(nextRootReducer.reducers));
        });
    }

    return store;
}

function buildRootReducer(allReducers) {
    return combineReducers(Object.assign({}, allReducers, { routing: routerReducer })) as Redux.Reducer;
}
