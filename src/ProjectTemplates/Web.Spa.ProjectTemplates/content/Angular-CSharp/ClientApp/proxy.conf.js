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
//#if(RequiresHttps)
    target: "https://localhost:5001",
//#else
    target: "http://localhost:5000",
//#endif
    secure: false
  }
]

module.exports = PROXY_CONFIG;
