# Microsoft.AspNetCore.Server.IIS.Core

``` diff
 namespace Microsoft.AspNetCore.Server.IIS.Core {
     public class IISServerAuthenticationHandler : IAuthenticationHandler {
         public IISServerAuthenticationHandler();
         public Task<AuthenticateResult> AuthenticateAsync();
         public Task ChallengeAsync(AuthenticationProperties properties);
         public Task ForbidAsync(AuthenticationProperties properties);
         public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context);
     }
     public class ThrowingWasUpgradedWriteOnlyStream : WriteOnlyStream {
         public ThrowingWasUpgradedWriteOnlyStream();
         public override void Flush();
         public override long Seek(long offset, SeekOrigin origin);
         public override void SetLength(long value);
         public override void Write(byte[] buffer, int offset, int count);
         public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
     }
     public abstract class WriteOnlyStream : Stream {
         protected WriteOnlyStream();
         public override bool CanRead { get; }
         public override bool CanSeek { get; }
         public override bool CanWrite { get; }
         public override long Length { get; }
         public override long Position { get; set; }
         public override int ReadTimeout { get; set; }
         public override int Read(byte[] buffer, int offset, int count);
         public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
         public override long Seek(long offset, SeekOrigin origin);
         public override void SetLength(long value);
     }
 }
```

