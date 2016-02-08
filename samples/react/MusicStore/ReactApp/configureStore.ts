import { createStore, applyMiddleware, compose, combineReducers } from 'redux';
import * as thunkModule from 'redux-thunk';
import { syncHistory, routeReducer } from 'react-router-redux';
import * as Store from './store';
import { typedToPlain } from './TypedRedux';

export default function configureStore(history: HistoryModule.History, initialState?: Store.ApplicationState) {
    // Build middleware
    const thunk = (thunkModule as any).default; // Workaround for TypeScript not importing thunk module as expected
    const reduxRouterMiddleware = syncHistory(history);
    const middlewares = [thunk, reduxRouterMiddleware, typedToPlain];
    const devToolsExtension = null;//(window as any).devToolsExtension; // If devTools is installed, connect to it
  
    const finalCreateStore = compose(
        applyMiddleware(...middlewares),
        devToolsExtension ? devToolsExtension() : f => f
    )(createStore)

    // Combine all reducers
    const allReducers = buildRootReducer(Store.reducers);

    const store = finalCreateStore(allReducers, initialState) as Redux.Store;
  
    // Required for replaying actions from devtools to work
    reduxRouterMiddleware.listenForReplays(store);
  
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
    return combineReducers(Object.assign({}, allReducers, { routing: routeReducer })) as Redux.Reducer;
}
