/// <reference path="..\..\MusicStore.GenreMenu.ng.ts" />

module MusicStore.GenreMenu {
    interface IGenreMenuViewModel {
        genres: Array<Models.IGenre>;
        urlBase: string;
    }

    class GenreMenuController implements IGenreMenuViewModel {
        constructor(genreApi: GenreApi.IGenreApiService, urlResolver: UrlResolver.IUrlResolverService) {
            var viewModel = this;

            genreApi.getGenresMenu().then(genres => {
                viewModel.genres = genres;
            });

            viewModel.urlBase = urlResolver.base;
        }

        public genres: Array<Models.IGenre>;

        public urlBase: string;
    }
    
    angular.module("MusicStore.GenreMenu")
        .controller("MusicStore.GenreMenu.GenreMenuController", [
            "MusicStore.GenreApi.IGenreApiService",
            "MusicStore.UrlResolver.IUrlResolverService",
            GenreMenuController
        ]);
}