var MusicStore;
(function (MusicStore) {
    var AlbumApi;
    (function (AlbumApi) {
        angular.module("MusicStore.AlbumApi", []);
    })(AlbumApi = MusicStore.AlbumApi || (MusicStore.AlbumApi = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.AlbumApi.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var AlbumApi;
    (function (AlbumApi) {
        var AlbumApiService = (function () {
            function AlbumApiService($cacheFactory, $q, $http, urlResolver) {
                this._inlineData = $cacheFactory.get("inlineData");
                this._q = $q;
                this._http = $http;
                this._urlResolver = urlResolver;
            }
            AlbumApiService.prototype.getAlbums = function (page, pageSize, sortBy) {
                var url = this._urlResolver.resolveUrl("~/api/albums"), query = {}, querySeparator = "?", inlineData;
                if (page) {
                    query.page = page;
                }
                if (pageSize) {
                    query.pageSize = pageSize;
                }
                if (sortBy) {
                    query.sortBy = sortBy;
                }
                for (var key in query) {
                    if (query.hasOwnProperty(key)) {
                        url += querySeparator + key + "=" + encodeURIComponent(query[key]);
                        if (querySeparator === "?") {
                            querySeparator = "&";
                        }
                    }
                }
                inlineData = this._inlineData ? this._inlineData.get(url) : null;
                if (inlineData) {
                    return this._q.when(inlineData);
                }
                else {
                    return this._http.get(url).then(function (result) { return result.data; });
                }
            };
            AlbumApiService.prototype.getAlbumDetails = function (albumId) {
                var url = this._urlResolver.resolveUrl("~/api/albums/" + albumId);
                return this._http.get(url).then(function (result) { return result.data; });
            };
            AlbumApiService.prototype.getMostPopularAlbums = function (count) {
                var url = this._urlResolver.resolveUrl("~/api/albums/mostPopular"), inlineData = this._inlineData ? this._inlineData.get(url) : null;
                if (inlineData) {
                    return this._q.when(inlineData);
                }
                else {
                    if (count && count > 0) {
                        url += "?count=" + count;
                    }
                    return this._http.get(url).then(function (result) { return result.data; });
                }
            };
            AlbumApiService.prototype.createAlbum = function (album, config) {
                var url = this._urlResolver.resolveUrl("api/albums");
                return this._http.post(url, album, config || { timeout: 10000 });
            };
            AlbumApiService.prototype.updateAlbum = function (album, config) {
                var url = this._urlResolver.resolveUrl("api/albums/" + album.AlbumId + "/update");
                return this._http.put(url, album, config || { timeout: 10000 });
            };
            AlbumApiService.prototype.deleteAlbum = function (albumId, config) {
                var url = this._urlResolver.resolveUrl("api/albums/" + albumId);
                return this._http.delete(url, config || { timeout: 10000 });
            };
            return AlbumApiService;
        })();
        angular.module("MusicStore.AlbumApi")
            .service("MusicStore.AlbumApi.IAlbumApiService", [
            "$cacheFactory",
            "$q",
            "$http",
            "MusicStore.UrlResolver.IUrlResolverService",
            AlbumApiService
        ]);
    })(AlbumApi = MusicStore.AlbumApi || (MusicStore.AlbumApi = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var ArtistApi;
    (function (ArtistApi) {
        angular.module("MusicStore.ArtistApi", []);
    })(ArtistApi = MusicStore.ArtistApi || (MusicStore.ArtistApi = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.ArtistApi.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var ArtistApi;
    (function (ArtistApi) {
        var ArtistsApiService = (function () {
            function ArtistsApiService($cacheFactory, $q, $http, urlResolver) {
                this._inlineData = $cacheFactory.get("inlineData");
                this._q = $q;
                this._http = $http;
                this._urlResolver = urlResolver;
            }
            ArtistsApiService.prototype.getArtistsLookup = function () {
                var url = this._urlResolver.resolveUrl("~/api/artists/lookup"), inlineData = this._inlineData ? this._inlineData.get(url) : null;
                if (inlineData) {
                    return this._q.when(inlineData);
                }
                else {
                    return this._http.get(url).then(function (result) { return result.data; });
                }
            };
            return ArtistsApiService;
        })();
        angular.module("MusicStore.ArtistApi")
            .service("MusicStore.ArtistApi.IArtistApiService", [
            "$cacheFactory",
            "$q",
            "$http",
            "MusicStore.UrlResolver.IUrlResolverService",
            ArtistsApiService
        ]);
    })(ArtistApi = MusicStore.ArtistApi || (MusicStore.ArtistApi = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var GenreApi;
    (function (GenreApi) {
        angular.module("MusicStore.GenreApi", []);
    })(GenreApi = MusicStore.GenreApi || (MusicStore.GenreApi = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.GenreApi.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var GenreApi;
    (function (GenreApi) {
        var GenreApiService = (function () {
            function GenreApiService($cacheFactory, $q, $http, urlResolver) {
                this._inlineData = $cacheFactory.get("inlineData");
                this._q = $q;
                this._http = $http;
                this._urlResolver = urlResolver;
            }
            GenreApiService.prototype.getGenresLookup = function () {
                var url = this._urlResolver.resolveUrl("~/api/genres/lookup"), inlineData = this._inlineData ? this._inlineData.get(url) : null;
                if (inlineData) {
                    return this._q.when(inlineData);
                }
                else {
                    return this._http.get(url).then(function (result) { return result.data; });
                }
            };
            GenreApiService.prototype.getGenresMenu = function () {
                var url = this._urlResolver.resolveUrl("~/api/genres/menu"), inlineData = this._inlineData ? this._inlineData.get(url) : null;
                if (inlineData) {
                    return this._q.when(inlineData);
                }
                else {
                    return this._http.get(url).then(function (result) { return result.data; });
                }
            };
            GenreApiService.prototype.getGenresList = function () {
                var url = this._urlResolver.resolveUrl("~/api/genres");
                return this._http.get(url);
            };
            GenreApiService.prototype.getGenreAlbums = function (genreId) {
                var url = this._urlResolver.resolveUrl("~/api/genres/" + genreId + "/albums");
                return this._http.get(url);
            };
            return GenreApiService;
        })();
        angular.module("MusicStore.GenreApi")
            .service("MusicStore.GenreApi.IGenreApiService", [
            "$cacheFactory",
            "$q",
            "$http",
            "MusicStore.UrlResolver.IUrlResolverService",
            GenreApiService
        ]);
    })(GenreApi = MusicStore.GenreApi || (MusicStore.GenreApi = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var GenreMenu;
    (function (GenreMenu) {
        angular.module("MusicStore.GenreMenu", []);
    })(GenreMenu = MusicStore.GenreMenu || (MusicStore.GenreMenu = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.GenreMenu.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var GenreMenu;
    (function (GenreMenu) {
        var GenreMenuController = (function () {
            function GenreMenuController(genreApi, urlResolver) {
                var viewModel = this;
                genreApi.getGenresMenu().then(function (genres) {
                    viewModel.genres = genres;
                });
                viewModel.urlBase = urlResolver.base;
            }
            return GenreMenuController;
        })();
        angular.module("MusicStore.GenreMenu")
            .controller("MusicStore.GenreMenu.GenreMenuController", [
            "MusicStore.GenreApi.IGenreApiService",
            "MusicStore.UrlResolver.IUrlResolverService",
            GenreMenuController
        ]);
    })(GenreMenu = MusicStore.GenreMenu || (MusicStore.GenreMenu = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.GenreMenu.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var GenreMenu;
    (function (GenreMenu) {
        //@NgDirective('appGenreMenu')
        var GenreMenuDirective = (function () {
            function GenreMenuDirective(urlResolver) {
                this.replace = true;
                this.restrict = "A";
                for (var m in this) {
                    if (this[m].bind) {
                        this[m] = this[m].bind(this);
                    }
                }
                this.templateUrl = urlResolver.resolveUrl("~/ng-apps/components/GenreMenu/GenreMenu.html");
            }
            return GenreMenuDirective;
        })();
        angular.module("MusicStore.GenreMenu")
            .directive("appGenreMenu", [
            "MusicStore.UrlResolver.IUrlResolverService",
            function (a) {
                return new GenreMenuDirective(a);
            }
        ]);
    })(GenreMenu = MusicStore.GenreMenu || (MusicStore.GenreMenu = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var InlineData;
    (function (InlineData) {
        angular.module("MusicStore.InlineData", []);
    })(InlineData = MusicStore.InlineData || (MusicStore.InlineData = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.InlineData.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var InlineData;
    (function (InlineData) {
        //@NgDirective('appInlineData')
        var InlineDataDirective = (function () {
            function InlineDataDirective($cacheFactory, $log) {
                this.restrict = "A";
                for (var m in this) {
                    if (this[m].bind) {
                        this[m] = this[m].bind(this);
                    }
                }
                this._cache = $cacheFactory.get("inlineData") || $cacheFactory("inlineData");
                this._log = $log;
            }
            InlineDataDirective.prototype.link = function (scope, element, attrs) {
                var data = attrs.type === "application/json"
                    ? angular.fromJson(element.text())
                    : element.text();
                this._log.info("appInlineData: Inline data element found for " + attrs.for);
                this._cache.put(attrs.for, data);
                //element.remove();
            };
            return InlineDataDirective;
        })();
        angular.module("MusicStore.InlineData")
            .directive("appInlineData", [
            "$cacheFactory",
            "$log",
            function (a, b) {
                return new InlineDataDirective(a, b);
            }
        ]);
    })(InlineData = MusicStore.InlineData || (MusicStore.InlineData = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var LoginLink;
    (function (LoginLink) {
        angular.module("MusicStore.LoginLink", []);
    })(LoginLink = MusicStore.LoginLink || (MusicStore.LoginLink = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.LoginLink.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var LoginLink;
    (function (LoginLink) {
        //@NgDirective('appLoginLink')
        var LoginLinkDirective = (function () {
            function LoginLinkDirective(urlResolver, $window) {
                this.restrict = "A";
                for (var m in this) {
                    if (this[m].bind) {
                        this[m] = this[m].bind(this);
                    }
                }
                this._window = $window;
            }
            LoginLinkDirective.prototype.link = function (scope, element, attrs) {
                var _this = this;
                if (!element.is("a[href]")) {
                    return;
                }
                // Grab the original login URL
                var loginUrl = attrs.href;
                element.click(function (event) {
                    // Update the returnUrl querystring value to current path
                    var currentUrl = _this._window.location.pathname + _this._window.location.search + _this._window.location.hash, newUrl = loginUrl + "?returnUrl=" + encodeURIComponent(currentUrl);
                    element.prop("href", newUrl);
                });
            };
            return LoginLinkDirective;
        })();
        angular.module("MusicStore.LoginLink")
            .directive("appLoginLink", [
            "MusicStore.UrlResolver.IUrlResolverService",
            "$window",
            function (a, b) {
                return new LoginLinkDirective(a, b);
            }
        ]);
    })(LoginLink = MusicStore.LoginLink || (MusicStore.LoginLink = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var Models;
    (function (Models) {
        var AlertType = (function () {
            function AlertType(value) {
                this.value = value;
            }
            AlertType.prototype.toString = function () {
                return this.value;
            };
            // Values
            AlertType.success = new AlertType("success");
            AlertType.info = new AlertType("info");
            AlertType.warning = new AlertType("warning");
            AlertType.danger = new AlertType("danger");
            return AlertType;
        })();
        Models.AlertType = AlertType;
    })(Models = MusicStore.Models || (MusicStore.Models = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var PreventSubmit;
    (function (PreventSubmit) {
        angular.module("MusicStore.PreventSubmit", []);
    })(PreventSubmit = MusicStore.PreventSubmit || (MusicStore.PreventSubmit = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.PreventSubmit.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var PreventSubmit;
    (function (PreventSubmit) {
        //@NgDirective('appPreventSubmit')
        var PreventSubmitDirective = (function () {
            function PreventSubmitDirective() {
                this.restrict = "A";
                for (var m in this) {
                    if (this[m].bind) {
                        this[m] = this[m].bind(this);
                    }
                }
            }
            PreventSubmitDirective.prototype.link = function (scope, element, attrs) {
                // TODO: Just make this directive apply to all <form> tags and no-op if no action attr
                element.submit(function (e) {
                    if (scope.$eval(attrs.appPreventSubmit)) {
                        e.preventDefault();
                        return false;
                    }
                });
            };
            return PreventSubmitDirective;
        })();
        angular.module("MusicStore.PreventSubmit")
            .directive("appPreventSubmit", [
            function () {
                return new PreventSubmitDirective();
            }
        ]);
    })(PreventSubmit = MusicStore.PreventSubmit || (MusicStore.PreventSubmit = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var TitleCase;
    (function (TitleCase) {
        angular.module("MusicStore.TitleCase", []);
    })(TitleCase = MusicStore.TitleCase || (MusicStore.TitleCase = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.TitleCase.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var TitleCase;
    (function (TitleCase) {
        //@NgFilter('titlecase')
        function titleCase(input) {
            var out = "", lastChar = "";
            for (var i = 0; i < input.length; i++) {
                out = out + (lastChar === " " || lastChar === ""
                    ? input.charAt(i).toUpperCase()
                    : input.charAt(i));
                lastChar = input.charAt(i);
            }
            return out;
        }
        angular.module("MusicStore.TitleCase")
            .filter("titlecase", function () { return titleCase; });
    })(TitleCase = MusicStore.TitleCase || (MusicStore.TitleCase = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var Truncate;
    (function (Truncate) {
        angular.module("MusicStore.Truncate", []);
    })(Truncate = MusicStore.Truncate || (MusicStore.Truncate = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.Truncate.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var Truncate;
    (function (Truncate) {
        //@NgFilter
        function truncate(input, length) {
            if (!input) {
                return input;
            }
            if (input.length <= length) {
                return input;
            }
            else {
                return input.substr(0, length).trim() + "…";
            }
        }
        angular.module("MusicStore.Truncate")
            .filter("truncate", function () { return truncate; });
    })(Truncate = MusicStore.Truncate || (MusicStore.Truncate = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var UrlResolver;
    (function (UrlResolver) {
        angular.module("MusicStore.UrlResolver", []);
    })(UrlResolver = MusicStore.UrlResolver || (MusicStore.UrlResolver = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.UrlResolver.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var UrlResolver;
    (function (UrlResolver) {
        var UrlResolverService = (function () {
            function UrlResolverService($rootElement) {
                this._base = $rootElement.attr("data-url-base");
                // Add trailing slash if not present
                if (this._base === "" || this._base.substr(this._base.length - 1) !== "/") {
                    this._base = this._base + "/";
                }
            }
            Object.defineProperty(UrlResolverService.prototype, "base", {
                get: function () {
                    return this._base;
                },
                enumerable: true,
                configurable: true
            });
            UrlResolverService.prototype.resolveUrl = function (relativeUrl) {
                var firstChar = relativeUrl.substr(0, 1);
                if (firstChar === "~") {
                    relativeUrl = relativeUrl.substr(1);
                }
                firstChar = relativeUrl.substr(0, 1);
                if (firstChar === "/") {
                    relativeUrl = relativeUrl.substr(1);
                }
                return this._base + relativeUrl;
            };
            return UrlResolverService;
        })();
        angular.module("MusicStore.UrlResolver")
            .service("MusicStore.UrlResolver.IUrlResolverService", [
            "$rootElement",
            UrlResolverService
        ]);
    })(UrlResolver = MusicStore.UrlResolver || (MusicStore.UrlResolver = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var UserDetails;
    (function (UserDetails) {
        angular.module("MusicStore.UserDetails", []);
    })(UserDetails = MusicStore.UserDetails || (MusicStore.UserDetails = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.UserDetails.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var UserDetails;
    (function (UserDetails) {
        var UserDetailsService = (function () {
            function UserDetailsService($document) {
                this._document = $document;
            }
            UserDetailsService.prototype.getUserDetails = function (elementId) {
                if (elementId === void 0) { elementId = "userDetails"; }
                if (!this._userDetails) {
                    //var el = this._document.querySelector("[data-json-id='" + elementId + "']");
                    var el = this._document.find("#" + elementId + "[type='application/json']");
                    if (el.length) {
                        this._userDetails = angular.fromJson(el.text());
                    }
                    else {
                        this._userDetails = {
                            isAuthenticated: false,
                            userId: null,
                            userName: null,
                            roles: []
                        };
                    }
                }
                return this._userDetails;
            };
            return UserDetailsService;
        })();
        angular.module("MusicStore.UserDetails")
            .service("MusicStore.UserDetails.IUserDetailsService", [
            "$document",
            UserDetailsService
        ]);
    })(UserDetails = MusicStore.UserDetails || (MusicStore.UserDetails = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var ViewAlert;
    (function (ViewAlert) {
        angular.module("MusicStore.ViewAlert", []);
    })(ViewAlert = MusicStore.ViewAlert || (MusicStore.ViewAlert = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.ViewAlert.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var ViewAlert;
    (function (ViewAlert) {
        var ViewAlertService = (function () {
            function ViewAlertService() {
            }
            return ViewAlertService;
        })();
        angular.module("MusicStore.ViewAlert")
            .service("MusicStore.ViewAlert.IViewAlertService", [
            ViewAlertService
        ]);
    })(ViewAlert = MusicStore.ViewAlert || (MusicStore.ViewAlert = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var Visited;
    (function (Visited) {
        angular.module("MusicStore.Visited", []);
    })(Visited = MusicStore.Visited || (MusicStore.Visited = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.Visited.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var Visited;
    (function (Visited) {
        //@NgDirective('input')
        //@NgDirective('select')
        var VisitedDirective = (function () {
            function VisitedDirective($window) {
                this.restrict = "E";
                this.require = "?ngModel";
                for (var m in this) {
                    if (this[m].bind) {
                        this[m] = this[m].bind(this);
                    }
                }
                this._window = $window;
            }
            VisitedDirective.prototype.link = function (scope, element, attrs, ctrl) {
                if (!ctrl) {
                    return;
                }
                element.on("focus", function (event) {
                    element.addClass("has-focus");
                    scope.$apply(function () { return ctrl.focus = true; });
                });
                element.on("blur", function (event) {
                    element.removeClass("has-focus");
                    element.addClass("has-visited");
                    scope.$apply(function () {
                        ctrl.focus = false;
                        ctrl.visited = true;
                    });
                });
                element.closest("form").on("submit", function () {
                    element.addClass("has-visited");
                    scope.$apply(function () {
                        ctrl.focus = false;
                        ctrl.visited = true;
                    });
                });
            };
            return VisitedDirective;
        })();
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
    })(Visited = MusicStore.Visited || (MusicStore.Visited = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var Store;
    (function (Store) {
        var Catalog;
        (function (Catalog) {
            angular.module("MusicStore.Store.Catalog", []);
        })(Catalog = Store.Catalog || (Store.Catalog = {}));
    })(Store = MusicStore.Store || (MusicStore.Store = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.Store.Catalog.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var Store;
    (function (Store) {
        var Catalog;
        (function (Catalog) {
            var AlbumDetailsController = (function () {
                function AlbumDetailsController($routeParams, albumApi) {
                    var viewModel = this, albumId = $routeParams.albumId;
                    albumApi.getAlbumDetails(albumId).then(function (album) {
                        viewModel.album = album;
                    });
                }
                return AlbumDetailsController;
            })();
            angular.module("MusicStore.Store.Catalog")
                .controller("MusicStore.Store.Catalog.AlbumDetailsController", [
                "$routeParams",
                "MusicStore.AlbumApi.IAlbumApiService",
                AlbumDetailsController
            ]);
        })(Catalog = Store.Catalog || (Store.Catalog = {}));
    })(Store = MusicStore.Store || (MusicStore.Store = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.Store.Catalog.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var Store;
    (function (Store) {
        var Catalog;
        (function (Catalog) {
            var GenreDetailsController = (function () {
                function GenreDetailsController($routeParams, genreApi) {
                    var viewModel = this;
                    genreApi.getGenreAlbums($routeParams.genreId).success(function (result) {
                        viewModel.albums = result;
                    });
                }
                return GenreDetailsController;
            })();
            angular.module("MusicStore.Store.Catalog")
                .controller("MusicStore.Store.Catalog.GenreDetailsController", [
                "$routeParams",
                "MusicStore.GenreApi.IGenreApiService",
                GenreDetailsController
            ]);
        })(Catalog = Store.Catalog || (Store.Catalog = {}));
    })(Store = MusicStore.Store || (MusicStore.Store = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.Store.Catalog.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var Store;
    (function (Store) {
        var Catalog;
        (function (Catalog) {
            var GenreListController = (function () {
                function GenreListController(genreApi) {
                    var viewModel = this;
                    genreApi.getGenresList().success(function (genres) {
                        viewModel.genres = genres;
                    });
                }
                return GenreListController;
            })();
            angular.module("MusicStore.Store.Catalog")
                .controller("MusicStore.Store.Catalog.GenreListController", [
                "MusicStore.GenreApi.IGenreApiService",
                GenreListController
            ]);
        })(Catalog = Store.Catalog || (Store.Catalog = {}));
    })(Store = MusicStore.Store || (MusicStore.Store = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var Store;
    (function (Store) {
        var Home;
        (function (Home) {
            angular.module("MusicStore.Store.Home", []);
        })(Home = Store.Home || (Store.Home = {}));
    })(Store = MusicStore.Store || (MusicStore.Store = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.Store.Home.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var Store;
    (function (Store) {
        var Home;
        (function (Home) {
            var HomeController = (function () {
                function HomeController(albumApi) {
                    var viewModel = this;
                    albumApi.getMostPopularAlbums().then(function (albums) {
                        viewModel.albums = albums;
                    });
                }
                return HomeController;
            })();
            angular.module("MusicStore.Store.Home")
                .controller("MusicStore.Store.Home.HomeController", [
                "MusicStore.AlbumApi.IAlbumApiService",
                HomeController
            ]);
        })(Home = Store.Home || (Store.Home = {}));
    })(Store = MusicStore.Store || (MusicStore.Store = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="../bower_components/dt-angular/angular.d.ts" /> 
/// <reference path="../bower_components/dt-angular/angular-route.d.ts" />
/// <reference path="../bower_components/dt-angular-ui-bootstrap/angular-ui-bootstrap.d.ts" /> 
/// <reference path="../references.ts" />
var MusicStore;
(function (MusicStore) {
    var Store;
    (function (Store) {
        angular.module("MusicStore.Store", [
            "ngRoute",
            "MusicStore.InlineData",
            "MusicStore.PreventSubmit",
            "MusicStore.GenreMenu",
            "MusicStore.UrlResolver",
            "MusicStore.UserDetails",
            "MusicStore.LoginLink",
            "MusicStore.GenreApi",
            "MusicStore.AlbumApi",
            "MusicStore.Visited",
            "MusicStore.Store.Home",
            "MusicStore.Store.Catalog",
        ]).config([
            "$routeProvider",
            "$logProvider",
            configuration
        ]).run([
            "$log",
            "MusicStore.UserDetails.IUserDetailsService",
            run
        ]);
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
        function configuration($routeProvider, $logProvider) {
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
        function run($log, userDetails) {
            $log.log(userDetails.getUserDetails());
        }
    })(Store = MusicStore.Store || (MusicStore.Store = {}));
})(MusicStore || (MusicStore = {}));
