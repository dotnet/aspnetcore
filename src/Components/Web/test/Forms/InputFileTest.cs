// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class InputFileTest
{
    [Fact]
    public void DerivedClass_CanOverrideDisposeMethod()
    {
        // Arrange
        var disposed = false;
        var derivedInputFile = new DerivedInputFile(() => disposed = true);

        // Act
        ((IDisposable)derivedInputFile).Dispose();

        // Assert
        Assert.True(disposed, "Derived class Dispose(bool) method should be called");
    }

    private class DerivedInputFile : InputFile
    {
        private readonly Action _onDispose;

        public DerivedInputFile(Action onDispose)
        {
            _onDispose = onDispose;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _onDispose();
            }
            base.Dispose(disposing);
        }
    }
}
