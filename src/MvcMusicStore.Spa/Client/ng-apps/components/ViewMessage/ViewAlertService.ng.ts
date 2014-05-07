/// <reference path="..\..\MusicStore.ViewAlert.ng.ts" />

module MusicStore.ViewAlert {
    export interface IViewAlertService {
        alert: Models.IAlert;
    }

    class ViewAlertService implements IViewAlertService {
        public alert: Models.IAlert;
    }
    
    angular.module("MusicStore.ViewAlert")
        .service("MusicStore.ViewAlert.IViewAlertService", [
            ViewAlertService
        ]);
} 