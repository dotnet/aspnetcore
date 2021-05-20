## Http2Cat

Http2Cat is a low level Http2 testing framework designed to excersize a server with frame level control. This can be useful for unit testing and compat testing.

The framework is distributed as internal sources since it shares the basic building blocks from Kestrel's Http2 implementation (frames, enum flags, frame reading and writing, etc.). InternalsVisibleTo should not be used, any needed components should be moved to one of the shared code directories.

This Http2Cat folder contains non-production code used in the test client. The shared production code is kept in separate folders.
