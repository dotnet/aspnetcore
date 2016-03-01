import React from 'react';
import Formsy from 'formsy-react';
import { Input } from 'formsy-react-components';
import { fakeData } from '../data/fakeData.js';

export class PersonEditor extends React.Component {
    constructor() {
        super();
        this.state = { savedChanges: false };
    }

    onChange() {
        this.setState({ savedChanges: false });
    }

    submit(model, reset, setErrors) {
        PersonEditor.sendJson('put', `/api/people/${ this.props.params.personId }`, model).then(response => {
            if (response.ok) {
                this.setState({ savedChanges: true });
            } else {
                // Parse server-side validation errors from the response and display them
                response.json().then(setErrors);
            }
        });
    }

    render() {
        var personId = parseInt(this.props.params.personId);
        var person = fakeData.filter(p => p.id === personId)[0];
        var notificationBox = this.state.savedChanges
            && <div className="alert alert-success"><b>Done!</b> Your changes were saved.</div>;

        return <div className='row'>
            <div className='page-header'>
                <h1>Edit { person.name }</h1>
            </div>
            <Formsy.Form ref='form' className='form-horizontal' onChange={ this.onChange.bind(this) } onValidSubmit={ this.submit.bind(this) }>
                <Input name='name' label='Name' value={ person.name } required />
                <Input name='city' label='City' value={ person.city } required />
                <Input name='state' label='State' value={ person.state } required />
                <Input name='country' label='Country' value={ person.country } required />
                <Input name='company' label='Company' value={ person.company } required />
                <Input name='favoriteNumber' label='Favorite Number' value={ person.favoriteNumber } required validations="isInt" validationError="Must be a number" />
                { notificationBox }
                <button type='submit' className='btn btn-primary'>Save</button>
            </Formsy.Form>
        </div>;
    }

    static sendJson(method, url, object) {
        return fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(object)
        });
    }
}
