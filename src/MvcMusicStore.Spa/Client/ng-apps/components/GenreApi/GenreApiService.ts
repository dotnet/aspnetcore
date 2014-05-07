module MusicStore.GenreApi {
    export interface IGenreApiService {
        getGenresLookup(): ng.IPromise<Array<Models.IGenreLookup>>;
        getGenresMenu(): ng.IPromise<Array<Models.IGenre>>;
        getGenresList(): ng.IHttpPromise<Array<Models.IGenre>>;
        getGenreAlbums(genreId: number): ng.IHttpPromise<Array<Models.IAlbum>>;
    }

    class GenreApiService implements IGenreApiService {
        private _inlineData: ng.ICacheObject;
        private _q: ng.IQService;
        private _http: ng.IHttpService;
        private _urlResolver: UrlResolver.IUrlResolverService;

        constructor($cacheFactory: ng.ICacheFactoryService,
                    $q: ng.IQService,
                    $http: ng.IHttpService,
                    urlResolver: UrlResolver.IUrlResolverService) {
            this._inlineData = $cacheFactory.get("inlineData");
            this._q = $q;
            this._http = $http;
            this._urlResolver = urlResolver;
        }

        public getGenresLookup() {
            var url = this._urlResolver.resolveUrl("~/api/genres/lookup"),
                inlineData = this._inlineData ? this._inlineData.get(url) : null;

            if (inlineData) {
                return this._q.when(inlineData);
            } else {
                return this._http.get(url).then(result => result.data);
            }
        }

        public getGenresMenu() {
            var url = this._urlResolver.resolveUrl("~/api/genres/menu"),
                inlineData = this._inlineData ? this._inlineData.get(url) : null;

            if (inlineData) {
                return this._q.when(inlineData);
            } else {
                return this._http.get(url).then(result => result.data);
            }
        }

        public getGenresList() {
            var url = this._urlResolver.resolveUrl("~/api/genres");
            return this._http.get(url);
        }

        public getGenreAlbums(genreId: number) {
            var url = this._urlResolver.resolveUrl("~/api/genres/" + genreId + "/albums");
            return this._http.get(url);
        }
    }
}