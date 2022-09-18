using System;

namespace BuildManager
{
    [Flags]
    public enum BuildTarget
    {
        Windows = 1,
        MacIntel = 2,
        MacSilicon = 4,
        MacBoth = 8,
        Linux = 16,
        //Standalone = Windows | MacBoth | Linux,
        Android = 32,
        IOS = 64,
        //Mobile = Android | IOS,
        WebGL = 128,
        UWP = 256,
        AppleTV = 512,
    }
}