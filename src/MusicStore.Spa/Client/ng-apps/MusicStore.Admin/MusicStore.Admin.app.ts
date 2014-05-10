/// <reference path="../references.ts" />

module MusicStore.Admin {

    var dependencies = [
        "ngRoute",
        "ui.bootstrap",
        MusicStore.InlineData,
        MusicStore.GenreMenu,
        MusicStore.UrlResolver,
        MusicStore.UserDetails,
        MusicStore.LoginLink,
        MusicStore.Visited,
        MusicStore.TitleCase,
        MusicStore.Truncate,
        MusicStore.GenreApi,
        MusicStore.AlbumApi,
        MusicStore.ArtistApi,
        MusicStore.ViewAlert,
        MusicStore.Admin.Catalog
    ];

    // Use this method to register work which needs to be performed on module loading.
    // Note only providers can be injected as dependencies here.
    function configuration($routeProvider: ng.route.IRouteProvider, $logProvider: ng.ILogProvider) {
        // TODO: Enable debug logging based on server config
        // TODO: Capture all logged errors and send back to server
        $logProvider.debugEnabled(true);

        // Configure routes
        $routeProvider
            .when("/albums/:albumId/details", { templateUrl: "ng-apps/MusicStore.Admin/Catalog/AlbumDetails.cshtml" })
            .when("/albums/:albumId/:mode", { templateUrl: "ng-apps/MusicStore.Admin/Catalog/AlbumEdit.cshtml" })
            .when("/albums/:mode", { templateUrl: "ng-apps/MusicStore.Admin/Catalog/AlbumEdit.cshtml" })
            .when("/albums", { templateUrl: "ng-apps/MusicStore.Admin/Catalog/AlbumList.cshtml" })
            .otherwise({ redirectTo: "/albums" });
    }

    // Use this method to register work which should be performed when the injector is done loading all modules.
    //function BUG:run() {

    //}
}