module MusicStore.LoginLink {
    interface LoginLinkAttributes extends ng.IAttributes {
        href: string;
    }

    //@NgDirective('appLoginLink')
    class LoginLinkDirective implements ng.IDirective {
        private _window: ng.IWindowService;

        constructor(urlResolver: UrlResolver.IUrlResolverService, $window: ng.IWindowService) {
            this._window = $window;
        }

        public restrict = "A";

        public link(scope: ng.IScope, element: ng.IAugmentedJQuery, attrs: LoginLinkAttributes) {
            if (!element.is("a[href]")) {
                return;
            }

            // Grab the original login URL
            var loginUrl = attrs.href;

            element.click(event => {
                // Update the returnUrl querystring value to current path
                var currentUrl = this._window.location.pathname + this._window.location.search + this._window.location.hash,
                    newUrl = loginUrl + "?returnUrl=" + encodeURIComponent(currentUrl);

                element.prop("href", newUrl);
            });
        }
    }
}