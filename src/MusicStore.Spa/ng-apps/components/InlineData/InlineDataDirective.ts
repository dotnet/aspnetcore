module MusicStore.InlineData {
    interface InlineDataAttributes extends ng.IAttributes {
        type: string;
        for: string;
    }

    //@NgDirective('appInlineData')
    class InlineDataDirective implements ng.IDirective {
        private _cache: ng.ICacheObject;
        private _log: ng.ILogService;

        constructor($cacheFactory: ng.ICacheFactoryService, $log: ng.ILogService) {
            this._cache = $cacheFactory.get("inlineData") || $cacheFactory("inlineData");
            this._log = $log;
        }

        public restrict = "A";

        public link(scope: ng.IScope, element: ng.IAugmentedJQuery, attrs: InlineDataAttributes) {
            var data = attrs.type === "application/json"
                ? angular.fromJson(element.text())
                : element.text();

            this._log.info("appInlineData: Inline data element found for " + attrs.for);

            this._cache.put(attrs.for, data);

            //element.remove();
        }
    }
} 