using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace CheckGenerationPrism
{
    public class OctreeNode
    {
        public Point Center { get; private set; }
        public double HalfSize { get; private set; }
        public OctreeNode[] Children { get; private set; }
        public List<PrismModel> Prisms { get; private set; }
        public double nc {  get; private set; }
        public double volume { get; private set; }
        public bool CanAddPrism { get; private set; }

        public OctreeNode(Point center, double halfSize, double nc)
        {
            CanAddPrism = true;
            Center = center;
            HalfSize = halfSize;
            Children = new OctreeNode[8];
            Prisms = new List<PrismModel>();
            this.nc = nc;
        }

        public bool IsLeaf() => Children.All(child => child == null);

        public void PrismsAdd(PrismModel prism)
        {
            Prisms.Add(prism);
            volume += prism.V;
            Task.Run(async () => 
            {
                if (volume / Math.Pow((HalfSize * 2), 3) > nc)
                {
                    CanAddPrism = false;
                }
            });
            
        }
    }

    public class Octree
    {
        public OctreeNode root;
        private int maxDepth;
        private double nc;
        

        public Octree(Point center, double halfSize, int maxDepth, double nc)
        {
            root = new OctreeNode(center, halfSize, nc);
            this.maxDepth = maxDepth;
            this.nc = nc;
        }

        public List<PrismModel> GetPrisms()
        {
            List<PrismModel> prisms = new List<PrismModel>();
            GetPrisms(root, prisms, 1);
            return prisms;
        }

        private void GetPrisms(OctreeNode node, List<PrismModel> prisms, int depth)
        {
            if (depth == maxDepth)
            {
                prisms.AddRange(node.Prisms);
            }
            else
            {
                foreach (OctreeNode child in node.Children)
                {
                    if (child != null)
                    {
                        GetPrisms(child, prisms, depth + 1);
                    }
                }
            }
        }

        public void Insert(PrismModel prism)
        {
            Insert(root, prism, 1);
        }

        public bool InsertBool(PrismModel prism)
        {
            return InsertBool(root, prism, 1);
        }

        public bool InsertBool2(OctreeNode node, PrismModel prism, int depth)
        {
            return InsertBool(node, prism, depth);
        }

        public void InitializeNodes(OctreeNode node, int currentDepth=1)
        {
            if (currentDepth >= maxDepth)
                return;

            double quarterSize = node.HalfSize / 2;

            for (int i = 0; i < 8; i++)
            {
                Point offset = new Point(
                    (i & 4) != 0 ? quarterSize : -quarterSize,
                    (i & 2) != 0 ? quarterSize : -quarterSize,
                    (i & 1) != 0 ? quarterSize : -quarterSize
                );

                Point childCenter = node.Center + offset;
                node.Children[i] = new OctreeNode(childCenter, quarterSize, i);// Рекурсивная инициализация дочерних узлов
                InitializeNodes(node.Children[i], currentDepth + 1);
            }
        }

        private bool InsertBool(OctreeNode node, PrismModel prism, int depth)
        {
            if (depth == maxDepth)
            {
                /*
                // Строгая проверка, если призма точно не пересекается с призмами из других нод
                //if ((prism.center.X < (node.Center.X + node.HalfSize - maxDistPrism) &&
                //    prism.center.X > (node.Center.X - node.HalfSize + maxDistPrism)) &&

                //    (prism.center.Y < (node.Center.Y + node.HalfSize - maxDistPrism) &&
                //    prism.center.Y > (node.Center.Y - node.HalfSize + maxDistPrism)) &&

                //    (prism.center.Z < (node.Center.Z + node.HalfSize - maxDistPrism) &&
                //    prism.center.Z > (node.Center.Z - node.HalfSize + maxDistPrism)))
                //{
                //    //bool flag = TryAddOneNode(prism, node);
                //    //if (flag != CheckIntersectionPrismService.Check(root.Prisms, prism, flag))
                //    //{
                //    //    CheckIntersectionPrismService.Check(root.Prisms, prism, flag);
                //    //    TryAddOneNode(prism, node);
                //    //}
                //    return TryAddOneNode(prism, node);
                //}


                //var tlistPrisms = GetNeighbourNodes(prism, node.HalfSize);
                //HashSet<PrismModel> prisms = new HashSet<PrismModel>();
                //foreach (var octreeNode in tlistPrisms)
                //{
                //    if (octreeNode.Prisms.Count != 0)
                //    {
                //        prisms.UnionWith(octreeNode.Prisms);
                //    }
                //}
                //bool flag2 = TryAddMoreNode(node, prism, tlistPrisms);
                //if (flag2 != CheckIntersectionPrismService.Check(root.Prisms, prism, flag2))
                //{
                //    CheckIntersectionPrismService.Check(root.Prisms, prism, flag2);
                //    TryAddMoreNode(node, prism, tlistPrisms);
                //}
                //return flag2;
                */

                if (!node.CanAddPrism)
                {
                    return false;
                }

                var tlistPrisms = GetNeighbourNodes(prism, node.HalfSize);
                return TryAddMoreNode(node, prism, tlistPrisms);

            }
            // Определяем индекс дочернего узла
            int index = 0;
            if (prism.center.X >= node.Center.X) index |= 4;
            if (prism.center.Y >= node.Center.Y) index |= 2;
            if (prism.center.Z >= node.Center.Z) index |= 1;

            // Если дочерний узел не существует, создаем его
            if (node.Children[index] == null)
            {
                double quarterSize = node.HalfSize / 2;
                Point offset = new Point(
                    (index & 4) == 4 ? quarterSize : -quarterSize,
                    (index & 2) == 2 ? quarterSize : -quarterSize,
                    (index & 1) == 1 ? quarterSize : -quarterSize
                );

                Point childCenter = node.Center + offset;
                node.Children[index] = new OctreeNode(childCenter, quarterSize, nc);
            }
            return InsertBool(node.Children[index], prism, depth + 1);
            // Рекурсивно добавляем точку в нужный дочерний узел
            //node.Children[index].Data.Add(point);
        }

        private void Insert(OctreeNode node, PrismModel prism, int depth)
        {
            if (depth == maxDepth)
            {
                // Проверка призм на пересечение в конкретной ноде
                node.Prisms.Add(prism);
                return;
            }
            // Определяем индекс дочернего узла
            int index = 0;
            if (prism.center.X >= node.Center.X) index |= 4;
            if (prism.center.Y >= node.Center.Y) index |= 2;
            if (prism.center.Z >= node.Center.Z) index |= 1;

            // Если дочерний узел не существует, создаем его
            if (node.Children[index] == null)
            {
                double quarterSize = node.HalfSize / 2;
                Point offset = new Point(
                    (index & 4) == 4 ? quarterSize : -quarterSize,
                    (index & 2) == 2 ? quarterSize : -quarterSize,
                    (index & 1) == 1 ? quarterSize : -quarterSize
                );

                Point childCenter = node.Center + offset;
                node.Children[index] = new OctreeNode(childCenter, quarterSize, nc);
            }
            Insert(node.Children[index], prism, depth + 1);
            // Рекурсивно добавляем точку в нужный дочерний узел
            //node.Children[index].Data.Add(point);
        }

        private bool TryAddOneNode(PrismModel pm, OctreeNode node)
        {
            if (node == null)
                throw new Exception("TryAddOneNode, node = null");
            if (node.Prisms.Count == 0)
            {
                node.PrismsAdd(pm);
                return true;
            }

            if (!IsPrismIntersection(pm, node.Prisms))
            {
                node.PrismsAdd(pm);
                return true;
            }

            return false;
        }

        private bool TryAddMoreNode(OctreeNode node, PrismModel prismModel, List<OctreeNode> octreeNodes)
        {
            if (octreeNodes == null || octreeNodes.Count == 0)
                throw new Exception("TryAddMoreNode, node = null or node.Count == 0");

            HashSet<PrismModel> prisms = new HashSet<PrismModel>();
            foreach (var octreeNode in octreeNodes)
            {
                if (octreeNode.Prisms.Count != 0)
                {
                    prisms.UnionWith(octreeNode.Prisms);
                }
            }

            if (prisms.Count == 0)
            {
                node.PrismsAdd(prismModel);
                return true;
            }

            if (!IsPrismIntersection(prismModel, prisms.ToList()))
            {
                node.PrismsAdd(prismModel);
                return true;
            }

            return false;
        }

        private bool IsPrismIntersection(PrismModel pmc, List<PrismModel> prisms)
        {
            return IsPrismIntersectionWithOutTask(pmc, prisms);
            /*
            if (prisms.Count < 10000)
            {
                var t = IsPrismIntersectionWithOutTask(pmc, prisms);   
                return t;
                //return IsPrismIntersectionWithOutTask(pmc, prisms);
            }
            var cancellToken = new CancellationTokenSource();
            var stepOne = prisms.Count / 8;
            bool flag = false;

            var tasks = new Task[8];
            tasks[0] = new Task(() =>
            {
                var pm = PrismModel.Copy(pmc);
                for (int i = 0; i <= stepOne; i++)
                {
                    if (cancellToken.IsCancellationRequested)
                        break;

                    if (prisms[i].IsPrismIntersection(pm))
                    {
                        flag = true;
                        cancellToken.Cancel();
                        break;
                    }
                }
            });
            tasks[1] = new Task(() =>
            {
                var pm = PrismModel.Copy(pmc);
                for (int i = stepOne + 1; i <= stepOne * 2; i++)
                {
                    if (cancellToken.IsCancellationRequested)
                        break;

                    if (prisms[i].IsPrismIntersection(pm))
                    {
                        flag = true;
                        cancellToken.Cancel();
                        break;
                    }
                }
            });
            tasks[2] = new Task(() =>
            {
                var pm = PrismModel.Copy(pmc);
                for (int i = stepOne * 2 + 1; i <= stepOne * 3; i++)
                {
                    if (cancellToken.IsCancellationRequested)
                        break;

                    if (prisms[i].IsPrismIntersection(pm))
                    {
                        flag = true;
                        cancellToken.Cancel();
                        break;
                    }
                }
            });
            tasks[3] = new Task(() =>
            {
                var pm = PrismModel.Copy(pmc);
                for (int i = stepOne * 3 + 1; i <= stepOne * 4; i++)
                {
                    if (cancellToken.IsCancellationRequested)
                        break;

                    if (prisms[i].IsPrismIntersection(pm))
                    {
                        flag = true;
                        cancellToken.Cancel();
                        break;
                    }
                }
            });
            tasks[4] = new Task(() =>
            {
                var pm = PrismModel.Copy(pmc);
                for (int i = stepOne * 4 + 1; i <= stepOne * 5; i++)
                {
                    if (cancellToken.IsCancellationRequested)
                        break;

                    if (prisms[i].IsPrismIntersection(pm))
                    {
                        flag = true;
                        cancellToken.Cancel();
                        break;
                    }
                }
            });
            tasks[5] = new Task(() =>
            {
                var pm = PrismModel.Copy(pmc);
                for (int i = stepOne * 5 + 1; i <= stepOne * 6; i++)
                {
                    if (cancellToken.IsCancellationRequested)
                        break;

                    if (prisms[i].IsPrismIntersection(pm))
                    {
                        flag = true;
                        cancellToken.Cancel();
                        break;
                    }
                }
            });
            tasks[6] = new Task(() =>
            {
                var pm = PrismModel.Copy(pmc);
                for (int i = stepOne * 6 + 1; i <= stepOne * 7; i++)
                {
                    if (cancellToken.IsCancellationRequested)
                        break;

                    if (prisms[i].IsPrismIntersection(pm))
                    {
                        flag = true;
                        cancellToken.Cancel();
                        break;
                    }
                }
            });
            tasks[7] = new Task(() =>
            {
                var pm = PrismModel.Copy(pmc);
                for (int i = stepOne * 7 + 1; i <= prisms.Count - 1; i++)
                {
                    if (cancellToken.IsCancellationRequested)
                        break;

                    if (prisms[i].IsPrismIntersection(pm))
                    {
                        flag = true;
                        cancellToken.Cancel();
                        break;
                    }
                }
            });

            foreach (var item in tasks)
                item.Start();
            Task.WaitAll(tasks);

            return flag;*/
        }

        private bool IsPrismIntersectionWithOutTask(PrismModel pm, List<PrismModel> prisms)
        {
            // 1  вариант
            //foreach (var prism in prisms)
            //{
            //    var t = pm.IsPrismIntersection(prism);
            //    if (t)
            //        return true;
            //}
            //return false;

            // 2 вариант
            foreach (var prism in prisms)
            {
                var t = PrismModel.IsPrismIntersection(pm, prism);
                if (t)
                {
                    PrismModel.Dispose(pm);
                    PrismModel.Dispose(prism);
                    return true;
                }
                PrismModel.Dispose(prism);
            }
            PrismModel.Dispose(pm);
            return false;

            // 3 вариант баг
            //bool flag = false;
            //Parallel.ForEach(prisms, (prism, state) =>
            //{
            //    if(PrismModel.IsPrismIntersection(pm, prism))
            //    {
            //        PrismModel.Dispose(prism);
            //        flag = true;
            //        state.Stop();
            //    }
            //    PrismModel.Dispose(prism);
            //});
            //PrismModel.Dispose(pm);
            //return flag;

            // 4 вариант
            //PrismModel.UnDispose(pm);
            //bool intersectionFound = prisms.AsParallel().Any(prism =>
            //{
            //    if (PrismModel.IsPrismIntersection(pm, prism))
            //    {
            //        PrismModel.Dispose(prism);
            //        return true;
            //    }
            //    PrismModel.Dispose(prism);
            //    return false;
            //});
            //PrismModel.Dispose(pm);
            //return intersectionFound;
        }

        private bool TryAddMoreNode2(OctreeNode node, PrismModel prismModel, List<OctreeNode> octreeNodes)
        {
            if (octreeNodes == null || octreeNodes.Count == 0)
                throw new Exception("TryAddMoreNode, node = null or node.Count == 0");

            // 1 вариант
            //var results = new ConcurrentBag<bool>();

            //Parallel.ForEach(octreeNodes, (octreeNode, state) =>
            //{
            //    var tmp = PrismModel.Copy(prismModel);

            //    if (IsPrismIntersectionWithOutTask(tmp, octreeNode.Prisms))
            //    {
            //        results.Add(true);
            //        state.Stop(); // Прекращаем дальнейшую обработку
            //    }
            //});

            //if (results.Contains(true))
            //{
            //    return false;
            //}

            // 2 вариант
            //bool intersectionFound = octreeNodes.AsParallel().Any(octreeNode =>
            //{
            //    var tmp = PrismModel.Copy(prismModel);
            //    return IsPrismIntersectionWithOutTask(tmp, octreeNode.Prisms);
            //});
            //if (intersectionFound)
            //{
            //    return false;
            //}

            // 3 вариант
            //Task<bool>[] tasks = new Task<bool>[octreeNodes.Count];

            //for (int i = 0; i < octreeNodes.Count; i++)
            //{
            //    var t = i;
            //    var tmp = PrismModel.Copy(prismModel);
            //    tasks[i] = Task.Run(() => IsPrismIntersectionWithOutTask(tmp, octreeNodes[t].Prisms));
            //}
            //Task.WaitAll(tasks);
            //bool[] results = Array.ConvertAll(tasks, task => task.Result);
            //foreach (var item in results)
            //{
            //    if (item)
            //    {
            //        return false;
            //    }
            //}

            node.Prisms.Add(prismModel);
            return true;
        }

        private List<OctreeNode> GetNeighbourNodes(PrismModel pm, double halfSize)
        {
            var center = pm.center;
            double X = pm.center.X;
            double Y = pm.center.Y;
            double Z = pm.center.Z;

            var nodes = new HashSet<OctreeNode>
            {
                FindeNode(root, center, 1)
            };
            double w = 15;
            Point[] points = new Point[] {
                new Point(X+w,Y+w,Z+w),
                new Point(X+w,Y+w,Z-w),
                new Point(X+w,Y-w,Z+w),
                new Point(X+w,Y-w,Z-w),
                new Point(X-w,Y+w,Z+w),
                new Point(X-w,Y+w,Z-w),
                new Point(X-w,Y-w,Z+w),
                new Point(X-w,Y-w,Z-w),
            };
            foreach (var point in points)
            {
                var tnode = FindeNode(root, point, 1);
                nodes.Add(tnode);

            }
            return nodes.ToList();
        }

        private List<OctreeNode> GetNeighbourNodes(PrismModel pm)
        {
            var nodes = new HashSet<OctreeNode>
            {
                FindeNode(root, pm.center, 1)
            };
            foreach (var point in pm.points)
            {
                var t = point.ToPoint();//Point.PointPlusD(pm.center, point.ToPoint(), 1);
                var tnode = FindeNode(root, t, 1);
                nodes.Add(tnode);

            }
            return nodes.ToList();
        }
        /*
        public List<OctreeNode> GetNeighbourNodes(OctreeNode node)
        {
            if (node.NeighbourNodes != null)
            {
                return node.NeighbourNodes.ToList();
            }
            node.NeighbourNodes = new HashSet<OctreeNode>();
            double[] offsets = { -node.HalfSize, 0, node.HalfSize };

            foreach (var dx in offsets)
            {
                foreach (var dy in offsets)
                {
                    foreach (var dz in offsets)
                    {
                        // Вычисляем координаты соседнего сектора
                        double newX = node.Center.X + dx * node.HalfSize;
                        double newY = node.Center.Y + dy * node.HalfSize;
                        double newZ = node.Center.Z + dz * node.HalfSize;

                        // Создаем новый сектор и добавляем его в список
                        var t = FindeNode(root, new Point(newX, newY, newZ), 1);
                        node.NeighbourNodes.Add(t);
                    }
                }
            }
            return node.NeighbourNodes.ToList();
        }
        */

        private OctreeNode FindeNode(OctreeNode node, Point point, int depth)
        {
            if (depth == maxDepth)
            {
                return node;
            }
            // Определяем индекс дочернего узла
            int index = 0;
            if (point.X >= node.Center.X) index |= 4;
            if (point.Y >= node.Center.Y) index |= 2;
            if (point.Z >= node.Center.Z) index |= 1;

            // Если дочерний узел не существует, создаем его
            if (node.Children[index] == null)
            {
                double quarterSize = node.HalfSize / 2;
                Point offset = new Point(
                    (index & 4) == 4 ? quarterSize : -quarterSize,
                    (index & 2) == 2 ? quarterSize : -quarterSize,
                    (index & 1) == 1 ? quarterSize : -quarterSize
                );

                Point childCenter = node.Center + offset;
                node.Children[index] = new OctreeNode(childCenter, quarterSize, nc);
            }
            return FindeNode(node.Children[index], point, depth + 1);
            // Рекурсивно добавляем точку в нужный дочерний узел
            //node.Children[index].Data.Add(point);
        }
    }
}
