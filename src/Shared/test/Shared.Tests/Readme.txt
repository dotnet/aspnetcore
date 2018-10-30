NOTE:
1. The tests for 'ExceptionDetailProvider' and 'StackTraceHelper' in project 'Microsoft.Extensions.StackTrace.Sources' are located in Diagnostics
   repo. This is because they refer to some packages from FileSystem repo which causes a circular reference and breaks the
   build.