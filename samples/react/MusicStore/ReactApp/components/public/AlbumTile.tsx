import * as React from 'react';
import { Link } from 'react-router';
import { Album } from '../../store/FeaturedAlbums';

export class AlbumTile extends React.Component<{ album: Album, key?: any }, void> {
    public render() {
        const { album } = this.props;
        return (
            <li className="col-lg-2 col-md-2 col-sm-2 col-xs-4 container">
                <Link to={ '/album/' + album.AlbumId }>
                    <img alt={ album.Title } src={ album.AlbumArtUrl } />
                    <h4>{ album.Title }</h4>
                </Link>
            </li>
        );
    }
}
