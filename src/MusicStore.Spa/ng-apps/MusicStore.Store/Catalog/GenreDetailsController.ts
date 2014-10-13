module MusicStore.Store.Catalog {
    interface IGenreDetailsViewModel {
        albums: Array<Models.IAlbum>;
    }

    interface IGenreDetailsRouteParams extends ng.route.IRouteParamsService {
        genreId: number;
    }

    class GenreDetailsController implements IGenreDetailsViewModel {
        public albums: Array<Models.IAlbum>;

        constructor($routeParams: IGenreDetailsRouteParams, genreApi: GenreApi.IGenreApiService) {
            var viewModel = this;

            genreApi.getGenreAlbums($routeParams.genreId).success(result => {
                viewModel.albums = result;
            });
        }
    }
}