module.exports = {
  extends: [
    '../common/.eslintrc.json',
  ],
  ignorePatterns: ["scripts"],
  parserOptions: {
    project: ["tsconfig.json"],
    tsconfigRootDir: __dirname
  }
};
