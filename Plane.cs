using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckGenerationPrism
{
    public class Plane
    {
        public double coefA;
        public double coefB;
        public double coefC;
        public double coefD;

        public Plane(Point p1, Point p2, Point p3)
        {
            Vector<double> A = Vector<double>.Build.DenseOfArray(new double[] { p1.X, p1.Y, p1.Z });
            Vector<double> B = Vector<double>.Build.DenseOfArray(new double[] { p2.X, p2.Y, p2.Z });
            Vector<double> C = Vector<double>.Build.DenseOfArray(new double[] { p3.X, p3.Y, p3.Z });

            var vectorAB = B - A;
            var vectorAC = C - A;

            var matrix = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                    {A[0]*(-1), A[1]*(-1),  A[2]*(-1) },
                    {vectorAB[0],vectorAB[1],vectorAB[2] },
                    {vectorAC[0],vectorAC[1],vectorAC[2] },
            });

            var coeffAtferX = matrix[0, 0] * Matrix<double>.Build.DenseOfArray(new double[,] { { matrix[1, 1], matrix[1, 2] }, { matrix[2, 1], matrix[2, 2] } }).Determinant();
            var coeffBeforX = Matrix<double>.Build.DenseOfArray(new double[,] { { matrix[1, 1], matrix[1, 2] }, { matrix[2, 1], matrix[2, 2] } }).Determinant();

            var coeffAtferY = (-1) * matrix[0, 1] * Matrix<double>.Build.DenseOfArray(new double[,] { { matrix[1, 0], matrix[2, 0] }, { matrix[1, 2], matrix[2, 2] } }).Determinant();
            var coeffBeforY = (-1) * Matrix<double>.Build.DenseOfArray(new double[,] { { matrix[1, 0], matrix[2, 0] }, { matrix[1, 2], matrix[2, 2] } }).Determinant();

            var coeffAtferZ = matrix[0, 2] * Matrix<double>.Build.DenseOfArray(new double[,] { { matrix[1, 0], matrix[1, 1] }, { matrix[2, 0], matrix[2, 1] } }).Determinant();
            var coeffBeforZ = Matrix<double>.Build.DenseOfArray(new double[,] { { matrix[1, 0], matrix[1, 1] }, { matrix[2, 0], matrix[2, 1] } }).Determinant();


            coefA = coeffBeforX;
            coefB = coeffBeforY;
            coefC = coeffBeforZ;
            coefD = coeffAtferX + coeffAtferY + coeffAtferZ;

            //for (int i = 50; i > 1; i--)
            //{
            //    if (coefA % i == 0 && coefB % i == 0 && coefC % i == 0 && coefD % i == 0)
            //    {
            //        coefA /= i;
            //        coefB /= i;
            //        coefC /= i;
            //        coefD /= i;
            //    }
            //}
        }

        private Plane(double a, double b, double c, double d)
        {
            coefA = a;
            coefB = b;
            coefC = c;
            coefD = d;
        }

        public static bool IsPointBetweenPlanes(Plane p1, Plane p2, Point point)
        {
            double p1Re1 = p1.coefA * point.X + p1.coefB * point.Y + p1.coefC * point.Z + p1.coefD;
            double p1Res2 = p2.coefA * point.X + p2.coefB * point.Y + p2.coefC * point.Z + p2.coefD;

            if (p1Re1 * p1Res2 <= 0)
                return true;

            return false;
        }

        public static Plane operator *(Plane plane, int i)
        {
            plane.coefA *= i;
            plane.coefB *= i;
            plane.coefC *= i;
            plane.coefD *= i;

            return new Plane(plane.coefA, plane.coefB, plane.coefC, plane.coefD);
        }

        public override string ToString()
        {
            return $"{coefA:0.00}*x".Replace(',', '.') + $"+ ({coefB:0.00})*y".Replace(',', '.') +
                $"+ ({coefC:0.00})*z".Replace(',', '.') + $"+ ({coefD:0.00}) = 0".Replace(',', '.');
        }
    }
}
