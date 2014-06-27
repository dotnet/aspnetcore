module MusicStore.UserDetails {
    export interface IUserDetailsService {
        getUserDetails(): Models.IUserDetails;
        getUserDetails(elementId: string): Models.IUserDetails;
    }

    class UserDetailsService implements IUserDetailsService {
        private _document: ng.IDocumentService;
        private _userDetails: Models.IUserDetails;

        constructor($document: ng.IDocumentService) {
            this._document = $document;
        }

        public getUserDetails(elementId = "userDetails") {
            if (!this._userDetails) {
                //var el = this._document.querySelector("[data-json-id='" + elementId + "']");
                var el = this._document.find("#" + elementId + "[type='application/json']");

                if (el.length) {
                    this._userDetails = angular.fromJson(el.text());
                } else {
                    this._userDetails = {
                        isAuthenticated: false,
                        userId: null,
                        userName: null,
                        roles: []
                    };
                }
            }
            return this._userDetails;
        }
    }
} 