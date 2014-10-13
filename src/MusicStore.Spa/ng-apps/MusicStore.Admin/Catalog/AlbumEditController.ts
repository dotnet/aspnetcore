module MusicStore.Admin.Catalog {
    interface IAlbumDetailsRouteParams extends ng.route.IRouteParamsService {
        mode: string;
        albumId: number;
    }

    interface IAlbumDetailsViewModel {
        mode: string; // edit or new
        disabled: boolean;
        album: Models.IAlbum;
        alert: Models.IAlert;
        artists: Array<Models.IArtist>;
        genres: Array<Models.IGenreLookup>;
        save();
        clearAlert();
    }

    class AlbumEditController implements IAlbumDetailsViewModel {
        private _albumApi: AlbumApi.IAlbumApiService;
        private _artistApi: ArtistApi.IArtistApiService;
        private _genreApi: GenreApi.IGenreApiService;
        private _viewAlert: ViewAlert.IViewAlertService;
        private _modal: ng.ui.bootstrap.IModalService;
        private _location: ng.ILocationService;
        private _timeout: ng.ITimeoutService;
        private _log: ng.ILogService;

        constructor($routeParams: IAlbumDetailsRouteParams,
                    albumApi: AlbumApi.IAlbumApiService,
                    artistApi: ArtistApi.IArtistApiService,
                    genreApi: GenreApi.IGenreApiService,
                    viewAlert: ViewAlert.IViewAlertService,
                    $modal: ng.ui.bootstrap.IModalService,
                    $location: ng.ILocationService,
                    $timeout: ng.ITimeoutService,
                    $q: ng.IQService,
                    $log: ng.ILogService) {

            this._albumApi = albumApi;
            this._artistApi = artistApi;
            this._genreApi = genreApi;
            this._viewAlert = viewAlert;
            this._modal = $modal;
            this._location = $location;
            this._timeout = $timeout;
            this._log = $log;

            this.mode = $routeParams.mode;

            this.alert = viewAlert.alert;

            artistApi.getArtistsLookup().then(artists => this.artists = artists);
            genreApi.getGenresLookup().then(genres => this.genres = genres);

            if (this.mode.toLowerCase() === "edit") {
                // TODO: Handle album load failure
                albumApi.getAlbumDetails($routeParams.albumId).then(album => {
                    this.album = album;

                    // Pre-load the lookup arrays with the current values if not set yet
                    this.genres = this.genres || [album.Genre];
                    this.artists = this.artists || [album.Artist];

                    this.disabled = false;
                });
            } else {
                this.disabled = false;
            }
        }

        public mode: string;

        public disabled = true;

        public album: Models.IAlbum;

        public alert: Models.IAlert;

        public artists: Array<Models.IArtist>;

        public genres: Array<Models.IGenreLookup>;

        public save() {
            this.disabled = true;

            var apiMethod = this.mode.toLowerCase() === "edit" ? this._albumApi.updateAlbum : this._albumApi.createAlbum;
            apiMethod = apiMethod.bind(this._albumApi);

            apiMethod(this.album).then(
                // Success
                response => {
                    var alert = {
                        type: Models.AlertType.success,
                        message: response.data.Message
                    };

                    // TODO: Do we need to destroy this timeout on controller unload?
                    this._timeout(() => this.alert !== alert || this.clearAlert(), 3000);

                    if (this.mode.toLowerCase() === "new") {
                        this._log.info("Created album successfully!");

                        var albumId: number = response.data.Data;

                        this._viewAlert.alert = alert;

                        // Reload the view with the new album ID
                        this._location.path("/albums/" + albumId + "/edit").replace();
                    } else {
                        this.alert = alert;
                        this.disabled = false;
                        this._log.info("Updated album " + this.album.AlbumId + " successfully!");
                    }
                },
                // Error
                response => {
                    // TODO: Make this common logic, e.g. base controller class, injected helper service, etc.
                    if (response.status === 400) {
                        // We made a bad request
                        if (response.data && response.data.ModelErrors) {
                            // The server says the update failed validation
                            // TODO: Map errors back to client validators and/or summary
                            this.alert = {
                                type: Models.AlertType.danger,
                                message: response.data.Message,
                                modelErrors: response.data.ModelErrors
                            };
                            this.disabled = false;
                        } else {
                            // Some other bad request, just show the message
                            this.alert = {
                                type: Models.AlertType.danger,
                                message: response.data.Message
                            };
                        }
                    } else if (response.status === 404) {
                        // The album wasn't found, probably deleted. Leave the form disabled and show error message.
                        this.alert = {
                            type: Models.AlertType.danger,
                            message: response.data.Message
                        };
                    } else if (response.status === 401) {
                        // We need to authenticate again
                        // TODO: Should we just redirect to login page, show a message with a link, or something else
                        this.alert = {
                            type: Models.AlertType.danger,
                            message: "Your session has timed out. Please log in and try again."
                        };
                    } else if (!response.status) {
                        // Request timed out or no response from server or worse
                        this._log.error("Error updating album " + this.album.AlbumId);
                        this._log.error(response);
                        this.alert = { type: Models.AlertType.danger, message: "An unexpected error occurred. Please try again." };
                        this.disabled = false;
                    }
                });
        }

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

        public clearAlert() {
            this.alert = null;
        }
    }
} 