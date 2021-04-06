module.exports = {
  root: true,
  parser: '@typescript-eslint/parser',
  plugins: [
    '@typescript-eslint',
  ],
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
  ],
  rules: {
    "@typescript-eslint/ban-types": [
      "error",
      {
        // same behavior as before in TSLint
        "types": {
          "Object": "Avoid using the `Object` type. Did you mean `object`?",
          "Function": "Avoid using the `Function` type. Prefer a specific function type, like `() => void`.",
          "Boolean": "Avoid using the `Boolean` type. Did you mean `boolean`?",
          "Number": "Avoid using the `Number` type. Did you mean `number`?",
          "String": "Avoid using the `String` type. Did you mean `string`?",
          "Symbol": "Avoid using the `Symbol` type. Did you mean `symbol`?"
        },
        "extendDefaults": false
      }
    ],

    "@typescript-eslint/no-inferrable-types": "off",
    "@typescript-eslint/no-explicit-any": "off",
    "@typescript-eslint/no-non-null-assertion": "off",
    "@typescript-eslint/ban-ts-comment": "off",
    "@typescript-eslint/explicit-module-boundary-types": ["warn", { "allowArgumentsExplicitlyTypedAsAny": true }],
    "no-unused-vars": "off",
    "@typescript-eslint/no-unused-vars": "off", // use the settings from tsconfig for these
    "no-empty-function": "off",
    "@typescript-eslint/no-empty-function": ["error", { "allow": ["private-constructors", "arrowFunctions"] }],
    "no-constant-condition": "off",

    "max-len": ["error", { "code": 300 }],
    "@typescript-eslint/member-ordering": "off",
    "@typescript-eslint/interface-name-prefix": "off",
    "@typescript-eslint/unified-signatures": "off",
    "max-classes-per-file": "off",
    "@typescript-eslint/no-floating-promises": "error",
    "no-empty": "off",
    "no-console": "off",
    "prefer-const": ["error", {
      "ignoreReadBeforeAssign": false
    }]
  }
};
