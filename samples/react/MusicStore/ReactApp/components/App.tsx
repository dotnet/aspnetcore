import * as React from 'react';
import { Router, Route, HistoryBase } from 'react-router';
import NavMenu from './NavMenu';
import Home from './public/Home';
import Genres from './public/Genres';
import GenreDetails from './public/GenreDetails';
import AlbumDetails from './public/AlbumDetails';

export interface AppProps {
    history: HistoryBase;
}

export class App extends React.Component<AppProps, void> {
    public render() {
        return (
            <Router history={ this.props.history }>
                <Route component={ Layout }>
                    <Route path="/" components={{ body: Home }} />
                    <Route path="/genres" components={{ body: Genres }} />
                    <Route path="/genre/:genreId" components={{ body: GenreDetails }} />
                    <Route path="/album/:albumId" components={{ body: AlbumDetails }} />
                </Route>
            </Router>
        );
    }
}

class Layout extends React.Component<{ body: React.ReactElement<any> }, void> {
    public render() {
        return <div>
            <NavMenu />
            <div className="container">
                { this.props.body }
            </div>
        </div>;
    }
}
