module.exports = {
  extends: [
    '../common/.eslintrc.json',
  ],
  ignorePatterns: ["dist"],
  parserOptions: {
    project: ["tsconfig.json"],
    tsconfigRootDir: __dirname
  }
};
