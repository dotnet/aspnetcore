import React from 'react';
import { Collapse, Nav, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink } from 'reactstrap';
import './NavMenu.css';

export default class NavMenu extends React.Component {
  constructor(props) {
    super(props);

    this.toggle = this.toggle.bind(this);
    this.state = {
      isOpen: true
    };
  }
  toggle() {
    this.setState({
      isOpen: !this.state.isOpen
    });
  }
  render() {
    return (
      <Navbar fixed="top" className="navbar-fixed-top navbar-dark" expand color="dark" >
        <NavbarBrand href="/">Company.WebApplication1</NavbarBrand>
        <NavbarToggler onClick={this.toggle} className="mr-2" />
        <Collapse isOpen={this.state.isOpen} navbar>
          <Nav className="ml-auto" navbar>
            <NavItem>
              <NavLink href="/">Home</NavLink>
            </NavItem>
            <NavItem>
              <NavLink href="/counter">Counter</NavLink>
            </NavItem>
            <NavItem>
              <NavLink href="/fetch-data">Fetch data</NavLink>
            </NavItem>
          </Nav>
        </Collapse>
      </Navbar >
    );
  }
}
