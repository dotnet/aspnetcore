module.exports = {
  extends: [
    '../common/.eslintrc.js',
  ],
  parserOptions: {
    project: "tsconfig.json",
    tsconfigRootDir: __dirname
  }
};
