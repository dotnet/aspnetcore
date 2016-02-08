import * as React from 'react';
import { Router, Route, HistoryBase } from 'react-router';
import NavMenu from './components/NavMenu';
import Home from './components/public/Home';
import Genres from './components/public/Genres';
import GenreDetails from './components/public/GenreDetails';
import AlbumDetails from './components/public/AlbumDetails';

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

export const routes = <Route component={ Layout }>
    <Route path="/" components={{ body: Home }} />
    <Route path="/genres" components={{ body: Genres }} />
    <Route path="/genre/:genreId" components={{ body: GenreDetails }} />
    <Route path="/album/:albumId" components={{ body: AlbumDetails }} />
</Route>;
