module MusicStore.Admin.Catalog {
    export interface IAlbumDeleteModalViewModel {
        album: Models.IAlbum;
        ok();
        cancel();
    }

    // We don't register this controller with Angular's DI system because the $modal service
    // will create and resolve its dependencies directly

    //@NgController(skip=true)
    export class AlbumDeleteModalController implements IAlbumDeleteModalViewModel {
        private _modalInstance: ng.ui.bootstrap.IModalServiceInstance;

        constructor($modalInstance: ng.ui.bootstrap.IModalServiceInstance, album: Models.IAlbum) {
            this._modalInstance = $modalInstance;
            this.album = album;
        }

        public album: Models.IAlbum;

        public ok() {
            this._modalInstance.close(true);
        }

        public cancel() {
            this._modalInstance.dismiss("cancel");
        }
    }
}