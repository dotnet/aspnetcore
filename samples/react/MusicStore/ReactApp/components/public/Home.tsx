import * as React from 'react';
import { Link } from 'react-router';
import { provide } from 'redux-typed';
import { ApplicationState }  from '../../store';
import { actionCreators } from '../../store/FeaturedAlbums';
import { AlbumTile } from './AlbumTile';

class Home extends React.Component<HomeProps, void> {
    componentWillMount() {
        this.props.requestFeaturedAlbums();
    }

    public render() {
        return <div>
            <div className="jumbotron">
                <h1>MVC Music Store</h1>
                <img src="/Images/home-showcase.png" />
            </div>
            <ul className="row list-unstyled" id="album-list">
                {this.props.albums.map(album =>
                    <AlbumTile key={ album.AlbumId } album={ album } />
                )}
            </ul>
        </div>;
    }
}

// Selects which part of global state maps to this component, and defines a type for the resulting props
const provider = provide(
    (state: ApplicationState) => state.featuredAlbums,
    actionCreators
);
type HomeProps = typeof provider.allProps;
export default provider.connect(Home);
