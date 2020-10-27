using System.Runtime.InteropServices;

namespace BasicTestApp.InteropTest
{
    [StructLayout(LayoutKind.Explicit)]
    public struct InteropStruct
    {
        [FieldOffset(0)]
        public string Message;

        [FieldOffset(8)]
        public int NumberField;
    }
}
