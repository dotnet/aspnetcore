/// <reference path="..\..\MusicStore.Store.Catalog.ng.ts" />

module MusicStore.Store.Catalog {
    interface IGenreListViewModel {
        genres: Array<Models.IGenre>;
    }

    class GenreListController implements IGenreListViewModel {
        public genres: Array<Models.IGenre>;

        constructor(genreApi: GenreApi.IGenreApiService) {
            var viewModel = this;

            genreApi.getGenresList().success(function (genres) {
                viewModel.genres = genres;
            });
        }
    }
    
    angular.module("MusicStore.Store.Catalog")
        .controller("MusicStore.Store.Catalog.GenreListController", [
            "MusicStore.GenreApi.IGenreApiService",
            GenreListController
        ]);
} 