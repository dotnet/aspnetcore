module MusicStore.Models {
    export interface IUserDetails {
        isAuthenticated: boolean;
        userName: string;
        userId: string;
        roles: Array<string>;
    }
} 