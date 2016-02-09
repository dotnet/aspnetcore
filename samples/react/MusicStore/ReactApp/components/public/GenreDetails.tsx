import * as React from 'react';
import { Link } from 'react-router';
import { provide } from 'redux-typed';
import { ApplicationState }  from '../../store';
import * as GenreDetailsStore from '../../store/GenreDetails';
import { AlbumTile } from './AlbumTile';

interface RouteParams {
    genreId: number
}

class GenreDetails extends React.Component<GenreDetailsProps, void> {
    componentWillMount() {
        this.props.requestGenreDetails(this.props.params.genreId);
    }
    
    componentWillReceiveProps(nextProps: GenreDetailsProps) {
        if (nextProps.params.genreId !== this.props.params.genreId) {
            nextProps.requestGenreDetails(nextProps.params.genreId);
        }
    }

    public render() {
        if (this.props.isLoaded) {
            let albums = this.props.albums;
            return <div>
                <h3>Albums</h3>

                <ul className="list-unstyled">
                {albums.map(album =>
                    <AlbumTile key={ album.AlbumId } album={ album } />
                )}
                </ul>
            </div>;
        } else {
            return <p>Loading...</p>;
        }
    }
}

// Selects which part of global state maps to this component, and defines a type for the resulting props
const provider = provide(
    (state: ApplicationState) => state.genreDetails,
    GenreDetailsStore.actionCreators
).withExternalProps<{ params: RouteParams }>();

type GenreDetailsProps = typeof provider.allProps;
export default provider.connect(GenreDetails);
