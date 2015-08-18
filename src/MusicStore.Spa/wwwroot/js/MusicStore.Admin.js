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
    var Admin;
    (function (Admin) {
        var Catalog;
        (function (Catalog) {
            // We don't register this controller with Angular's DI system because the $modal service
            // will create and resolve its dependencies directly
            //@NgController(skip=true)
            var AlbumDeleteModalController = (function () {
                function AlbumDeleteModalController($modalInstance, album) {
                    this._modalInstance = $modalInstance;
                    this.album = album;
                }
                AlbumDeleteModalController.prototype.ok = function () {
                    this._modalInstance.close(true);
                };
                AlbumDeleteModalController.prototype.cancel = function () {
                    this._modalInstance.dismiss("cancel");
                };
                return AlbumDeleteModalController;
            })();
            Catalog.AlbumDeleteModalController = AlbumDeleteModalController;
        })(Catalog = Admin.Catalog || (Admin.Catalog = {}));
    })(Admin = MusicStore.Admin || (MusicStore.Admin = {}));
})(MusicStore || (MusicStore = {}));
var MusicStore;
(function (MusicStore) {
    var Admin;
    (function (Admin) {
        var Catalog;
        (function (Catalog) {
            angular.module("MusicStore.Admin.Catalog", []);
        })(Catalog = Admin.Catalog || (Admin.Catalog = {}));
    })(Admin = MusicStore.Admin || (MusicStore.Admin = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.Admin.Catalog.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var Admin;
    (function (Admin) {
        var Catalog;
        (function (Catalog) {
            var AlbumDetailsController = (function () {
                function AlbumDetailsController($routeParams, $modal, $location, albumApi, viewAlert) {
                    var _this = this;
                    this._modal = $modal;
                    this._location = $location;
                    this._albumApi = albumApi;
                    this._viewAlert = viewAlert;
                    albumApi.getAlbumDetails($routeParams.albumId).then(function (album) { return _this.album = album; });
                }
                AlbumDetailsController.prototype.deleteAlbum = function () {
                    var _this = this;
                    var deleteModal = this._modal.open({
                        templateUrl: "ng-apps/MusicStore.Admin/Catalog/AlbumDeleteModal.cshtml",
                        controller: "MusicStore.Admin.Catalog.AlbumDeleteModalController as viewModel",
                        resolve: {
                            album: function () { return _this.album; }
                        }
                    });
                    deleteModal.result.then(function (shouldDelete) {
                        if (!shouldDelete) {
                            return;
                        }
                        _this._albumApi.deleteAlbum(_this.album.AlbumId).then(function (result) {
                            // Navigate back to the list
                            _this._viewAlert.alert = {
                                type: MusicStore.Models.AlertType.success,
                                message: result.data.Message
                            };
                            _this._location.path("/albums").replace();
                        });
                    });
                };
                return AlbumDetailsController;
            })();
            angular.module("MusicStore.Admin.Catalog")
                .controller("MusicStore.Admin.Catalog.AlbumDetailsController", [
                "$routeParams",
                "$modal",
                "$location",
                "MusicStore.AlbumApi.IAlbumApiService",
                "MusicStore.ViewAlert.IViewAlertService",
                AlbumDetailsController
            ]);
        })(Catalog = Admin.Catalog || (Admin.Catalog = {}));
    })(Admin = MusicStore.Admin || (MusicStore.Admin = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.Admin.Catalog.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var Admin;
    (function (Admin) {
        var Catalog;
        (function (Catalog) {
            var AlbumEditController = (function () {
                function AlbumEditController($routeParams, albumApi, artistApi, genreApi, viewAlert, $modal, $location, $timeout, $q, $log) {
                    var _this = this;
                    this.disabled = true;
                    this._albumApi = albumApi;
                    this._artistApi = artistApi;
                    this._genreApi = genreApi;
                    this._viewAlert = viewAlert;
                    this._modal = $modal;
                    this._location = $location;
                    this._timeout = $timeout;
                    this._log = $log;
                    this.mode = $routeParams.mode;
                    this.alert = viewAlert.alert;
                    artistApi.getArtistsLookup().then(function (artists) { return _this.artists = artists; });
                    genreApi.getGenresLookup().then(function (genres) { return _this.genres = genres; });
                    if (this.mode.toLowerCase() === "edit") {
                        // TODO: Handle album load failure
                        albumApi.getAlbumDetails($routeParams.albumId).then(function (album) {
                            _this.album = album;
                            // Pre-load the lookup arrays with the current values if not set yet
                            _this.genres = _this.genres || [album.Genre];
                            _this.artists = _this.artists || [album.Artist];
                            _this.disabled = false;
                        });
                    }
                    else {
                        this.disabled = false;
                    }
                }
                AlbumEditController.prototype.save = function () {
                    var _this = this;
                    this.disabled = true;
                    var apiMethod = this.mode.toLowerCase() === "edit" ? this._albumApi.updateAlbum : this._albumApi.createAlbum;
                    apiMethod = apiMethod.bind(this._albumApi);
                    apiMethod(this.album).then(
                    // Success
                    function (response) {
                        var alert = {
                            type: MusicStore.Models.AlertType.success,
                            message: response.data.Message
                        };
                        // TODO: Do we need to destroy this timeout on controller unload?
                        _this._timeout(function () { return _this.alert !== alert || _this.clearAlert(); }, 3000);
                        if (_this.mode.toLowerCase() === "new") {
                            _this._log.info("Created album successfully!");
                            var albumId = response.data.Data;
                            _this._viewAlert.alert = alert;
                            // Reload the view with the new album ID
                            _this._location.path("/albums/" + albumId + "/edit").replace();
                        }
                        else {
                            _this.alert = alert;
                            _this.disabled = false;
                            _this._log.info("Updated album " + _this.album.AlbumId + " successfully!");
                        }
                    }, 
                    // Error
                    function (response) {
                        // TODO: Make this common logic, e.g. base controller class, injected helper service, etc.
                        if (response.status === 400) {
                            // We made a bad request
                            if (response.data && response.data.ModelErrors) {
                                // The server says the update failed validation
                                // TODO: Map errors back to client validators and/or summary
                                _this.alert = {
                                    type: MusicStore.Models.AlertType.danger,
                                    message: response.data.Message,
                                    modelErrors: response.data.ModelErrors
                                };
                                _this.disabled = false;
                            }
                            else {
                                // Some other bad request, just show the message
                                _this.alert = {
                                    type: MusicStore.Models.AlertType.danger,
                                    message: response.data.Message
                                };
                            }
                        }
                        else if (response.status === 404) {
                            // The album wasn't found, probably deleted. Leave the form disabled and show error message.
                            _this.alert = {
                                type: MusicStore.Models.AlertType.danger,
                                message: response.data.Message
                            };
                        }
                        else if (response.status === 401) {
                            // We need to authenticate again
                            // TODO: Should we just redirect to login page, show a message with a link, or something else
                            _this.alert = {
                                type: MusicStore.Models.AlertType.danger,
                                message: "Your session has timed out. Please log in and try again."
                            };
                        }
                        else if (!response.status) {
                            // Request timed out or no response from server or worse
                            _this._log.error("Error updating album " + _this.album.AlbumId);
                            _this._log.error(response);
                            _this.alert = { type: MusicStore.Models.AlertType.danger, message: "An unexpected error occurred. Please try again." };
                            _this.disabled = false;
                        }
                    });
                };
                AlbumEditController.prototype.deleteAlbum = function () {
                    var _this = this;
                    var deleteModal = this._modal.open({
                        templateUrl: "ng-apps/MusicStore.Admin/Catalog/AlbumDeleteModal.cshtml",
                        controller: "MusicStore.Admin.Catalog.AlbumDeleteModalController as viewModel",
                        resolve: {
                            album: function () { return _this.album; }
                        }
                    });
                    deleteModal.result.then(function (shouldDelete) {
                        if (!shouldDelete) {
                            return;
                        }
                        _this._albumApi.deleteAlbum(_this.album.AlbumId).then(function (result) {
                            // Navigate back to the list
                            _this._viewAlert.alert = {
                                type: MusicStore.Models.AlertType.success,
                                message: result.data.Message
                            };
                            _this._location.path("/albums").replace();
                        });
                    });
                };
                AlbumEditController.prototype.clearAlert = function () {
                    this.alert = null;
                };
                return AlbumEditController;
            })();
            angular.module("MusicStore.Admin.Catalog")
                .controller("MusicStore.Admin.Catalog.AlbumEditController", [
                "$routeParams",
                "MusicStore.AlbumApi.IAlbumApiService",
                "MusicStore.ArtistApi.IArtistApiService",
                "MusicStore.GenreApi.IGenreApiService",
                "MusicStore.ViewAlert.IViewAlertService",
                "$modal",
                "$location",
                "$timeout",
                "$q",
                "$log",
                AlbumEditController
            ]);
        })(Catalog = Admin.Catalog || (Admin.Catalog = {}));
    })(Admin = MusicStore.Admin || (MusicStore.Admin = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="..\..\MusicStore.Admin.Catalog.ng.ts" />
var MusicStore;
(function (MusicStore) {
    var Admin;
    (function (Admin) {
        var Catalog;
        (function (Catalog) {
            var AlbumListController = (function () {
                function AlbumListController(albumApi, viewAlert, $modal, $timeout, $log) {
                    this._albumApi = albumApi;
                    this._modal = $modal;
                    this._timeout = $timeout;
                    this._log = $log;
                    this.currentPage = 1;
                    this.pageSize = 50;
                    this.sortColumn = "Title";
                    this.loadPage(1);
                    this.showAlert(viewAlert.alert, 3000);
                    viewAlert.alert = null;
                }
                AlbumListController.prototype.loadPage = function (page) {
                    var _this = this;
                    page = page || this.currentPage;
                    var sortByExpression = this.getSortByExpression();
                    this._albumApi.getAlbums(page, this.pageSize, sortByExpression).then(function (result) {
                        _this.albums = result.Data;
                        _this.currentPage = result.Page;
                        _this.totalCount = result.TotalCount;
                    });
                };
                AlbumListController.prototype.sortBy = function (column) {
                    if (this.sortColumn === column) {
                        // Just flip the direction
                        this.sortDescending = !this.sortDescending;
                    }
                    else {
                        this.sortColumn = column;
                        this.sortDescending = false;
                    }
                    this.loadPage();
                };
                AlbumListController.prototype.deleteAlbum = function (album) {
                    var _this = this;
                    var deleteModal = this._modal.open({
                        templateUrl: "ng-apps/MusicStore.Admin/Catalog/AlbumDeleteModal.cshtml",
                        controller: "MusicStore.Admin.Catalog.AlbumDeleteModalController as viewModel",
                        resolve: {
                            album: function () { return album; }
                        }
                    });
                    deleteModal.result.then(function (shouldDelete) {
                        if (!shouldDelete) {
                            return;
                        }
                        _this._albumApi.deleteAlbum(album.AlbumId).then(function (result) {
                            _this.loadPage();
                            _this.showAlert({
                                type: MusicStore.Models.AlertType.success,
                                message: result.data.Message
                            }, 3000);
                        });
                    });
                };
                AlbumListController.prototype.clearAlert = function () {
                    this.alert = null;
                };
                AlbumListController.prototype.showAlert = function (alert, closeAfter) {
                    var _this = this;
                    if (!alert) {
                        return;
                    }
                    this.alert = alert;
                    // TODO: Do we need to destroy this timeout on controller unload?
                    if (closeAfter) {
                        this._timeout(function () { return _this.alert !== alert || _this.clearAlert(); }, closeAfter);
                    }
                };
                AlbumListController.prototype.getSortByExpression = function () {
                    if (this.sortDescending) {
                        return this.sortColumn + " DESC";
                    }
                    return this.sortColumn;
                };
                return AlbumListController;
            })();
            angular.module("MusicStore.Admin.Catalog")
                .controller("MusicStore.Admin.Catalog.AlbumListController", [
                "MusicStore.AlbumApi.IAlbumApiService",
                "MusicStore.ViewAlert.IViewAlertService",
                "$modal",
                "$timeout",
                "$log",
                AlbumListController
            ]);
        })(Catalog = Admin.Catalog || (Admin.Catalog = {}));
    })(Admin = MusicStore.Admin || (MusicStore.Admin = {}));
})(MusicStore || (MusicStore = {}));
/// <reference path="../bower_components/dt-angular/angular.d.ts" /> 
/// <reference path="../bower_components/dt-angular/angular-route.d.ts" />
/// <reference path="../bower_components/dt-angular-ui-bootstrap/angular-ui-bootstrap.d.ts" /> 
/// <reference path="../references.ts" />
var MusicStore;
(function (MusicStore) {
    var Admin;
    (function (Admin) {
        angular.module("MusicStore.Admin", [
            "ngRoute",
            "ui.bootstrap",
            "MusicStore.InlineData",
            "MusicStore.GenreMenu",
            "MusicStore.UrlResolver",
            "MusicStore.UserDetails",
            "MusicStore.LoginLink",
            "MusicStore.Visited",
            "MusicStore.TitleCase",
            "MusicStore.Truncate",
            "MusicStore.GenreApi",
            "MusicStore.AlbumApi",
            "MusicStore.ArtistApi",
            "MusicStore.ViewAlert",
            "MusicStore.Admin.Catalog",
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
        function configuration($routeProvider, $logProvider) {
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
        function run($log, userDetails) {
            $log.log(userDetails.getUserDetails());
        }
    })(Admin = MusicStore.Admin || (MusicStore.Admin = {}));
})(MusicStore || (MusicStore = {}));
