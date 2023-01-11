using System;
using System.Runtime.InteropServices;

class fr
{
    const string libName = "fp.so";

// KNOW: fp is defined as [4]uint64_t
// Idea 1: just pass IntPtr to FrE and hope compiler handles it
// Idea 2: create struct of fr that is an array of ulongs and convert from FrE to Fr
// Idea 3: create method to convert FpE to array then past in
// Idea 4: regenerate libraries with FpE struct that has 4 fields instead of array

    [DllImport(libName)]
    public static extern IntPtr cMul(IntPtr recv , IntPtr x, IntPtr y);

    [DllImport(libName)]
    public static extern IntPtr cAdd(IntPtr recv, IntPtr x, IntPtr y);

    [DllImport(libName)]
    public static extern IntPtr cSub(IntPtr recv, IntPtr x, IntPtr y);

    [DllImport(libName)]
    public static extern IntPtr cInverse(IntPtr recv);

    [DllImport(libName)]
    public static extern IntPtr cToMont(IntPtr recv);

    [DllImport(libName)]
    public static extern IntPtr cFromMont(IntPtr recv);

}