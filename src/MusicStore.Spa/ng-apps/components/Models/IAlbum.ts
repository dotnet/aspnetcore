module MusicStore.Models {
    export interface IAlbum {
        AlbumId: number;
        GenreId: number;
        ArtistId: number;

        Title: string;
        AlbumArtUrl: string;
        Price: number;

        Artist: IArtist;
        Genre: IGenre;

        DetailsUrl: string;
    }
} 