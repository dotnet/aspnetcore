namespace Microsoft.Repl.ConsoleHandling
{
    public struct Point
    {
        public readonly int X;

        public readonly int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static bool operator >(Point left, Point right)
        {
            return left.Y > right.Y || (left.Y == right.Y && right.X > left.X);
        }

        public static bool operator <(Point left, Point right)
        {
            return left.Y < right.Y || (left.Y == right.Y && right.X < left.X);
        }

        public static bool operator ==(Point left, Point right)
        {
            return left.X == right.X && left.Y == right.Y;
        }

        public static bool operator !=(Point left, Point right)
        {
            return left.X != right.X || left.Y != right.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Point other && other.X == X && other.Y == Y;
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public override string ToString()
        {
            return $"(X={X}, Y={Y})";
        }
    }
}
