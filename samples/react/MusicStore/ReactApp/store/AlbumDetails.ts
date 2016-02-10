import { fetch } from 'domain-task/fetch';
import { typeName, isActionType, Action, Reducer } from 'redux-typed';
import { ActionCreator } from './';
import { Genre } from './GenreList';

// -----------------
// STATE - This defines the type of data maintained in the Redux store.

export interface AlbumDetailsState {
    album: AlbumDetails;
    requestedAlbumId: number;
}

export interface AlbumDetails {
    AlbumId: number;
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
        // Only load if it's not already loaded (or currently being loaded)
        if (albumId !== getState().albumDetails.requestedAlbumId) {
            fetch(`/api/albums/${ albumId }`)
                .then(results => results.json())
                .then(album => {
                    // Only replace state if it's still the most recent request
                    if (albumId === getState().albumDetails.requestedAlbumId) {
                        dispatch(new ReceiveAlbumDetails(album));
                    }
                });
            
            dispatch(new RequestAlbumDetails(albumId));
        }
    }
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.
// For unrecognized actions, must return the existing state (or default initial state if none was supplied).
const unloadedState: AlbumDetailsState = { requestedAlbumId: null as number, album: null };
export const reducer: Reducer<AlbumDetailsState> = (state, action) => {
    if (isActionType(action, RequestAlbumDetails)) {
        return { requestedAlbumId: action.albumId, album: null };
    } else if (isActionType(action, ReceiveAlbumDetails)) {
        return { requestedAlbumId: action.album.AlbumId, album: action.album };
    } else {
        return state || unloadedState;
    }
};
