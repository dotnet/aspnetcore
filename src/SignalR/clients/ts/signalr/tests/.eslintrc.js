module.exports = {
  extends: [
    '../../.eslintrc.json',
  ],
  parserOptions: {
    project: ["tsconfig.json"],
    tsconfigRootDir: __dirname
  }
};
