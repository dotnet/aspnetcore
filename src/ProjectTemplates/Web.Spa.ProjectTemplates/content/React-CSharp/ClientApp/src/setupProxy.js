const createProxyMiddleware = require('http-proxy-middleware');

const context =  [
  "/weatherforecast",
//#if (IndividualLocalAuth)
  "/_configuration",
  "/.well-known",
  "/Identity",
  "/connect",
  "/ApplyDatabaseMigrations",
//#endif
];

module.exports = function(app) {
  const appProxy = createProxyMiddleware(context, {
//#if(RequiresHttps)
    target: 'https://localhost:5001',
//#else
    target: 'http://localhost:5000',
//#endif
    secure: false
  });

  app.use(appProxy);
};
