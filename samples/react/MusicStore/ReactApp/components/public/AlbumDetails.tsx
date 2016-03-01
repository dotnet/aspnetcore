import * as React from 'react';
import { Link } from 'react-router';
import { provide } from 'redux-typed';
import { ApplicationState }  from '../../store';
import * as AlbumDetailsState from '../../store/AlbumDetails';

interface RouteParams {
    albumId: string;
}

class AlbumDetails extends React.Component<AlbumDetailsProps, void> {
    componentWillMount() {
        this.props.requestAlbumDetails(parseInt(this.props.params.albumId));
    }

    componentWillReceiveProps(nextProps: AlbumDetailsProps) {
        this.props.requestAlbumDetails(parseInt(nextProps.params.albumId));
    }

    public render() {
        if (this.props.album) {
            const albumData = this.props.album;
            return <div>
                <h2>{ albumData.Title }</h2>

                <p><img alt={ albumData.Title } src={ albumData.AlbumArtUrl } /></p>

                <div id="album-details">
                    <p>
                        <em>Genre:</em>
                        { albumData.Genre.Name }
                    </p>
                    <p>
                        <em>Artist:</em>
                        { albumData.Artist.Name }
                    </p>
                    <p>
                        <em>Price:</em>
                        ${ albumData.Price.toFixed(2) }
                    </p>
                    <p className="button">
                        Add to cart
                    </p>
                </div>
            </div>;
        } else {
            return <p>Loading...</p>;
        }
    }
}

// Selects which part of global state maps to this component, and defines a type for the resulting props
const provider = provide(
    (state: ApplicationState) => state.albumDetails,
    AlbumDetailsState.actionCreators
).withExternalProps<{ params: RouteParams }>();
type AlbumDetailsProps = typeof provider.allProps;
export default provider.connect(AlbumDetails);
