using MathNet.Numerics.LinearAlgebra;
using System.Text;

namespace CheckGenerationPrism
{
    static class VectorExtention
    {
        public static string ToStringPoint(this Vector<double> v)
        {
            return $"({v[0]:0.00}".Replace(',', '.') + "," + $"{v[1]:0.00}".Replace(',', '.') + "," + $"{v[2]:0.00}".Replace(',', '.') + ")";
        }

        public static Point ToPoint(this Vector<double> v)
        {
            return new Point(v[0], v[1], v[2]);
        }
    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double DistantToPoint(Point point)
        {
            return Point.InvSqrt(Math.Pow(X - point.X, 2) + Math.Pow(Y - point.Y, 2) + Math.Pow(Z - point.Z, 2));
            //return Math.Sqrt(Math.Pow(X - point.X, 2) + Math.Pow(Y - point.Y, 2) + Math.Pow(Z - point.Z, 2));
        }

        public static double DistantToPoint(Point p1, Point p2)
        {
            return Point.InvSqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
            //return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
        }

        public override string ToString()
        {
            return $"{X:0.00}".Replace(',', '.') + "," + $"{Y:0.00}".Replace(',', '.') + "," + $"{Z:0.00}".Replace(',', '.') + "\n";
        }

        public static Point operator +(Point lhs, Point rhs)
        {
            return new Point(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        }
        public static Point operator +(Point lhs, int d)
        {
            return new Point(lhs.X + d, lhs.Y + d, lhs.Z + d);
        }
        public static double InvSqrt(double x)
        {
            double t = x;
            double xhalf = 0.5d * x;
            long i = BitConverter.DoubleToInt64Bits(x);
            i = 0x5fe6ec85e7de30daL - (i >> 1);
            x = BitConverter.Int64BitsToDouble(i);
            x = x * (1.5d - xhalf * x * x); // Первая итерация
            return x * t;
        }

        // return point b + d
        public static Point PointPlusD(Point A, Point B, int d)
        {
            // Вычисляем компоненты вектора AB
            double deltaX = B.X - A.X;
            double deltaY = B.Y - A.Y;
            double deltaZ = B.Z - A.Z;

            // Вычисляем квадрат длины вектора AB
            double lengthSquared = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;

            // Приближеннное значение обратного квадратного корня
            double invLength = InvSqrt(lengthSquared);

            // Вычисляем направления
            double directionX = deltaX * invLength;
            double directionY = deltaY * invLength;
            double directionZ = deltaZ * invLength;

            return new Point(
                B.X + d * directionX,
                B.Y + d * directionY,
                B.Z + d * directionZ);
        }
    }

    public class PrismModel
    {
        public static object lockObject = new();
        public Point center;

        public double alphaAngel;
        public double betaAngel;
        public double gammaAngel;
        public int height;
        public int width;
        public double maxDistance;
        public double V;

        public List<(Plane, Plane)>? planes;        // список плоскостей призмы
        public List<Vector<double>>? pointsOfPrism; // точки между гранями призмы
        public List<Vector<double>>? points;        // точки самих граней призмы
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point">Середина призмы, начальная точка</param>
        /// <param name="alpha">угл поворота в градусах по оси x</param>
        /// <param name="beta">угл поворота в градусах по оси y</param>
        /// <param name="gamma">угл поворота в градусах по оси z</param>
        /// <param name="height">высота призмы</param>
        /// <param name="width">ширина основания</param>
        public PrismModel(Point point, double alpha = 0, double beta = 0, double gamma = 0, int height = 4, int width = 4)
        {
            V = ((3 * Point.InvSqrt(3) * Math.Pow(width / 2, 2)) / 2) * height;
            center = point;
            alphaAngel = alpha;
            betaAngel = beta;
            gammaAngel = gamma;
            this.height = height;
            this.width = width;
            points = new List<Vector<double>>();
            pointsFilling(points);
            applyingRotationMatrix(points);
            maxDistance = Point.InvSqrt(Math.Pow(height / 2, 2) + Math.Pow(width / 2, 2));
        }

        // заполнение модели
        private void pointsFilling(List<Vector<double>> points)
        {
            //var p = new Point() { X = center.X+width, Y = center.Y, Z = center.Z-height/2 };
            var p = Vector<double>.Build.DenseOfArray(new double[] { width / 2.0, 0, -height / 2.0 });//width/2 for x, height/2 for z
            points.Add(p);
            for (int i = 1; i < 6; i++)
            {
                var first = p[0] * Math.Cos(Math.PI * 60 * i / 180);
                var second = p[0] * -1 * Math.Sin(Math.PI * 60 * i / 180);
                var third = p[2];
                points.Add(Vector<double>.Build.DenseOfArray(new double[] {first,
                     second, third }));
            }
            int count = points.Count;
            for (int i = 0; i < count; i++)
            {
                points.Add(Vector<double>.Build.DenseOfArray(new double[] { points[i][0], points[i][1], points[i][2] + height }));
            }
        }

        // применение матриц поворота
        public void applyingRotationMatrix(List<Vector<double>> points)
        {
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = points[i] * RotationMatrixX(alphaAngel) * RotationMatrixY(betaAngel) * RotationMatrixZ(gammaAngel);
            }
            for (int i = 0; i < points.Count; i++)
            {
                //points[i] = Vector<double>.Build.DenseOfArray(new double[] { points[i][0] + center.X, points[i][1] + center.Y, points[i][2] + center.Z });
                points[i] += Vector<double>.Build.DenseOfArray(new double[] { center.X, center.Y, center.Z });
            }
        }

        // заполнение вершин призмы, потом заполнения пространства между ними точками
        private static void FillingListPoints(PrismModel prismModel)
        {
            prismModel.pointsOfPrism = new();
            List<(Vector<double>, Vector<double>)> p = new()
                {
                    (prismModel.points[0], prismModel.points[1]),
                    (prismModel.points[1], prismModel.points[2]),
                    (prismModel.points[2], prismModel.points[3]),
                    (prismModel.points[3], prismModel.points[4]),
                    (prismModel.points[4], prismModel.points[5]),
                    (prismModel.points[5], prismModel.points[0]),

                    (prismModel.points[6], prismModel.points[7]),
                    (prismModel.points[7], prismModel.points[8]),
                    (prismModel.points[8], prismModel.points[9]),
                    (prismModel.points[9], prismModel.points[10]),
                    (prismModel.points[10], prismModel.points[11]),
                    (prismModel.points[11], prismModel.points[6]),

                    (prismModel.points[0], prismModel.points[6]),
                    (prismModel.points[1], prismModel.points[7]),
                    (prismModel.points[2], prismModel.points[8]),
                    (prismModel.points[3], prismModel.points[9]),
                    (prismModel.points[4], prismModel.points[10]),
                    (prismModel.points[5], prismModel.points[11])
                };

            foreach (var item in p)
            {
                int numberOfPoint = 100;


                double stepSize = 1.0 / (numberOfPoint - 1);

                for (int i = 0; i < numberOfPoint; i++)
                {
                    double t = i * stepSize;
                    var interpolatedPoint = item.Item1 * (1 - t) + item.Item2 * t;
                    prismModel.pointsOfPrism.Add(interpolatedPoint);
                }
            }

        }

        public static void FillingListPlanes(PrismModel prismModel)
        {
            prismModel.planes = new()
                {
                    (new Plane(prismModel.points[0].ToPoint(), prismModel.points[1].ToPoint(), prismModel.points[2].ToPoint()) * -1, new Plane(prismModel.points[11].ToPoint(), prismModel.points[10].ToPoint(), prismModel.points[9].ToPoint())),
                    (new Plane(prismModel.points[0].ToPoint(), prismModel.points[1].ToPoint(), prismModel.points[6].ToPoint()) * -1,new Plane(prismModel.points[3].ToPoint(), prismModel.points[4].ToPoint(), prismModel.points[9].ToPoint()) ),
                    (new Plane(prismModel.points[1].ToPoint(), prismModel.points[2].ToPoint(), prismModel.points[7].ToPoint()) * -1,new Plane(prismModel.points[4].ToPoint(), prismModel.points[5].ToPoint(), prismModel.points[10].ToPoint()) ),
                    (new Plane(prismModel.points[2].ToPoint(), prismModel.points[3].ToPoint(), prismModel.points[8].ToPoint()) * -1,new Plane(prismModel.points[5].ToPoint(), prismModel.points[0].ToPoint(), prismModel.points[11].ToPoint()) )
                };
        }

        public static bool IsPrismIntersection(PrismModel prism, PrismModel prism1)
        {
            //промежуточное вычисления, чтобы отсеять точно неправильные варианты

            if ((prism.maxDistance + prism1.maxDistance) < (Point.DistantToPoint(prism1.center, prism.center)))
                return false;

            if (Point.DistantToPoint(prism1.center, prism.center) <= (prism1.width < prism1.height ? prism1.width : prism1.height) / 2.0 + (prism.width < prism.height ? prism.width : prism.height) / 2.0)
                return true;


            if (prism.pointsOfPrism == null)
                FillingListPoints(prism);

            if (prism1.pointsOfPrism == null)
                FillingListPoints(prism1);

            if (prism.planes == null)
                FillingListPlanes(prism);

            if (prism1.planes == null)
                FillingListPlanes(prism1);


            foreach (var point in prism.pointsOfPrism!)
            {
                if (Plane.IsPointBetweenPlanes(prism1.planes[0].Item1, prism1.planes[0].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(prism1.planes[1].Item1, prism1.planes[1].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(prism1.planes[2].Item1, prism1.planes[2].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(prism1.planes[3].Item1, prism1.planes[3].Item2, point.ToPoint()))
                {
                    return true;
                }
            }
            foreach (var point in prism1.pointsOfPrism!)
            {
                if (Plane.IsPointBetweenPlanes(prism.planes[0].Item1, prism.planes[0].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(prism.planes[1].Item1, prism.planes[1].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(prism.planes[2].Item1, prism.planes[2].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(prism.planes[3].Item1, prism.planes[3].Item2, point.ToPoint()))
                {
                    return true;
                }
            }
            return false;

        }

        public static void UnDispose(PrismModel prism)
        {
            prism.FillingListPlanes();
            prism.FillingListPoints();
        }

        public static void Dispose(PrismModel prism)
        {
            prism.pointsOfPrism = null;
            prism.planes = null;
        }

        public static PrismModel Copy(PrismModel prismModel)
        {
            lock (lockObject)
            {
                return new PrismModel(prismModel.center, prismModel.alphaAngel, prismModel.betaAngel, prismModel.gammaAngel, prismModel.height, prismModel.width);
            }
        }

        public void FillingListPlanes()
        {
            this.planes = new()
                {
                    (new Plane(this.points[0].ToPoint(), this.points[1].ToPoint(), this.points[2].ToPoint()) * -1,new Plane(this.points[11].ToPoint(),this.points[10].ToPoint(),this.points[9].ToPoint())),
                    (new Plane(this.points[0].ToPoint(), this.points[1].ToPoint(), this.points[6].ToPoint()) * -1,new Plane(this.points[3].ToPoint(), this.points[4].ToPoint(), this.points[9].ToPoint()) ),
                    (new Plane(this.points[1].ToPoint(), this.points[2].ToPoint(), this.points[7].ToPoint()) * -1,new Plane(this.points[4].ToPoint(), this.points[5].ToPoint(), this.points[10].ToPoint()) ),
                    (new Plane(this.points[2].ToPoint(), this.points[3].ToPoint(), this.points[8].ToPoint()) * -1,new Plane(this.points[5].ToPoint(), this.points[0].ToPoint(), this.points[11].ToPoint()) )
                };
        }

        public void FillingListPoints()
        {
            this.pointsOfPrism = new();
            List<(Vector<double>, Vector<double>)> p = new()
                {
                    (this.points[0], this.points[1]),
                    (this.points[1], this.points[2]),
                    (this.points[2], this.points[3]),
                    (this.points[3], this.points[4]),
                    (this.points[4], this.points[5]),
                    (this.points[5], this.points[0]),

                    (this.points[6], this.points[7]),
                    (this.points[7], this.points[8]),
                    (this.points[8], this.points[9]),
                    (this.points[9], this.points[10]),
                    (this.points[10], this.points[11]),
                    (this.points[11], this.points[6]),

                    (this.points[0], this.points[6]),
                    (this.points[1], this.points[7]),
                    (this.points[2], this.points[8]),
                    (this.points[3], this.points[9]),
                    (this.points[4], this.points[10]),
                    (this.points[5], this.points[11])
                };

            foreach (var item in p)
            {
                int numberOfPoint = 100;


                double stepSize = 1.0 / (numberOfPoint - 1);

                for (int i = 0; i < numberOfPoint; i++)
                {
                    double t = i * stepSize;
                    var interpolatedPoint = item.Item1 * (1 - t) + item.Item2 * t;
                    this.pointsOfPrism.Add(interpolatedPoint);
                }
            }
        }

        //[MeasureTimeAspect]
        public bool IsPrismIntersection(PrismModel pm)
        {
            if ((this.maxDistance + pm.maxDistance) < (Point.DistantToPoint(pm.center, this.center)))
                return false;

            if (Point.DistantToPoint(pm.center, this.center) <= (pm.width < pm.height ? pm.width : pm.height) / 2.0 + (this.width < this.height ? this.width : this.height) / 2.0)
                return true;


            if (pm.pointsOfPrism == null)
                pm.FillingListPoints();

            if (this.pointsOfPrism == null)
                this.FillingListPoints();

            if (pm.planes == null)
                pm.FillingListPlanes();

            if (this.planes == null)
                this.FillingListPlanes();


            foreach (var point in pm.pointsOfPrism)
            {
                if (Plane.IsPointBetweenPlanes(this.planes[0].Item1, this.planes[0].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(this.planes[1].Item1, this.planes[1].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(this.planes[2].Item1, this.planes[2].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(this.planes[3].Item1, this.planes[3].Item2, point.ToPoint()))
                {
                    pm.pointsOfPrism = null;
                    this.pointsOfPrism = null;
                    return true;
                }
            }
            foreach (var point in this.pointsOfPrism)
            {
                if (Plane.IsPointBetweenPlanes(pm.planes[0].Item1, pm.planes[0].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(pm.planes[1].Item1, pm.planes[1].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(pm.planes[2].Item1, pm.planes[2].Item2, point.ToPoint()) &&
                    Plane.IsPointBetweenPlanes(pm.planes[3].Item1, pm.planes[3].Item2, point.ToPoint()))
                {
                    pm.pointsOfPrism = null;
                    this.pointsOfPrism = null;
                    return true;
                }
            }
            pm.pointsOfPrism = null;
            this.pointsOfPrism = null;
            return false;
        }

        public override string ToString()
        {
            //return $"{center.X:0.00}\t{center.Y:0.00}\t{center.Z:0.00}\t{width}\t{height}\t{alphaAngel:0.00}\t{betaAngel:0.00}\t{gammaAngel:0.00}";
            return $"{center.X:0.00} {center.Y:0.00} {center.Z:0.00} {width} {height} {alphaAngel:0.00} {betaAngel:0.00} {gammaAngel:0.00}";
            var strBuild = new StringBuilder();
            foreach (var item in points)
            {
                strBuild.Append(item.ToPoint());
            }
            return strBuild.ToString();

            //return "new PrismModel(new Point(" + $"{center.X:0.00}".Replace(',', '.') + ',' + $"{center.Y:0.00}".Replace(',', '.') +
            //        ',' + $"{center.Z:0.00})".Replace(',', '.') + ',' + $"{alphaAngel:0.00}".Replace(',', '.') + ',' + $"{betaAngel:0.00}".Replace(',', '.')
            //        + ',' + $"{gammaAngel:0.00}".Replace(',', '.') + $",{height},{width});";
        }

        //матрица поворота на угл angl
        public static Matrix<double> RotationMatrixZ(double angl)
        {
            return Matrix<double>.Build.DenseOfArray(new double[,]
            {
                {Math.Cos(Math.PI * angl / 180), -Math.Sin(Math.PI * angl / 180), 0},
                {Math.Sin(Math.PI * angl / 180), Math.Cos(Math.PI * angl / 180), 0},
                {0, 0, 1}
            });
        }

        //матрица поворота на угл angl
        public static Matrix<double> RotationMatrixX(double angl)
        {
            return Matrix<double>.Build.DenseOfArray(new double[,]
            {
                {1, 0, 0},
                {0, Math.Cos(Math.PI * angl / 180), -Math.Sin(Math.PI * angl / 180)},
                {0, Math.Sin(Math.PI * angl / 180), Math.Cos(Math.PI * angl / 180)}
            });
        }

        //матрица поворта на угл angl
        public static Matrix<double> RotationMatrixY(double angl)
        {
            return Matrix<double>.Build.DenseOfArray(new double[,]
            {
                {Math.Cos(Math.PI * angl / 180), 0, Math.Sin(Math.PI * angl / 180)},
                {0, 1, 0},
                {-Math.Sin(Math.PI * angl / 180), 0, Math.Cos(Math.PI * angl / 180)}
            });
        }
    }
}
