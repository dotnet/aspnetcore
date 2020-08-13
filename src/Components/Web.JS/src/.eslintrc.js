module.exports = {
  parser: '@typescript-eslint/parser',  // Specifies the ESLint parser
  plugins: ['@typescript-eslint'],
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',  // Uses the recommended rules from the @typescript-eslint/eslint-plugin
  ],
  env: {
    browser: true,
    es6: true,
  },
  rules: {
    // Place to specify ESLint rules. Can be used to overwrite rules specified from the extended configs
    // e.g. "@typescript-eslint/explicit-function-return-type": "off",
    "@typescript-eslint/indent": ["error", 2],
    "@typescript-eslint/no-use-before-define": [ "off" ],
    "@typescript-eslint/no-unused-vars": ["error", { "varsIgnorePattern": "^_", "argsIgnorePattern": "^_" }],
    "no-var": "error",
    "prefer-const": "error",
    "quotes": ["error", "single", { "avoidEscape": true }],
    "semi": ["error", "always"],
    "semi-style": ["error", "last"],
    "semi-spacing": ["error", { "after": true }],
    "spaced-comment": ["error", "always"],
    "unicode-bom": ["error", "never"],
    "brace-style": ["error", "1tbs"],
    "comma-dangle": ["error", {
      "arrays": "always-multiline",
      "objects": "always-multiline",
      "imports": "always-multiline",
      "exports": "always-multiline",
      "functions": "ignore"
    }],
    "comma-style": ["error", "last"],
    "comma-spacing": ["error", { "after": true }],
    "no-trailing-spaces": ["error"],
    "curly": ["error"],
    "dot-location": ["error", "property"],
    "eqeqeq": ["error", "always"],
    "no-eq-null": ["error"],
    "no-multi-spaces": ["error"],
    "no-unused-labels": ["error"],
    "require-await": ["error"],
    "array-bracket-newline": ["error", { "multiline": true, "minItems": 4 }],
    "array-bracket-spacing": ["error", "never"],
    "array-element-newline": ["error", { "minItems": 3 }],
    "block-spacing": ["error"],
    "func-call-spacing": ["error", "never"],
    "function-paren-newline": ["error", "multiline"],
    "key-spacing": ["error", { "mode": "strict" }],
    "keyword-spacing": ["error", { "before": true }],
    "lines-between-class-members": ["error", "always"],
    "new-parens": ["error"],
    "no-multi-assign": ["error"],
    "no-multiple-empty-lines": ["error"],
    "no-unneeded-ternary": ["error"],
    "no-whitespace-before-property": ["error"],
    "one-var": ["error", "never"],
    "space-before-function-paren": ["error", {
      "anonymous": "never",
      "named": "never",
      "asyncArrow": "always"
    }],
    "space-in-parens": ["error", "never"],
    "space-infix-ops": ["error"]

  },
  globals: {
    DotNet: "readonly"
  }
};
