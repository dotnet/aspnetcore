// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidatorRegistry } from './Validator';
import { requiredValidator } from './Validators/Required';
import { stringLengthValidator } from './Validators/StringLength';
import { rangeValidator } from './Validators/Range';
import { regexValidator } from './Validators/Regex';
import { emailValidator } from './Validators/Email';
import { urlValidator } from './Validators/Url';
import { phoneValidator } from './Validators/Phone';
import { creditcardValidator } from './Validators/CreditCard';
import { equaltoValidator } from './Validators/EqualTo';
import { fileextensionsValidator } from './Validators/FileExtensions';

export function registerBuiltInValidators(registry: ValidatorRegistry): void {
  registry.set('required', requiredValidator);
  registry.set('length', stringLengthValidator);
  registry.set('minlength', stringLengthValidator);
  registry.set('maxlength', stringLengthValidator);
  registry.set('range', rangeValidator);
  registry.set('regex', regexValidator);
  registry.set('email', emailValidator);
  registry.set('url', urlValidator);
  registry.set('phone', phoneValidator);
  registry.set('creditcard', creditcardValidator);
  registry.set('equalto', equaltoValidator);
  registry.set('fileextensions', fileextensionsValidator);
}

