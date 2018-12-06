# CORS Sample

This sample consists of a request origin (SampleOrigin) and a request destination (SampleDestination). Both have different domain names, to simulate a CORS request. 

## Modify Hosts File
To run this CORS sample, modify the hosts file to register the hostnames `destination.example.com` and `origin.example.com`.
### Windows:
Run a text editor (e.g. Notepad) as an Administrator. Open the hosts file on the path: "C:\Windows\System32\drivers\etc\hosts".

### Linux:
On a Terminal window, type "sudo nano /etc/hosts" and enter your admin password when prompted.

In the hosts file, add the following to the bottom of the file:

```
127.0.0.1	 destination.example.com
127.0.0.1	 origin.example.com 
```

Save the file and close it. Then clear your browser history.

## Run the sample
The SampleOrigin application will use port 5001, and SampleDestination will use 5000. Please ensure there are no other processes using those ports before running the CORS sample.

* In a command prompt window, open the directory where you cloned the repository, and open the SampleDestination directory. Run the command: dotnet run
* Repeat the above step in the SampleOrigin directory
* Open a browser window and go to `http://origin.example.com:5001`
* Input a method and header to create a CORS request or use one of the example buttons to see CORS in action

As an example, apart from `GET`, `HEAD` and `POST` requests, `PUT` requests are allowed in the CORS policy on SampleDestination. Any others, like `DELETE`, `OPTIONS` etc. are not allowed and throw an error.
`Cache-Control` has been added as an allowed header to the sample. Any other headers are not allowed and throw an error. You may leave the header name and value blank.

To edit the policy, please see `app.UseCors()` method in the `Startup.cs` file of SampleDestination.

**If using Visual Studio to launch the request origin:**
Open Visual Studio and in the `launchSettings.json` file for the SampleOrigin project, change the `launchUrl` under SampleOrigin to `http://origin.example.com:5001`.
Using the dropdown near the Start button, choose SampleOrigin before pressing Start to ensure that it uses Kestrel and not IIS Express. 

