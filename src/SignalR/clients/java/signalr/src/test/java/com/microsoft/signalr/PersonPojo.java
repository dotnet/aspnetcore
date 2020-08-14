// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
