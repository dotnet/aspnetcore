// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

class PersonPojo<T> implements Comparable<PersonPojo<T>> {
    public String firstName;
    public String lastName;
    public int age;
    public T t;

    public PersonPojo() {
        super();
    }

    public PersonPojo(String firstName, String lastName, int age, T t) {
        this.firstName = firstName;
        this.lastName = lastName;
        this.age = age;
        this.t = t;
    }

    public String getFirstName() {
        return this.firstName;
    }

    public String getLastName() {
        return this.lastName;
    }

    public int getAge() {
        return this.age;
    }

    public T getT() {
        return t;
    }

    @Override
    public int compareTo(PersonPojo<T> ep) {
        return 0;
    }
}
