namespace Nethermind.Field.Montgomery.FrEElement;

using System;
using System.Runtime.InteropServices;

public readonly partial struct FrE
{
    const string libName = "fr.so";

    [DllImport(libName)]
    public static extern ulong[] cMul(ulong[] recv , ulong[] x, ulong[] y);

    [DllImport(libName)]
    public static extern ulong[] cAdd(ulong[] recv, ulong[] x, ulong[] y);

    [DllImport(libName)]
    public static extern ulong[] cSub(ulong[] recv, ulong[] x, ulong[] y);

    [DllImport(libName)]
    public static extern ulong[] cInverse(ulong[] recv, ulong[] z);

    public static void cInverse(FrE recv, FrE z) {
        ulong[] recv_input = {recv.u0, recv.u1, recv.u2, recv.u3};
        ulong[] z_input = {z.u0, z.u1, z.u2, z.u3};
        ulong[] res = cInverse(recv_input, z_input);
        z = new FrE(res[0], res[1], res[2], res[3]);
    }

    [DllImport(libName)]
    public static extern ulong[] cToMont(ulong[] recv);

    [DllImport(libName)]
    public static extern ulong[] cFromMont(ulong[] recv);

}