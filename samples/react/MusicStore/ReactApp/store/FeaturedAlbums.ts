import { fetch } from 'domain-task/fetch';
import { typeName, isActionType, Action, Reducer } from 'redux-typed';
import { ActionCreator } from './';

// -----------------
// STATE - This defines the type of data maintained in the Redux store.

export interface FeaturedAlbumsState {
    albums: Album[];
}

export interface Album {
    AlbumId: number;
    Title: string;
    AlbumArtUrl: string;
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.
// Use @typeName and isActionType for type detection that works even after serialization/deserialization.

@typeName("REQUEST_FEATURED_ALBUMS")
class RequestFeaturedAlbums extends Action {
}

@typeName("RECEIVE_FEATURED_ALBUMS")
class ReceiveFeaturedAlbums extends Action {
    constructor(public albums: Album[]) {
        super();
    }
}

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {
    requestFeaturedAlbums: (): ActionCreator => (dispatch, getState) => {
        fetch('/api/albums/mostPopular')
            .then(results => results.json())
            .then(albums => dispatch(new ReceiveFeaturedAlbums(albums)));
        
        return dispatch(new RequestFeaturedAlbums());        
    }
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.
// For unrecognized actions, must return the existing state (or default initial state if none was supplied).

export const reducer: Reducer<FeaturedAlbumsState> = (state, action) => {
    if (isActionType(action, ReceiveFeaturedAlbums)) {
        return { albums: action.albums };
    } else {
        return state || { albums: [] };
    }
};
