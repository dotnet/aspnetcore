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
}