import { ActionCreatorGeneric } from '../fx/TypedRedux';
import * as FeaturedAlbums from './FeaturedAlbums';
import * as GenreList from './GenreList';
import * as GenreDetails from './GenreDetails';
import * as AlbumDetails from './AlbumDetails';

// The top-level state object
export interface ApplicationState {
    featuredAlbums: FeaturedAlbums.FeaturedAlbumsState;
    genreList: GenreList.GenresListState,
    genreDetails: GenreDetails.GenreDetailsState,
    albumDetails: AlbumDetails.AlbumDetailsState
}

// Whenever an action is dispatched, Redux will update each top-level application state property using
// the reducer with the matching name. It's important that the names match exactly, and that the reducer
// acts on the corresponding ApplicationState property type.
export const reducers = {
    featuredAlbums: FeaturedAlbums.reducer,
    genreList: GenreList.reducer,
    genreDetails: GenreDetails.reducer,
    albumDetails: AlbumDetails.reducer
};

// This type can be used as a hint on action creators so that its 'dispatch' and 'getState' params are
// correctly typed to match your store. 
export type ActionCreator = ActionCreatorGeneric<ApplicationState>;
