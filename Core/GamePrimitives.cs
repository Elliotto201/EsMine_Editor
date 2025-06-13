using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineCore
{
    public struct Color
    {
        public int R;
        public int G;
        public int B;

        public Color(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    public struct Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 UnitY => new Vector3(0f, 1f, 0f);

        public Vector3 Normalized()
        {
            float length = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
            if (length == 0f) return new Vector3(0f, 0f, 0f);
            return new Vector3(X / length, Y / length, Z / length);
        }


        public static implicit operator OpenTK.Mathematics.Vector3(Vector3 v)
        {
            return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
        }

        // Addition
        public static Vector3 operator +(Vector3 a, Vector3 b) =>
            new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        // Subtraction
        public static Vector3 operator -(Vector3 a, Vector3 b) =>
            new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        // Component-wise multiplication
        public static Vector3 operator *(Vector3 a, Vector3 b) =>
            new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

        // Scalar multiplication
        public static Vector3 operator *(Vector3 v, float scalar) =>
            new Vector3(v.X * scalar, v.Y * scalar, v.Z * scalar);

        public static Vector3 operator *(float scalar, Vector3 v) =>
            v * scalar;

        // Component-wise division
        public static Vector3 operator /(Vector3 a, Vector3 b) =>
            new Vector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);

        // Scalar division
        public static Vector3 operator /(Vector3 v, float scalar) =>
            new Vector3(v.X / scalar, v.Y / scalar, v.Z / scalar);
    }

    public struct Vector3Int
    {
        public int X;
        public int Y;
        public int Z;

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3Int UnitY => new Vector3Int(0, 1, 0);

        public Vector3 Normalized()
        {
            float length = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
            if (length == 0f) return new Vector3(0f, 0f, 0f);
            return new Vector3(X / length, Y / length, Z / length);
        }

        public static implicit operator OpenTK.Mathematics.Vector3i(Vector3Int v)
        {
            return new OpenTK.Mathematics.Vector3i(v.X, v.Y, v.Z);
        }

        // Addition
        public static Vector3Int operator +(Vector3Int a, Vector3Int b) =>
            new Vector3Int(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        // Subtraction
        public static Vector3Int operator -(Vector3Int a, Vector3Int b) =>
            new Vector3Int(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        // Component-wise multiplication
        public static Vector3Int operator *(Vector3Int a, Vector3Int b) =>
            new Vector3Int(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

        // Scalar multiplication
        public static Vector3Int operator *(Vector3Int v, int scalar) =>
            new Vector3Int(v.X * scalar, v.Y * scalar, v.Z * scalar);

        public static Vector3Int operator *(int scalar, Vector3Int v) =>
            v * scalar;

        // Component-wise division
        public static Vector3Int operator /(Vector3Int a, Vector3Int b) =>
            new Vector3Int(a.X / b.X, a.Y / b.Y, a.Z / b.Z);

        // Scalar division
        public static Vector3Int operator /(Vector3Int v, int scalar) =>
            new Vector3Int(v.X / scalar, v.Y / scalar, v.Z / scalar);
    }
}
