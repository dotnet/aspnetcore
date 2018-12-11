import { applyMiddleware, combineReducers, compose, createStore, Reducer } from 'redux';
import thunk from 'redux-thunk';
import { connectRouter } from 'connected-react-router';
import { routerReducer, routerMiddleware } from 'react-router-redux';
import { History } from 'history';
import { ApplicationState, reducers } from './';

export default function configureStore(history: History, initialState?: ApplicationState) {
    const middleware = [
        thunk,
        routerMiddleware(history)
    ];

    const rootReducer = combineReducers({
        ...reducers,
        routing: routerReducer
    });

    const enhancers = [];
    const windowIfDefined = typeof window === 'undefined' ? null : window as any;
    if (windowIfDefined && windowIfDefined.__REDUX_DEVTOOLS_EXTENSION__) {
        enhancers.push(windowIfDefined.__REDUX_DEVTOOLS_EXTENSION__());
    }

    return createStore(
        connectRouter(history)(rootReducer),
        initialState,
        compose(applyMiddleware(...middleware), ...enhancers)
    );
}
