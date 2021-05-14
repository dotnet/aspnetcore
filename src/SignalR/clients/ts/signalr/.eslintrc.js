module.exports = {
  extends: [
    '../common/.eslintrc.json',
  ],
  parserOptions: {
    project: ["tsconfig.json", "./tests/tsconfig.json"],
    tsconfigRootDir: __dirname
  }
};
