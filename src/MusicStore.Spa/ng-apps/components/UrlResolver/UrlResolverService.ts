module MusicStore.UrlResolver {
    export interface IUrlResolverService {
        base: string;
        resolveUrl(relativeUrl: string);
    }

    class UrlResolverService implements IUrlResolverService {
        private _base: string;

        constructor($rootElement: ng.IAugmentedJQuery) {
            this._base = $rootElement.attr("data-url-base");

            // Add trailing slash if not present
            if (this._base === "" || this._base.substr(this._base.length - 1) !== "/") {
                this._base = this._base + "/";
            }
        }

        public get base() {
            return this._base;
        }

        public resolveUrl(relativeUrl: string) {
            var firstChar = relativeUrl.substr(0, 1);

            if (firstChar === "~") {
                relativeUrl = relativeUrl.substr(1);
            }

            firstChar = relativeUrl.substr(0, 1);

            if (firstChar === "/") {
                relativeUrl = relativeUrl.substr(1);
            }

            return this._base + relativeUrl;
        }
    }
}