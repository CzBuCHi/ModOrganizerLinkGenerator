using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ModOrganizerLinkGenerator
{
    [SuppressMessage("ReSharper", "StyleCop.SA1305", Justification = "DllImport")]
    public static class PInvoke
    {
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "DllImport")]
        public enum SYMBOLIC_LINK_FLAG
        {
            FILE = 0,

            DIRECTORY = 1
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SYMBOLIC_LINK_FLAG dwFlags);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}