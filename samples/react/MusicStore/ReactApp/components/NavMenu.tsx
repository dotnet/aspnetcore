import * as React from 'react';                                                                                                                                                                    
import { Navbar, Nav, NavItem, NavDropdown, MenuItem } from 'react-bootstrap';
import { Link } from 'react-router';
import { LinkContainer } from 'react-router-bootstrap';
import { provide } from '../TypedRedux';
import { ApplicationState }  from '../store';
import * as GenreList from '../store/GenreList';

class NavMenu extends React.Component<NavMenuProps, void> {
    componentWillMount() {
        this.props.requestGenresList();
    }

    public render() {
        var genres = this.props.genres.slice(0, 5);
        return (
            <Navbar inverse fixedTop>
                <Navbar.Header>
                    <Navbar.Brand><Link to={ '/' }>Music Store</Link></Navbar.Brand>
                </Navbar.Header>
                <Navbar.Collapse>
                    <Nav>
                        <LinkContainer to={ '/' }><NavItem>Home</NavItem></LinkContainer>
                        <NavDropdown id="menu-dropdown" title="Store">
                            {genres.map(genre =>
                                <LinkContainer key={ genre.GenreId } to={ `/genre/${ genre.GenreId }` }>
                                    <MenuItem>{ genre.Name }</MenuItem>
                                </LinkContainer>     
                            )}
                            <MenuItem divider />
                            <LinkContainer to={ '/genres' }><MenuItem>More…</MenuItem></LinkContainer>
                        </NavDropdown>
                    </Nav>
                    <Nav pullRight>
                        <NavItem href="#">Admin</NavItem>
                    </Nav>
                </Navbar.Collapse>
            </Navbar>
        );
    }
}

// Selects which part of global state maps to this component, and defines a type for the resulting props
const provider = provide(
    (state: ApplicationState) => state.genreList,
    GenreList.actionCreators    
);
type NavMenuProps = typeof provider.allProps;
export default provider.connect(NavMenu);
