using Nethermind.Field.Montgomery.FpEElement;
using Nethermind.Field.Montgomery.FrEElement;

namespace Nethermind.Verkle.Curve;

public class ExtendedPoint
{
    public readonly FpE X;
    public readonly FpE Y;
    public readonly FpE Z;

    private static FpE A => CurveParams.A;
    private static FpE D => CurveParams.D;

    public ExtendedPoint(FpE x, FpE y)
    {
        X = x;
        Y = y;
        Z = FpE.One;
    }

    private ExtendedPoint(FpE x, FpE y, FpE z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    private ExtendedPoint(AffinePoint p)
    {
        X = p.X;
        Y = p.Y;
        Z = FpE.One;
    }

    public bool IsZero => X.IsZero && Y.Equals(Z) && !Y.IsZero;
    public static ExtendedPoint Identity() => new(AffinePoint.Identity());

    public static ExtendedPoint Generator() => new(AffinePoint.Generator());
    public ExtendedPoint Dup() => new(X.Dup(), Y.Dup(), Z.Dup());

    public static bool Equals(ExtendedPoint p, ExtendedPoint q)
    {
        if (p.IsZero) return q.IsZero;
        if (q.IsZero) return false;

        return ((p.X * q.Z).Equals(p.Z * q.X)) && ((p.Y * q.Z).Equals(q.Y * p.Z));
    }

    public static ExtendedPoint Neg(ExtendedPoint p) => new(p.X.Negative(), p.Y, p.Z);

    // https://hyperelliptic.org/EFD/g1p/auto-twisted-projective.html
    public static ExtendedPoint Add(ExtendedPoint p, ExtendedPoint q)
    {
        FpE x1 = p.X;
        FpE y1 = p.Y;
        FpE z1 = p.Z;

        FpE x2 = q.X;
        FpE y2 = q.Y;
        FpE z2 = q.Z;

        FpE a = z1 * z2;
        FpE b = a * a;

        FpE c = x1 * x2;

        FpE d = y1 * y2;

        FpE e = D * c * d;

        FpE f = b - e;
        FpE g = b + e;

        FpE x3 = a * f * ((x1 + y1) * (x2 + y2) - c - d);
        FpE y3 = a * g * (d - A * c);
        FpE z3 = f * g;

        return new ExtendedPoint(x3, y3, z3);
    }
    public static ExtendedPoint Sub(ExtendedPoint p, ExtendedPoint q) => Add(p, Neg(q));
    public static ExtendedPoint Double(ExtendedPoint p)
    {
        FpE x1 = p.X;
        FpE y1 = p.Y;
        FpE z1 = p.Z;

        FpE b = (x1 + y1) * (x1 + y1);
        FpE c = x1 * x1;
        FpE d = y1 * y1;

        FpE e = A * c;
        FpE f = e + d;
        FpE h = z1 * z1;
        FpE j = f - (h + h);

        FpE x3 = (b - c - d) * j;
        FpE y3 = f * (e - d);
        FpE z3 = f * j;
        return new ExtendedPoint(x3, y3, z3);
    }

    public static ExtendedPoint ScalarMultiplication(ExtendedPoint point, FrE scalarMont)
    {
        ExtendedPoint? result = Identity();
        ExtendedPoint? temp = point.Dup();

        FrE.ToRegular(in scalarMont, out FrE scalar);

        int len = scalar.BitLen();
        for (int i = len; i >= 0; i--)
        {
            result = Double(result);
            if (scalar.Bit(i))
            {
                result += temp;
            }
        }
        return result;
    }

    public AffinePoint ToAffine()
    {
        if (IsZero) return AffinePoint.Identity();
        if (Z.IsZero) throw new Exception();
        if (Z.IsOne) return new AffinePoint(X, Y);

        FpE.Inverse(Z, out FpE zInv);
        FpE xAff = X * zInv;
        FpE yAff = Y * zInv;

        return new AffinePoint(xAff, yAff);
    }

    public byte[] ToBytes() => ToAffine().ToBytes();

    public static ExtendedPoint operator +(in ExtendedPoint a, in ExtendedPoint b)
    {
        return Add(a, b);
    }

    public static ExtendedPoint operator -(in ExtendedPoint a, in ExtendedPoint b)
    {
        return Sub(a, b);
    }

    public static ExtendedPoint operator *(in ExtendedPoint a, in FrE b)
    {
        return ScalarMultiplication(a, b);
    }

    public static ExtendedPoint operator *(in FrE a, in ExtendedPoint b)
    {
        return ScalarMultiplication(b, a);
    }

    public static bool operator ==(in ExtendedPoint a, in ExtendedPoint b)
    {
        return Equals(a, b);
    }

    public static bool operator !=(in ExtendedPoint a, in ExtendedPoint b)
    {
        return !(a == b);
    }

    private bool Equals(ExtendedPoint other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((ExtendedPoint)obj);
    }

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

}
