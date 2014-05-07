/// <reference path="..\..\MusicStore.Visited.ng.ts" />

module MusicStore.Visited {
    interface IVisitedFormController extends ng.IFormController {
        focus?: boolean;
        visited?: boolean;
    }

    //@NgDirective('input')
    //@NgDirective('select')
    class VisitedDirective implements ng.IDirective {
        private _window: ng.IWindowService;
        
        constructor($window: ng.IWindowService) {
            for (var m in this) {
                if (this[m].bind) {
                    this[m] = this[m].bind(this);
                }
            }
            this._window = $window;
        }

        public restrict = "E";

        public require = "?ngModel";

        public link(scope: ng.IScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes, ctrl: IVisitedFormController) {
            if (!ctrl) {
                return;
            }

            element.on("focus", event => {
                element.addClass("has-focus");
                scope.$apply(() => ctrl.focus = true);
            });

            element.on("blur", event => {
                element.removeClass("has-focus");
                element.addClass("has-visited");
                scope.$apply(() => {
                    ctrl.focus = false;
                    ctrl.visited = true;
                });
            });

            element.closest("form").on("submit", function () {
                element.addClass("has-visited");

                scope.$apply(() => {
                    ctrl.focus = false;
                    ctrl.visited = true;
                });
            });
        }
    }
    
    angular.module("MusicStore.Visited")
        .directive("input", [
            "$window",
            function (a) {
                return new VisitedDirective(a);
            }
        ])
        .directive("select", [
            "$window",
            function (a) {
                return new VisitedDirective(a);
            }
        ]);
}