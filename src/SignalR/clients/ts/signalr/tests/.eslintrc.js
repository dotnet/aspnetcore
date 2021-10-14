module.exports = {
  extends: [
    '../../common/.eslintrc.json',
  ],
  parserOptions: {
    project: ["tsconfig.json"],
    tsconfigRootDir: __dirname
  }
};
