// Guids.cs
// MUST match guids.h
using System;

namespace DgmlTestMonitor
{
    static class GuidList
    {
        public const string guidDgmlTestMonitorPkgString = "69a221af-1c88-4ffd-8f14-fb2d1862ac0a";
        public const string guidDgmlTestMonitorCmdSetString = "ec667542-c21f-4467-bfac-4d94ce61a4dc";
        public const string guidToolWindowPersistanceString = "0b5d9891-3590-4dfb-94c2-8fb5e92550f6";

        public static readonly Guid guidDgmlTestMonitorCmdSet = new Guid(guidDgmlTestMonitorCmdSetString);
    };
}