const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:8080';

const PROXY_CONFIG = [
  {
    context: [
      "/weatherforecast",
//#if (IndividualLocalAuth)
      "/_configuration",
      "/.well-known",
      "/Identity",
      "/connect",
      "/ApplyDatabaseMigrations",
//#endif
   ],
    target: target,
    secure: false
  }
]

module.exports = PROXY_CONFIG;
