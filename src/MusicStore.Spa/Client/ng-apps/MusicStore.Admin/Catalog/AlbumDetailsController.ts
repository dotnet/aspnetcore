module MusicStore.Admin.Catalog {
    interface IAlbumDetailsRouteParams extends ng.route.IRouteParamsService {
        albumId: number;
    }

    interface IAlbumDetailsViewModel {
        album: Models.IAlbum;
        deleteAlbum();
    }

    class AlbumDetailsController implements IAlbumDetailsViewModel {
        private _modal: ng.ui.bootstrap.IModalService;
        private _location: ng.ILocationService;
        private _albumApi: AlbumApi.IAlbumApiService;
        private _viewAlert: ViewAlert.IViewAlertService;

        constructor($routeParams: IAlbumDetailsRouteParams,
                    $modal: ng.ui.bootstrap.IModalService,
                    $location: ng.ILocationService,
                    albumApi: AlbumApi.IAlbumApiService,
                    viewAlert: ViewAlert.IViewAlertService) {

            this._modal = $modal;
            this._location = $location;
            this._albumApi = albumApi;
            this._viewAlert = viewAlert;

            albumApi.getAlbumDetails($routeParams.albumId).then(album => this.album = album);
        }

        public album: Models.IAlbum;

        public deleteAlbum() {
            var deleteModal = this._modal.open({
                templateUrl: "ng-apps/MusicStore.Admin/Catalog/AlbumDeleteModal.cshtml",
                controller: "MusicStore.Admin.Catalog.AlbumDeleteModalController as viewModel",
                resolve: {
                    album: () => this.album
                }
            });

            deleteModal.result.then(shouldDelete => {
                if (!shouldDelete) {
                    return;
                }

                this._albumApi.deleteAlbum(this.album.AlbumId).then(result => {
                    // Navigate back to the list
                    this._viewAlert.alert = {
                        type: Models.AlertType.success,
                        message: result.data.Message
                    };
                    this._location.path("/albums").replace();
                });
            });
        }
    }
}