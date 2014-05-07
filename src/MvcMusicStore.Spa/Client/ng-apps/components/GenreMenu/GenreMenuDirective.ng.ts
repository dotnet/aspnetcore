/// <reference path="..\..\MusicStore.GenreMenu.ng.ts" />

module MusicStore.GenreMenu {

    //@NgDirective('appGenreMenu')
    class GenreMenuDirective implements ng.IDirective {
        public replace = true;
        public restrict = "A";
        public templateUrl;

        constructor(urlResolver: UrlResolver.IUrlResolverService) {
            for (var m in this) {
                if (this[m].bind) {
                    this[m] = this[m].bind(this);
                }
            }
            this.templateUrl = urlResolver.resolveUrl("~/ng-apps/components/GenreMenu/GenreMenu.html");
        }
    }
    
    angular.module("MusicStore.GenreMenu")
        .directive("appGenreMenu", [
            "MusicStore.UrlResolver.IUrlResolverService",
            function (a) {
                return new GenreMenuDirective(a);
            }
        ]);
}