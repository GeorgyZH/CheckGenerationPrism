using System.Diagnostics;
using System.Timers;
using static System.Collections.Specialized.BitVector32;
using Timer = System.Timers.Timer;

namespace CheckGenerationPrism
{
    internal class Program
    {
        public static PrismModel GenerateRandomPrism(double LimitedSize, int sizeLow, int sizeMax)
        {
            var randomWidth = new Random().Next(sizeLow, sizeMax);
            var randomHeight = new Random().Next(sizeLow, sizeMax);
            var centerPrism = GeneratePoint("cub", LimitedSize, 1);
            var prism = new PrismModel(
                centerPrism,
                alpha: new Random().NextDouble() * 180,
                gamma: new Random().NextDouble() * 180,
                beta: new Random().NextDouble() * 180,
                width: randomWidth,
                height: randomHeight);
            return prism;
        }

        public static Point GeneratePoint(string? typeDistributionXYZ, double limitedSize, int genSphere)
        {
            double rndPX;
            double rndPY;
            double rndPZ;

            switch (typeDistributionXYZ)
            {
                case "cub":
                    rndPX = (2 * new Random().NextDouble() - 1) * limitedSize;
                    rndPY = (2 * new Random().NextDouble() - 1) * limitedSize;
                    rndPZ = (2 * new Random().NextDouble() - 1) * limitedSize;
                    break;
                case "sphere":
                    do
                    {
                        rndPX = (2 * new Random().NextDouble() - 1) * limitedSize;
                        rndPY = (2 * new Random().NextDouble() - 1) * limitedSize;
                        rndPZ = (2 * new Random().NextDouble() - 1) * limitedSize;
                        genSphere--;
                    }
                    while ((rndPX * rndPX + rndPY * rndPY + rndPZ * rndPZ) <= limitedSize * limitedSize && genSphere > 0);
                    break;
                default:
                    throw new Exception("The wrong distribution");
            }

            return new Point(rndPX, rndPY, rndPZ);
        }

        public static void HasPrismIntersection(List<PrismModel> list)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    if (list[i].IsPrismIntersection(list[j]))
                    {
                        Console.WriteLine("Intersection:");
                        Console.WriteLine($"{list[i]}");
                        Console.WriteLine($"{list[j]}");
                        Console.WriteLine(new String('-', 100));
                    }
                }
                //Console.WriteLine(i);
            }

        }

        public static Octree GeneratePrisms(ulong Num, double Nc, int SizeMax, int SizeLow, int sections)
        {
            var timer = new Timer() { Interval = 5000 };
            
            double maxVPrism = (((6 * (SizeMax / 2) * (SizeMax / 2)) / 2) * Math.Sin(Math.PI / 3)) * SizeMax;

            int midleSize = (SizeMax - SizeLow) / 2 + SizeLow;
            double middleVPrism = (((6 * (midleSize / 2) * (midleSize / 2)) / 2) * Math.Sin(Math.PI / 3)) * midleSize;

            double LimitedSize = Math.Pow(maxVPrism * Num * 100 / (Nc * 100), 1.0 / 3) / 2;
            var maxDistPrism = Point.InvSqrt(Math.Pow(SizeMax / 2, 2) + Math.Pow(SizeMax / 2, 2));
            var octree = new Octree(new Point(0, 0, 0), LimitedSize, sections, maxDistPrism);

            int tryCount = 0;
            ulong i = 0;
            timer.Elapsed += (o, e) =>
            {
                Console.WriteLine($"prisms: {i}\t/\t{Num}");
            };
            //timer.Start();
            for (i = 0; i < Num && tryCount<1000;)
            {
                var prism = GenerateRandomPrism(LimitedSize, SizeLow, SizeMax);
                if (octree.InsertBool(prism))
                {
                    i++;
                    tryCount = 0;   
                }
                else
                {
                    tryCount++;
                }
            }
            timer.Stop();
            return octree;//.GetPrisms();
        }
        public static void StartGeneration(ulong Num, double Nc, int SizeMax, int SizeLow, int sections)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Console.WriteLine("Start");
            var prisms = GeneratePrisms(Num, Nc, SizeMax, SizeLow, sections);
            sw.Stop();
            Console.WriteLine($"End at {sw.Elapsed.TotalSeconds}");           
            //Console.WriteLine("All");
        }
        public class Data
        {
            public ulong Num;
            public double Nc;
            public int SizeMax;
            public int SizeLow;
            public int Sections;
            public override string ToString()
            {
                return $"Num:{Num}, Nc:{Nc},SizeMax:{SizeMax},SizeLow:{SizeLow},Sections:{Sections}";
            }
        }
        static void Main(string[] args)
        {
            //var timer = new Timer() { Interval = 5000 };
            //timer.Elapsed += (o, e) =>
            //{
            //    Console.WriteLine("elapsed");
            //};
            //new Data() { Num = 10000, Nc = 0.2, SizeMax = 12, SizeLow = 2 };
            ulong Num = 1_00_000;
            double Nc = 0.2;
            int SizeMax = 12;
            int SizeLow = 2;
            //Data[] datas = new Data[] {
            //new Data() { Num = 10_000_000, Nc = 0.2, SizeMax = 12, SizeLow = 2, Sections = 7 },
            //new Data() { Num = 10_000_000, Nc = 0.2, SizeMax = 12, SizeLow = 2, Sections = 7 },
            //new Data() { Num = 10_000_000, Nc = 0.2, SizeMax = 12, SizeLow = 2, Sections = 8 },
            //new Data() { Num = 10_000_000, Nc = 0.2, SizeMax = 12, SizeLow = 2, Sections = 8 },
            //};

            //foreach (var item in datas)
            //{
            //    Console.WriteLine(item);
            //    StartGeneration(item.Num, item.Nc, item.SizeMax, item.SizeLow, item.Sections);
            //    Console.WriteLine(new String('-', 100));
            //}

            Stopwatch sw = Stopwatch.StartNew();
            Console.WriteLine("Start");
            var octree = GeneratePrisms(Num, Nc, SizeMax, SizeLow, 5);
            sw.Stop();
            Console.WriteLine($"End at {sw.Elapsed.TotalSeconds}");

            var nodes = octree.GetNodes();
            double vnode = Math.Pow(nodes[0].HalfSize * 2,3);

            double maxRatio = double.MinValue;
            Console.WriteLine("Ratio node: "+vnode);
            foreach (var item in octree.GetNodes())
            {
                var t = item.prismsVolume / vnode;
                if (t > maxRatio)
                {
                    Console.WriteLine($"prism in node: {item.PrismsCount()} \t ratio prism: {item.prismsVolume} \t ratio: "+t);

                    maxRatio = t;
                }
            }

            //Console.WriteLine("Check intersection");
            //HasPrismIntersection(prisms);
            Console.WriteLine("All");
            Console.ReadKey();


        }
    }
}
