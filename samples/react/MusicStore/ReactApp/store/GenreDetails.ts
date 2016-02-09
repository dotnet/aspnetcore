import { fetch } from 'domain-task/fetch';
import { typeName, isActionType, Action, Reducer } from 'redux-typed';
import { ActionCreator } from './';
import { Album } from './FeaturedAlbums';

// -----------------
// STATE - This defines the type of data maintained in the Redux store.

export interface GenreDetailsState {
    isLoaded: boolean;
    albums: Album[];
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.
// Use @typeName and isActionType for type detection that works even after serialization/deserialization.

@typeName("REQUEST_GENRE_DETAILS")
class RequestGenreDetails extends Action {
    constructor(public genreId: number) {
        super();
    }
}

@typeName("RECEIVE_GENRE_DETAILS")
class ReceiveGenreDetails extends Action {
    constructor(public albums: Album[]) {
        super();
    }
}

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {    
    requestGenreDetails: (genreId: number): ActionCreator => (dispatch, getState) => {
        fetch(`/api/genres/${ genreId }/albums`)
            .then(results => results.json())
            .then(albums => dispatch(new ReceiveGenreDetails(albums)));
        
        dispatch(new RequestGenreDetails(genreId));
    }
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.
// For unrecognized actions, must return the existing state (or default initial state if none was supplied).
const unloadedState: GenreDetailsState = { isLoaded: false, albums: [] };
export const reducer: Reducer<GenreDetailsState> = (state, action) => {
    if (isActionType(action, RequestGenreDetails)) {
        return unloadedState;
    } else if (isActionType(action, ReceiveGenreDetails)) {
        return { isLoaded: true, albums: action.albums };
    } else {
        return state || unloadedState;
    }
};
