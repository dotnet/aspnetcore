/// <reference path="..\..\MusicStore.Store.Catalog.ng.ts" />

module MusicStore.Store.Catalog {
    interface IAlbumDetailsViewModel {
        album: Models.IAlbum;
    }

    interface IAlbumDetailsRouteParams extends ng.route.IRouteParamsService {
        albumId: number;
    }

    class AlbumDetailsController implements IAlbumDetailsViewModel {
        public album: Models.IAlbum;

        constructor($routeParams: IAlbumDetailsRouteParams, albumApi: AlbumApi.IAlbumApiService) {
            var viewModel = this,
                albumId = $routeParams.albumId;

            albumApi.getAlbumDetails(albumId).then(album => {
                viewModel.album = album;
            });
        }
    }
    
    angular.module("MusicStore.Store.Catalog")
        .controller("MusicStore.Store.Catalog.AlbumDetailsController", [
            "$routeParams",
            "MusicStore.AlbumApi.IAlbumApiService",
            AlbumDetailsController
        ]);
} 