/// <reference path="../references.ts" />

module MusicStore.Store {

    var dependencies = [
        "ngRoute",
        MusicStore.InlineData,
        MusicStore.PreventSubmit,
        MusicStore.GenreMenu,
        MusicStore.UrlResolver,
        MusicStore.UserDetails,
        MusicStore.LoginLink,
        MusicStore.GenreApi,
        MusicStore.AlbumApi,
        MusicStore.Visited,
        MusicStore.Store.Home,
        MusicStore.Store.Catalog
    ];

    // Use this method to register work which needs to be performed on module loading.
    // Note only providers can be injected as dependencies here.
    function configuration($routeProvider: ng.route.IRouteProvider, $logProvider: ng.ILogProvider) {
        // TODO: Enable debug logging based on server config
        // TODO: Capture all logged errors and send back to server
        $logProvider.debugEnabled(true);

        $routeProvider
            .when("/", { templateUrl: "ng-apps/MusicStore.Store/Home/Home.html" })
            .when("/albums/genres", { templateUrl: "ng-apps/MusicStore.Store/Catalog/GenreList.html" })
            .when("/albums/genres/:genreId", { templateUrl: "ng-apps/MusicStore.Store/Catalog/GenreDetails.html" })
            .when("/albums/:albumId", { templateUrl: "ng-apps/MusicStore.Store/Catalog/AlbumDetails.html" })
            .otherwise({ redirectTo: "/" });
    }

    // Use this method to register work which should be performed when the injector is done loading all modules.
    function run($log: ng.ILogService, userDetails: UserDetails.IUserDetailsService) {
        $log.log(userDetails.getUserDetails());
    }
}