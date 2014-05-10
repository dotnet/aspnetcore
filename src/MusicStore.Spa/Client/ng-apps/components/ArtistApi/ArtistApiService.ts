module MusicStore.ArtistApi {
    export interface IArtistApiService {
        getArtistsLookup(): ng.IPromise<Array<Models.IArtist>>;
    }

    class ArtistsApiService implements IArtistApiService {
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

        public getArtistsLookup() {
            var url = this._urlResolver.resolveUrl("~/api/artists/lookup"),
                inlineData = this._inlineData ? this._inlineData.get(url) : null;

            if (inlineData) {
                return this._q.when(inlineData);
            } else {
                return this._http.get(url).then(result => result.data);
            }
        }
    }
}