module MusicStore.AlbumApi {
    export interface IAlbumApiService {
        getAlbums(page?: number, pageSize?: number, sortBy?: string): ng.IPromise<Models.IPagedList<Models.IAlbum>>;
        getAlbumDetails(albumId: number): ng.IPromise<Models.IAlbum>;
        getMostPopularAlbums(count?: number): ng.IPromise<Array<Models.IAlbum>>;
        createAlbum(album: Models.IAlbum, config?: ng.IRequestConfig): ng.IHttpPromise<Models.IApiResult>;
        updateAlbum(album: Models.IAlbum, config?: ng.IRequestConfig): ng.IHttpPromise<Models.IApiResult>;
        deleteAlbum(albumId: number, config?: ng.IRequestConfig): ng.IHttpPromise<Models.IApiResult>;
    }

    class AlbumApiService implements IAlbumApiService {
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

        public getAlbums(page?: number, pageSize?: number, sortBy?: string) {
            var url = this._urlResolver.resolveUrl("~/api/albums"),
                query: any = {},
                querySeparator = "?",
                inlineData;

            if (page) {
                query.page = page;
            }

            if (pageSize) {
                query.pageSize = pageSize;
            }

            if (sortBy) {
                query.sortBy = sortBy;
            }

            for (var key in query) {
                if (query.hasOwnProperty(key)) {
                    url += querySeparator + key + "=" + encodeURIComponent(query[key]);
                    if (querySeparator === "?") {
                        querySeparator = "&";
                    }
                }
            }

            inlineData = this._inlineData ? this._inlineData.get(url) : null;

            if (inlineData) {
                return this._q.when(inlineData);
            } else {
                return this._http.get(url).then(result => result.data);
            }
        }

        public getAlbumDetails(albumId: number) {
            var url = this._urlResolver.resolveUrl("~/api/albums/" + albumId);
            return this._http.get(url).then(result => result.data);
        }

        public getMostPopularAlbums(count?: number) {
            var url = this._urlResolver.resolveUrl("~/api/albums/mostPopular"),
                inlineData = this._inlineData ? this._inlineData.get(url) : null;

            if (inlineData) {
                return this._q.when(inlineData);
            } else {
                if (count && count > 0) {
                    url += "?count=" + count;
                }

                return this._http.get(url).then(result => result.data);
            }
        }

        public createAlbum(album: Models.IAlbum, config?: ng.IRequestConfig) {
            var url = this._urlResolver.resolveUrl("api/albums");
            return this._http.post(url, album, config || { timeout: 10000 });
        }

        public updateAlbum(album: Models.IAlbum, config?: ng.IRequestConfig) {
            var url = this._urlResolver.resolveUrl("api/albums/" + album.AlbumId + "/update");
            return this._http.put(url, album, config || { timeout: 10000 });
        }

        public deleteAlbum(albumId: number, config?: ng.IRequestConfig) {
            var url = this._urlResolver.resolveUrl("api/albums/" + albumId);
            return this._http.delete(url, config || { timeout: 10000 });
        }
    }
} 