/// <reference path="..\..\MusicStore.PreventSubmit.ng.ts" />

module MusicStore.PreventSubmit {
    interface IPreventSubmitAttributes extends ng.IAttributes {
        name: string;
        appPreventSubmit: string;
    }

    //@NgDirective('appPreventSubmit')
    class PreventSubmitDirective implements ng.IDirective {
        constructor() {
            for (var m in this) {
                if (this[m].bind) {
                    this[m] = this[m].bind(this);
                }
            }
        }

        private _preventSubmit: any;

        public restrict = "A";

        public link(scope: any, element: ng.IAugmentedJQuery, attrs: IPreventSubmitAttributes) {
            // TODO: Just make this directive apply to all <form> tags and no-op if no action attr

            element.submit(e => {
                if (scope.$eval(attrs.appPreventSubmit)) {
                    e.preventDefault();
                    return false;
                }
            });
        }
    }
    
    angular.module("MusicStore.PreventSubmit")
        .directive("appPreventSubmit", [
            function () {
                return new PreventSubmitDirective();
            }
        ]);
}  