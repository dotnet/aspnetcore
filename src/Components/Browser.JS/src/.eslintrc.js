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
    "no-trailing-spaces": ["error"]
  },
  globals: {
    DotNet: "readonly"
  }
};
