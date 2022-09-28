using System;

namespace BuildManager.Scripts
{
    [Flags]
    internal enum BuildTarget
    {
        Windows = 1,
        Mac = 2,
        Linux = 4,
        //Standalone = Windows | Mac | Linux,
        Android = 8,
        IOS = 16,
        //Mobile = Android | IOS,
        WebGL = 32,
        UWP = 64,
        AppleTV = 128,
    }
}