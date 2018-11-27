-- script that POSTs body for requests

function init(args)
    wrk.body = "Age=12&BirthDate=2006-03-01T09%3A51%3A43.041-07%3A00&Name=George"
    wrk.headers["Content-Type"] = "application/x-www-form-urlencoded"
    wrk.method = "POST"
end
