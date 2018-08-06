import React, { Component } from 'react';
import { Col, Container, Row } from 'reactstrap';
import { NavMenu } from './NavMenu';

export class Layout extends Component {
  displayName = Layout.name

  render() {
    return (
      <Container className="mw-100" >
        <Row>
          <Col sm={3}>
            <NavMenu />
          </Col>
          <Col sm={9}>
            {this.props.children}
          </Col>
        </Row>
      </Container>
    );
  }
}
