using System.Runtime.InteropServices;

namespace AlmueRaspi
{
    internal static class NativeMethods
    {
        [DllImport("libc")]
        public static extern uint getuid();
    }
}
