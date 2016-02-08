import { fetch } from '../fx/tracked-fetch';
import { typeName, isActionType, Action, Reducer } from '../fx/TypedRedux';
import { ActionCreator } from './';
import { Genre } from './GenreList';

// -----------------
// STATE - This defines the type of data maintained in the Redux store.

export interface AlbumDetailsState {
    isLoaded: boolean;
    album: AlbumDetails;
}

export interface AlbumDetails {
    AlbumId: string;
    Title: string;
    AlbumArtUrl: string;
    Genre: Genre;
    Artist: Artist;
    Price: number;
}

interface Artist {
    Name: string;
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.
// Use @typeName and isActionType for type detection that works even after serialization/deserialization.

@typeName("REQUEST_ALBUM_DETAILS")
class RequestAlbumDetails extends Action {
    constructor(public albumId: number) {
        super();
    }
}

@typeName("RECEIVE_ALBUM_DETAILS")
class ReceiveAlbumDetails extends Action {
    constructor(public album: AlbumDetails) {
        super();
    }
}

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {    
    requestAlbumDetails: (albumId: number): ActionCreator => (dispatch, getState) => {
        fetch(`/api/albums/${ albumId }`)
            .then(results => results.json())
            .then(album => dispatch(new ReceiveAlbumDetails(album)));
        
        dispatch(new RequestAlbumDetails(albumId));
    }
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.
// For unrecognized actions, must return the existing state (or default initial state if none was supplied).
const unloadedState: AlbumDetailsState = { isLoaded: false, album: null };
export const reducer: Reducer<AlbumDetailsState> = (state, action) => {
    if (isActionType(action, RequestAlbumDetails)) {
        return unloadedState;
    } else if (isActionType(action, ReceiveAlbumDetails)) {
        return { isLoaded: true, album: action.album };
    } else {
        return state || unloadedState;
    }
};
