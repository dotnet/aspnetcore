module.exports = {
  extends: [
    '../.eslintrc.json',
  ],
  ignorePatterns: ["scripts"],
  parserOptions: {
    project: ["tsconfig.json"],
    tsconfigRootDir: __dirname
  }
};
