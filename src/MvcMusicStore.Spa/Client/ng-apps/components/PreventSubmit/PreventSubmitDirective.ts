module MusicStore.PreventSubmit {
    interface IPreventSubmitAttributes extends ng.IAttributes {
        name: string;
        appPreventSubmit: string;
    }

    //@NgDirective('appPreventSubmit')
    class PreventSubmitDirective implements ng.IDirective {
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
}  