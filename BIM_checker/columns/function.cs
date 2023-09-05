using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace columns
{
    class line
    {
        public double a;
        public double b;
        public double c;
        public XYZ sp;
        public XYZ ep;
        public XYZ unit_vector;
        public double length;
    }
    //提取wall中的坐标和直线
    class basic
    {

        #region Solid method
        
        #endregion


        public static line GetLine(Wall wall)
        {
            line segment = new line();
            try
            {
                LocationCurve lcurve = wall.Location as LocationCurve;
                Line wallline = lcurve.Curve as Line;
                IList<XYZ> coordinate = wallline.Tessellate();
                segment.sp = coordinate[0];
                segment.ep = coordinate[1];
                segment.length = PointDistance(coordinate[0], coordinate[1]);
                segment.a = segment.sp.Y - segment.ep.Y;
                segment.b = segment.ep.X - segment.sp.X;
                segment.c = segment.sp.X * segment.ep.Y - segment.ep.X * segment.sp.Y;
                segment.unit_vector = wallline.Direction;
            }
            catch
            {
                //todo
            }
            return segment;
        }

        //从两点中获取line
        public static line GetLine(XYZ point1, XYZ point2)
        {
            line segment = new line();
            segment.sp = point1;
            segment.ep = point2;
            segment.a = segment.sp.Y - segment.ep.Y;
            segment.b = segment.ep.X - segment.sp.X;
            segment.c = segment.sp.X * segment.ep.Y - segment.ep.X * segment.sp.Y;
            segment.length = PointDistance(point1, point2);
            XYZ vector = new XYZ(segment.ep.X - segment.sp.X, segment.ep.Y - segment.sp.Y, segment.ep.Z - segment.sp.Z);
            if (Math.Abs(segment.length) > 0001)
            {
                segment.unit_vector = vector / segment.length;
            }
            else
            {
                segment.unit_vector = new XYZ(0, 0, 0);
            }
            return segment;
        }

        //求点到线段的距离
        public static double VertcieDistPointToLine(XYZ point, line line)
        {
            double space = 0;
            double a, b, c;
            a = PointDistance(line.sp, line.ep);// 线段的长度 
            b = PointDistance(line.sp, point); // (x1,y1)到点的距离
            c = PointDistance(line.ep, point); // (x2,y2)到点的距离  
            //点到线段距离  
            if (c <= 0.000001 || b <= 0.000001)
            {
                space = 0;
                return space;
            }
            if (a <= 0.000001)
            {
                space = b;
                return space;
            }
            if (c * c == a * a + b * b)
            {
                space = b;
                return space;
            }
            if (b * b == a * a + c * c)
            {
                space = c;
                return space;
            }
            double p = (a + b + c) / 2;// 半周长    
            double s = Math.Sqrt(p * (p - a) * (p - b) * (p - c));// 海伦公式求面积    
            space = 2 * s / a; // 返回点到线的距离（利用三角形面积公式求高）    
            return space;
        }

        //找到line端点离line_index的垂线最近的点
        public static XYZ FindClosestVerticeDistPoint(line line, line line_index)
        {
            XYZ point1 = line.sp;
            XYZ point2 = line.ep;
            double distance1 = VertcieDistPointToLine(point1, line_index);
            double distance2 = VertcieDistPointToLine(point2, line_index);
            if (distance1 <= distance2)
            {
                return point1;
            }
            else
            {
                return point2;
            }
        }

        public static double FindMinVerticeDist(line line, line line_index)
        {
            XYZ closest = FindClosestVerticeDistPoint(line, line_index);
            double distance = VertcieDistPointToLine(closest, line_index);
            return distance;
        }


        //从line的端点中找到离line_index直线距离最近的点
        public static XYZ FindClosestDirectDistPoint(line line, line line_index)
        {
            List<XYZ> closest_points = FindClosestPoints(line,line_index);
            XYZ closest = line.sp; //初始化
            foreach (XYZ point in closest_points)
            {
                if (point == line.sp || point == line.ep)
                {
                    closest = point;
                }
            }
            return closest;
        }

        //计算两点之间的距离
        public static double PointDistance(XYZ point1, XYZ point2)
        {
            double lineLength = Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y) + (point1.Z - point2.Z) * (point1.Z - point2.Z));
            return lineLength;
        }

        //找两直线距离最近的两个点
        public static List<XYZ> FindClosestPoints(line line1, line line2)
        {
            XYZ point1 = new XYZ();
            XYZ point2 = new XYZ();
            double distance1 = Math.Pow((line1.sp.X - line2.sp.X), 2) + Math.Pow((line1.sp.Y - line2.sp.Y), 2);
            double distance2 = Math.Pow((line1.sp.X - line2.ep.X), 2) + Math.Pow((line1.sp.Y - line2.ep.Y), 2);
            double distance3 = Math.Pow((line1.ep.X - line2.sp.X), 2) + Math.Pow((line1.ep.Y - line2.sp.Y), 2);
            double distance4 = Math.Pow((line1.ep.X - line2.ep.X), 2) + Math.Pow((line1.ep.Y - line2.ep.Y), 2);
            List<double> distance = new List<double>();
            distance.Add(distance1); distance.Add(distance2); distance.Add(distance3); distance.Add(distance4);
            double min_dis = distance.Min();
            List<XYZ> closest_points = new List<XYZ>();
            //if (min_dis >= 0.00001)
            //{
                if (distance1 == min_dis)
                {
                    point1 = line1.sp;
                    point2 = line2.sp;
                }
                else if (distance2 == min_dis)
                {
                    point1 = line1.sp;
                    point2 = line2.ep;
                }
                else if (distance3 == min_dis)
                {
                    point1 = line1.ep;
                    point2 = line2.sp;
                }
                else if (distance4 == min_dis)
                {
                    point1 = line1.ep;
                    point2 = line2.ep;
                }

                closest_points.Add(point1);
                closest_points.Add(point2);
            //}
            //else 距离太近返回空列表 todo: 为什么???
            return closest_points;
        }

        //找两直线距离最近的两个点，返回对应的line和坐标
        public static Dictionary<line,XYZ> FindClosestPointsOfLines(line line1, line line2)
        {
            XYZ point1 = new XYZ();
            XYZ point2 = new XYZ();
            double distance1 = Math.Pow((line1.sp.X - line2.sp.X), 2) + Math.Pow((line1.sp.Y - line2.sp.Y), 2);
            double distance2 = Math.Pow((line1.sp.X - line2.ep.X), 2) + Math.Pow((line1.sp.Y - line2.ep.Y), 2);
            double distance3 = Math.Pow((line1.ep.X - line2.sp.X), 2) + Math.Pow((line1.ep.Y - line2.sp.Y), 2);
            double distance4 = Math.Pow((line1.ep.X - line2.ep.X), 2) + Math.Pow((line1.ep.Y - line2.ep.Y), 2);
            List<double> distance = new List<double>();
            distance.Add(distance1); distance.Add(distance2); distance.Add(distance3); distance.Add(distance4);
            double min_dis = distance.Min();
            Dictionary<line, XYZ> line_point = new Dictionary<line, XYZ>();
            //if (min_dis >= 0.05)
            //{
                if (distance1 == min_dis)
                {
                    point1 = line1.sp;
                    point2 = line2.sp;
                }
                else if (distance2 == min_dis)
                {
                    point1 = line1.sp;
                    point2 = line2.ep;
                }
                else if (distance3 == min_dis)
                {
                    point1 = line1.ep;
                    point2 = line2.sp;
                }
                else if (distance4 == min_dis)
                {
                    point1 = line1.ep;
                    point2 = line2.sp;
                }

                line_point.Add(line1,point1);
                line_point.Add(line2,point2);
            //}
            //else 距离太近返回空列表
            return line_point;
        }


        //从直线中找到离点最近的端点
        public static List<XYZ> FindClosestPoints(line line, XYZ point)
        {
            List<XYZ> closest_points = new List<XYZ>();
            double distance11 = Math.Pow((line.sp.X - point.X), 2) + Math.Pow((line.sp.Y - point.Y), 2);
            double distance12 = Math.Pow((line.ep.X - point.X), 2) + Math.Pow((line.ep.Y - point.Y), 2);
            if (distance11 <= distance12)
            {
                closest_points.Add(line.sp);
                closest_points.Add(point);
            }
            else
            {
                closest_points.Add(line.ep);
                closest_points.Add(point);
            }
            return closest_points;
        }

        //从两点中找到离point_index最近的点
        public static List<XYZ> FindClosestPoints(XYZ point1, XYZ point2, XYZ point_index)
        {
            List<XYZ> closest_points = new List<XYZ>();
            double distance11 = Math.Pow((point1.X - point_index.X), 2) + Math.Pow((point1.Y - point_index.Y), 2);
            double distance12 = Math.Pow((point2.X - point_index.X), 2) + Math.Pow((point2.Y - point_index.Y), 2);
            if (distance11 <= distance12)
            {
                closest_points.Add(point1);
                closest_points.Add(point_index);
            }
            else
            {
                closest_points.Add(point_index);
                closest_points.Add(point_index);
            }
            return closest_points;
        }

        //求平行的两直线的距离
        public static double LineDistance(line line1, line line2, Wall wall1, Wall wall2)
        {
            if (line1.a == 0 && line1.b != 0)
            {
                return Math.Abs(line1.c - line1.b / line2.b * line2.c) / Math.Pow(line1.b, 2);
            }
            else if (line1.a != 0 && line1.b == 0)
            {
                return Math.Abs(line1.c - line1.a / line2.a * line2.c) / Math.Pow(line1.a, 2);
            }
            else if (line1.a != 0.01 && line1.b != 0.01)
            {
                return Math.Abs(line1.c - line1.a / line2.a * line2.c) / (Math.Pow(line1.a, 2) + Math.Pow(line1.b, 2));
            }
            else
            {
                TaskDialog.Show("NEED DEBUG", wall1.Id.ToString() + "/" + wall2.Id.ToString() + ":\n 线段的a、b都等于0");
                return Math.Abs(line1.c - line2.c);
            }
        }

        //找到三条平行线中中间的那条线
        public static line FindMiddleLine(line line1, line line2, line line3)
        {
            //不平行x轴的时候
            if (!(-0.001 <= line1.a & line1.a <= 0.001))
            {
                double c1 = line1.c / line1.a; double c2 = line2.c / line2.a; double c3 = line3.c / line3.a;
                List<double> cs = new List<double>();
                cs.Add(c1); cs.Add(c2); cs.Add(c3);
                if ((line1.c / line1.a) != cs.Max() & (line1.c / line1.a) != cs.Min())
                {
                    return line1;
                }
                else if ((line2.c / line2.a) != cs.Max() & (line2.c / line2.a) != cs.Min())
                {
                    return line2;
                }
                else
                {
                    return line3;
                }
            }
            else
            {
                double y1 = line1.sp.Y ; double y2 = line2.sp.Y; double y3 = line3.sp.Y;
                List<double> ys = new List<double>();
                ys.Add(y1); ys.Add(y2); ys.Add(y3);
                if (line1.sp.Y != ys.Max() & line1.sp.Y != ys.Min())
                {
                    return line1;
                }
                else if (line2.sp.Y != ys.Max() & line2.sp.Y != ys.Min())
                {
                    return line2;
                }
                else
                {
                    return line3;
                }
            }
        }

        //找到三条平行线中中间的那条线
        public static Wall FindMiddleWall(Wall wall1, Wall wall2, Wall wall3)
        {
            Dictionary<line, Wall> line_wall = new Dictionary<line, Wall>();
            line line1 = basic.GetLine(wall1);
            line line2 = basic.GetLine(wall2);
            line line3 = basic.GetLine(wall3);
            line_wall.Add(line1, wall1);
            line_wall.Add(line2, wall2);
            line_wall.Add(line3, wall3);
            //不平行x轴的时候
            if (!(-0.001 <= line1.a & line1.a <= 0.001))
            {
                double c1 = line1.c / line1.a; double c2 = line2.c / line2.a; double c3 = line3.c / line3.a;
                List<double> cs = new List<double>();
                cs.Add(c1); cs.Add(c2); cs.Add(c3);
                if ((line1.c / line1.a) != cs.Max() & (line1.c / line1.a) != cs.Min())
                {
                    return line_wall[line1];
                }
                else if ((line2.c / line2.a) != cs.Max() & (line2.c / line2.a) != cs.Min())
                {
                    return line_wall[line2];
                }
                else
                {
                    return line_wall[line3];
                }
            }
            else
            {
                double y1 = line1.sp.Y; double y2 = line2.sp.Y; double y3 = line3.sp.Y;
                List<double> ys = new List<double>();
                ys.Add(y1); ys.Add(y2); ys.Add(y3);
                if (line1.sp.Y != ys.Max() & line1.sp.Y != ys.Min())
                {
                    return line_wall[line1];
                }
                else if (line2.sp.Y != ys.Max() & line2.sp.Y != ys.Min())
                {
                    return line_wall[line2];
                }
                else
                {
                    return line_wall[line3];
                }
            }
        }

        public static double Angle_cal(Element wall1, Element wall2)
        {
            double angle = -1;
            try
            {
                LocationCurve curve1 = wall1.Location as LocationCurve;
                Line line1 = curve1.Curve as Line;
                XYZ Adirection1 = line1.Direction;
                LocationCurve curve2 = wall2.Location as LocationCurve;
                Line line2 = curve2.Curve as Line;
                XYZ Adirection2 = line2.Direction;
                double productValue1 = (Adirection1.X * Adirection2.X) + (Adirection1.Y * Adirection2.Y);  // 向量的乘积
                double A11 = Math.Sqrt(Adirection1.X * Adirection1.X + Adirection1.Y * Adirection1.Y);  // 向量a的模
                double A21 = Math.Sqrt(Adirection2.X * Adirection2.X + Adirection2.Y * Adirection2.Y);  // 向量b的模
                double cosValue1 = productValue1 / (A11 * A21);      // 余弦公式
                if (cosValue1 < -1 & cosValue1 > -2)
                { cosValue1 = -1; }
                else if (cosValue1 > 1 && cosValue1 < 2)
                { cosValue1 = 1; }
                angle = Math.Abs(Math.Acos(cosValue1) * (180 / Math.PI));
            }
            catch
            { }
            return angle;
        }

        //找到line1，line2中离line_index直线距离较近的那条
        public static line FindClosestLine(line line_index, line line1, line line2)
        {
            List<double> line1dis = new List<double>();
            List<double> line2dis = new List<double>();
            line1dis.Add(Math.Pow((line_index.sp.X - line1.sp.X), 2) + Math.Pow((line_index.sp.Y - line1.sp.Y), 2));
            line1dis.Add(Math.Pow((line_index.sp.X - line1.ep.X), 2) + Math.Pow((line_index.sp.Y - line1.ep.Y), 2));
            line1dis.Add(Math.Pow((line_index.ep.X - line1.sp.X), 2) + Math.Pow((line_index.ep.Y - line1.sp.Y), 2));
            line1dis.Add(Math.Pow((line_index.ep.X - line1.ep.X), 2) + Math.Pow((line_index.ep.Y - line1.ep.Y), 2));
            double min_dis1 = line1dis.Min();
            line2dis.Add(Math.Pow((line_index.sp.X - line2.sp.X), 2) + Math.Pow((line_index.sp.Y - line2.sp.Y), 2));
            line2dis.Add(Math.Pow((line_index.sp.X - line2.ep.X), 2) + Math.Pow((line_index.sp.Y - line2.ep.Y), 2));
            line2dis.Add(Math.Pow((line_index.ep.X - line2.sp.X), 2) + Math.Pow((line_index.ep.Y - line2.sp.Y), 2));
            line2dis.Add(Math.Pow((line_index.ep.X - line2.ep.X), 2) + Math.Pow((line_index.ep.Y - line2.ep.Y), 2));
            double min_dis2 = line2dis.Min();
            if (min_dis1 > min_dis2)
            {
                return line2;
            }
            else
            {
                return line1;
            }
        }

        //找到list<line>中距line_index最近的line
        public static line FindClosestLine_VerticalDist(line line_index, List<line> listline)
        {
            double min_dist = -100;
            line closest_line = listline[0];
            foreach (line l in listline)
            {
                double distance = basic.VertcieDistPointToLine(line_index.sp,l);
                if (distance < min_dist)
                {
                    min_dist = distance;
                    closest_line = l;
                }
            }
            return closest_line;
        }

        public static bool IfPointInLine(line line, XYZ point)
        {
            bool PointInLine = false;
            if ((Math.Min(line.sp.X, line.ep.X) - 0.0001) < point.X & (Math.Max(line.sp.X, line.ep.X) + 0.0001) > point.X & (Math.Min(line.sp.Y, line.ep.Y) - 0.0001) < point.Y & (Math.Max(line.sp.Y, line.ep.Y) + 0.0001) > point.Y)
            {
                PointInLine = true;
            }
            return PointInLine;
        }

        //找到一个点到另一条直线的垂足
        public static XYZ FindFoot(line line_index, XYZ point)
        {
            XYZ foot;
            if (-0.001 <= line_index.a & line_index.a <= 0.001)
            {
                foot = new XYZ(point.X, line_index.sp.Y, line_index.sp.Z);
            }
            //line_longer平行于Y轴
            else if (-0.001 <= line_index.b & line_index.b <= 0.001)
            {
                foot = new XYZ(line_index.sp.X, point.Y, line_index.sp.Z);
            }
            //非特殊情况
            else
            {
                //向量垂直：对应向量相乘再相加等于0
                double intersection_X = (line_index.b * point.X * line_index.unit_vector.X + line_index.c * line_index.unit_vector.Y + line_index.b * point.Y * line_index.unit_vector.Y) / (line_index.b * line_index.unit_vector.X - line_index.a * line_index.unit_vector.Y);
                double intersection_Y = (-line_index.a * intersection_X - line_index.c) / line_index.b;
                //TaskDialog.Show("debug", intersection_X.ToString() +"\n" + intersection_Y.ToString());
                foot = new XYZ(intersection_X, intersection_Y, line_index.sp.Z);
            }
            
            return foot;
        }


        //输入：两条线段；功能：求较短的一条线离较长线最近的点到较长的线的垂线；输出：较短的线，和垂线的list。line2为较短的线
        public static List<line> FindPerpendicularLine(line line_vertical, line line_point)
        {
            XYZ closest_point = basic.FindClosestDirectDistPoint(line_point, line_vertical); //找到line_point离line_vertical最近的点
            XYZ foot;
            //特殊情况：
            //line_longer平行于X轴
            if (-0.001 <= line_vertical.a & line_vertical.a <= 0.001)
            {
                foot = new XYZ(closest_point.X, line_vertical.sp.Y, line_vertical.sp.Z);
            }
            //line_longer平行于Y轴
            else if (-0.001 <= line_vertical.b & line_vertical.b <= 0.001)
            {
                foot = new XYZ(line_vertical.sp.X, closest_point.Y, line_vertical.sp.Z);
            }
            //非特殊情况
            else 
            {
                //向量垂直：对应向量相乘再相加等于0
                double intersection_X = (line_vertical.b * closest_point.X * line_vertical.unit_vector.X + line_vertical.c * line_vertical.unit_vector.Y + line_vertical.b * closest_point.Y * line_vertical.unit_vector.Y) / (line_vertical.b * line_vertical.unit_vector.X - line_vertical.a * line_vertical.unit_vector.Y);
                double intersection_Y = (-line_vertical.a * intersection_X - line_vertical.c) / line_vertical.b;
                //TaskDialog.Show("debug", intersection_X.ToString() +"\n" + intersection_Y.ToString());
                foot = new XYZ(intersection_X, intersection_Y, line_vertical.sp.Z);
            }
            List<line> shorter_prependicular = new List<line>();
            shorter_prependicular.Add(line_point);
            if (closest_point != foot)
            {
                shorter_prependicular.Add(basic.GetLine(closest_point, foot));
            }
            else
            {
                TaskDialog.Show("Need DEBUG","找到的垂线两端点距离为0");
            }
            return shorter_prependicular;
        }

        //返回两条线中较短的线
        public static line FindShorterLine(line line1, line line2)
        {
            if (line2.length > line1.length)
            {
                line line_longer = line2;
                line2 = line1;
                line1 = line_longer;
            }
            return line2;
        }

        public static XYZ FindIntersection(line line1, line line2)
        {
            //求交点
            double D = line1.a * line2.b - line2.a * line1.b;
            double cross_x = (line1.b * line2.c - line2.b * line1.c) / D;
            double cross_y = (line2.a * line1.c - line1.a * line2.c) / D;
            XYZ cross = new XYZ(cross_x, cross_y, line1.sp.Z);
            return cross;
        }

        public static List<Element> ElementsGetOrdered(List<Element> walllist)
        {
            for (int i = 0; i < walllist.Count; i++)
            {
                for (int j = i + 1; j < walllist.Count; j++)
                {
                    line line1 = basic.GetLine(walllist[i] as Wall);
                    line line2 = basic.GetLine(walllist[j] as Wall);
                    if (!(-0.001 <= line1.a & line1.a <= 0.001))
                    {
                        double c1 = line1.c / line1.a; double c2 = line2.c / line2.a;
                        if (line1.b / line1.a > 0) //x,y同号,二四象限
                        {
                            if (c1 > c2)
                            {
                                Element smaller = walllist[i];
                                walllist[i] = walllist[j];
                                walllist[j] = smaller;
                            }
                        }
                        else //x,y同号，一三象限
                        {
                            if (c1 < c2)
                            {
                                Element smaller = walllist[i];
                                walllist[i] = walllist[j];
                                walllist[j] = smaller;
                            }
                        }
                    }
                    else
                    {
                        double y1 = line1.sp.Y; double y2 = line2.sp.Y; 
                        if (y1 < y2)
                        {
                            Element smaller = walllist[i];
                            walllist[i] = walllist[j];
                            walllist[j] = smaller;
                        }
                    }
                }
            }
            return walllist;
        }


        /* 和IfPointInLine重复
        //判断直线上的点是否在线段中
        public static bool IsPointOfStraightlineInSegment(XYZ point, line segment)
        {
            //如果X,Y坐标都在线段的XY中则点在线段中
            List<double> terminal_vertex_x = new List<double>();
            terminal_vertex_x.Add(segment.sp.X);
            terminal_vertex_x.Add(segment.ep.X);
            List<double> terminal_vertex_y = new List<double>();
            terminal_vertex_y.Add(segment.sp.Y);
            terminal_vertex_y.Add(segment.ep.Y);
            bool PointInSegment = false;
            if ((terminal_vertex_x.Min() <= point.X & point.X <= terminal_vertex_x.Max()) & (terminal_vertex_y.Min() <= point.Y & point.Y <= terminal_vertex_y.Max()))
            {
                PointInSegment = true;
            }
            return PointInSegment;
        }
        */
    }

    class position_recognation
    {
        //判断两条线段平行或相交
        public static bool IsParallel(line line1, line line2)
        {
            bool parallel = false;
            double D = line1.a * line2.b - line2.a * line1.b;
            // D = 0表示两直线平行
            if (Math.Abs(D) < 0.01)  //line1 line2 are parallel
            {
                parallel = true;
            }
            return parallel;
        }



        //判断平行线段是否共线
        public static bool IsCollineation(line line1, line line2, Wall wall1, Wall wall2)
        {
            bool collineation = false;
            //a=0则平行于X轴
            if (Math.Abs(line1.a) <= 0.001 & Math.Abs(line2.a) <= 0.001)
            {
                //TaskDialog.Show("debug", ele1.Id.ToString()+"\n"+ ele2.Id.ToString() + "\n"+ "x direction");
                //if (line1.sp.Y - 0.01 <= line2.sp.Y & line2.sp.Y <= line2.sp.Y + 0.01)
                //if (Math.Abs(line1.sp.Y-line2.sp.Y) <= 0)
                if (Math.Abs(line1.sp.Y - line2.sp.Y) <= 0.00001)
                {
                    collineation = true;
                }
            }
            //b=0则平行于Y轴
            else if (Math.Abs(line1.b) <= 0.001 & Math.Abs(line2.b) <= 0.001)
            {
                //TaskDialog.Show("debug", ele1.Id.ToString() + "\n" + ele2.Id.ToString() + "\n" + "y direction");
                //if (line1.sp.X - 0.01 <= line2.sp.X & line2.sp.X <= line1.sp.X + 0.01)
                //if (Math.Abs(line1.sp.X - line2.sp.X) == 0)
                if (Math.Abs(line1.sp.X - line2.sp.X) <= 0.00001)
                {
                    collineation = true;
                }
            }
            else  //判断是否既不平行X轴也不平行Y轴线段是否共线 
            {
                double distance = basic.LineDistance(line1, line2, wall1, wall2);
                if (Math.Abs(distance) <= 0.00001)
                {
                    collineation = true;
                }
            }
            return collineation;
        }


        //判断平行线段是否重合
        public static bool IsSuperposition(line line1, line line2)
        {
            bool superposition = false;
            double minx1 = Math.Min(line1.sp.X, line1.ep.X) - 0.01;
            double maxx1 = Math.Max(line1.sp.X, line1.ep.X) + 0.01;
            double miny1 = Math.Min(line1.sp.Y, line1.ep.Y) - 0.01;
            double maxy1 = Math.Max(line1.sp.Y, line1.ep.Y) + 0.01;
            double minx2 = Math.Min(line2.sp.X, line2.ep.X) - 0.01;
            double maxx2 = Math.Max(line2.sp.X, line2.ep.X) + 0.01;
            double miny2 = Math.Min(line2.sp.Y, line2.ep.Y) - 0.01;
            double maxy2 = Math.Max(line2.sp.Y, line2.ep.Y) + 0.01;
            //判断是否有重合部分: 一条线在另一条线的最大坐标和最小坐标之间则判定为重合
            if ((minx1 <= line2.sp.X & line2.sp.X <= maxx1 & miny1 <= line2.sp.Y & line2.sp.Y <= maxy1) | (minx1 <= line2.ep.X & line2.ep.X <= maxx1 & miny1 <= line2.ep.Y & line2.ep.Y <= maxy1) | minx2 <= line1.sp.X & line1.sp.X <= maxx2 & miny2 <= line1.sp.Y & line1.sp.Y <= maxy2 | minx2 <= line1.ep.X & line1.ep.X <= maxx2 & miny2 <= line1.ep.Y & line1.ep.Y <= maxy2)
            {
                superposition = true;
            }
            return superposition;
        }

        //判断三条线的位置关系
        public static Dictionary<string, List<Wall>> ThreeLinesLocation(Wall wall1, Wall wall2, Wall wall3)
        {
            Dictionary<string, List<Wall>> walls_class = new Dictionary<string, List<Wall>>();
            walls_class.Add("parallel", new List<Wall>());
            walls_class.Add("vertical", new List<Wall>());
            walls_class.Add("other", new List<Wall>());
            //double angle12 = basic.Angle_cal(wall1, wall2);
            //double angle23 = basic.Angle_cal(wall2, wall3);
            //double angle31 = basic.Angle_cal(wall3, wall1);
            List<Wall> walls = new List<Wall>();
            walls.Add(wall1);
            walls.Add(wall2);
            walls.Add(wall3);
            for (int i = 0; i < 2; i++)
            {
                for (int j = i + 1; j < 3; j++)
                {
                    double angle = basic.Angle_cal(walls[i], walls[j]);
                    if ((0 <= angle & angle <= 0.5) | (179.5 <= angle & angle <= 180))//这个误差我不确定
                    {
                        if (!walls_class["parallel"].Contains(walls[i]))
                        {
                            walls_class["parallel"].Add(walls[i]);
                        }
                        if (!walls_class["parallel"].Contains(walls[j]))
                        {
                            walls_class["parallel"].Add(walls[j]);
                        }
                    }
                    else if (89.5 <= angle & angle <= 90.5)
                    {
                        if (!walls_class["vertical"].Contains(walls[i]))
                        {
                            walls_class["vertical"].Add(walls[i]);
                        }
                        if (!walls_class["vertical"].Contains(walls[j]))
                        {
                            walls_class["vertical"].Add(walls[j]);
                        }
                    }
                    else
                    {
                        if (!walls_class["other"].Contains(walls[i]))
                        {
                            walls_class["other"].Add(walls[i]);
                        }
                        if (!walls_class["other"].Contains(walls[j]))
                        {
                            walls_class["other"].Add(walls[j]);
                        }
                    }
                }
            }
            return walls_class;
        }

    }


    class extend
    {
        //延申平行线段，直接将两条线最近的点相连
        public static List<Wall> ConnectClosestPointsOfWalls(Document doc, line line1, line line2, Wall wall_1, Wall wall_2)
        {
            //TaskDialog.Show("debug", ele1.Id.ToString() + "\n" + ele2.Id.ToString() + "\n" + "collineate wall trim");
            /*
            XYZ point1 = new XYZ();
            XYZ point2 = new XYZ();
            double distance1 = Math.Pow((line1.sp.X - line2.sp.X), 2) + Math.Pow((line1.sp.Y - line2.sp.Y), 2);
            double distance2 = Math.Pow((line1.sp.X - line2.ep.X), 2) + Math.Pow((line1.sp.Y - line2.ep.Y), 2);
            double distance3 = Math.Pow((line1.ep.X - line2.sp.X), 2) + Math.Pow((line1.ep.Y - line2.sp.Y), 2);
            double distance4 = Math.Pow((line1.ep.X - line2.ep.X), 2) + Math.Pow((line1.ep.Y - line2.ep.Y), 2);
            List<double> distance = new List<double>();
            distance.Add(distance1); distance.Add(distance2); distance.Add(distance3); distance.Add(distance4);
            double min_dis = distance.Min();
            if (distance1 == min_dis)
            {
                point1 = line1.sp;
                point2 = line2.sp;
            }
            else if (distance2 == min_dis)
            {
                point1 = line1.sp;
                point2 = line2.ep;
            }
            else if (distance3 == min_dis)
            {
                point1 = line1.ep;
                point2 = line2.sp;
            }
            else if (distance4 == min_dis)
            {
                point1 = line1.ep;
                point2 = line2.sp;
            }
            else { TaskDialog.Show("Warning: TRIM", wall_1.Id + " / " + wall_2.Id + "\n俩共线墙的point查找失败，不可能发生的情况"); }
            */
            List<XYZ> closest_points = basic.FindClosestPoints(line1, line2);
            List<Wall> new_walls = new List<Wall>();
            if (closest_points.Count != 0)
            {
                try
                {
                    Wall new_wall = function.Create_wall(doc, closest_points[0], closest_points[1], wall_1);
                    if (new_wall != null)
                    {
                        new_walls.Add(new_wall);
                    }
                }
                catch (Exception ex)
                {
                    new_walls.Clear(); //失败了就返回空列表
                    TaskDialog.Show("Error:", "将两条线最近的点相连失败\n" + wall_1.Id + " / " + wall_2.Id + "\n" + ex.Message);
                }
            }
            else
            {
                new_walls.Clear();
            }
            return new_walls;
        }


        //做两条平行但不共线的墙的垂线
        public static List<Wall> ConnectTwoUncolinearWall(Document doc, line line1, line line2, Wall wall_1, Wall wall_2)
        {
            List<Wall> new_walls = new List<Wall>();
            line line_shorter = line2;
            line line_longer = line1;
            if (line2.length > line1.length)
            {
                line_longer = line2;
                line_shorter = line1;
            }
            //将长度短的line作为line2,以line2向line1做的垂线创建墙
            List<line> shorter_prependicular = basic.FindPerpendicularLine(line_longer, line_shorter);
            Wall new_wall = function.Create_wall(doc, shorter_prependicular[1].sp, shorter_prependicular[1].ep, wall_1);
            if (new_wall != null)
            {
                new_walls.Add(new_wall);
            }
            return new_walls;
        }

        //延申两条平行不共线的平行线段
        public static List<Wall> ExtendParallelUncolinearWall(Document doc, line line1, line line2, Wall wall_1, Wall wall_2)
        {
            line line_shorter, line_longer;
            //平移较短的线到较长的线，先连接较长的线和平移后的线，再连接平移的线与较长的线
            if (line2.length > line1.length)
            {
                line_longer = line2;
                line_shorter = line1;
            }
            else
            {
                line_longer = line1;
                line_shorter = line2;
            }
            List<Wall> new_walls = new List<Wall>();
            
            try
            {
                line trans_line = function.TranslationLine(line_longer, line_shorter);
                bool superposition = position_recognation.IsSuperposition(line_longer, trans_line); //一条线平移到另一条后与另一条是否相交
                if (!superposition)
                {
                    List<Wall> connect_trans = extend.ConnectClosestPointsOfWalls(doc, line_longer, trans_line, wall_1, wall_2);
                    if (connect_trans.Count != 0)
                    {
                        new_walls.AddRange(extend.ConnectClosestPointsOfWalls(doc, basic.GetLine(connect_trans[0]), line_shorter, wall_1, wall_2));
                        new_walls.AddRange(connect_trans);
                    }
                    else //如果连接平移后的线和较长的线失败，则连接trans_line和line_short
                    {
                        new_walls.AddRange(extend.ConnectClosestPointsOfWalls(doc, trans_line, line_shorter, wall_1, wall_2));
                    }
                }
                else
                {
                    new_walls.AddRange(extend.ConnectTwoUncolinearWall(doc, line1, line2, wall_1, wall_2));
                }
            }
            catch
            {
                TaskDialog.Show("Need debug","连接以下平行不共线的两面墙失败：\n"+wall_1.Id.ToString() + "\n" + wall_2.Id.ToString());
            }
            return new_walls;
        }
        
        /*
        public static List<Wall> ExtendNearParallelWall(Document doc, line line1, line line2, Wall wall_1, Wall wall_2)
        {
            List<Wall> new_walls = new List<Wall>();
            line near_wall = function.TranslationLine(line1, line2);
            bool superposition = position_recognation.IsSuperposition(line1, near_wall);
            if (!superposition)
            {
                new_walls = extend.ConnectClosestPointsOfWalls(doc, line1, near_wall, wall_1, wall_2);
            }
            else
            {
                TaskDialog.Show("prompt", wall_1.Id.ToString() + "/" + wall_2.Id.ToString() + ":\n两相近墙平行不共线，平移后重合,暂时认为可以不延申");
            }
            return new_walls;
        }

        //延申两条距离较远的平行线段，将line2对齐到line1,如果重合由一面墙向另一面墙做垂线；如果不重合，先平移line2，由line2
        public static List<Wall> ExtendFarParallelWall(Document doc, line line1, line line2, Wall wall_1, Wall wall_2)
        {
            List<Wall> new_walls = new List<Wall>();
            line trans_line = function.TranslationLine(line1, line2);
            bool superposition = position_recognation.IsSuperposition(line1, trans_line);
            if (!superposition)
            {
                try
                {
                    //先把line2平移后的line和line1连起来，再把平移后的线和平移前的线连起来
                    new_walls.AddRange(extend.ConnectClosestPointsOfWalls(doc, line1, trans_line, wall_1, wall_2));
                    //把新生成的墙和line2连接起来
                    
                }
                catch (Exception ex)
                {
                    new_walls.Clear();
                    TaskDialog.Show("Error:", "连接两条较远且不重合的平行墙失败\n" + wall_1.Id + " / " + wall_2.Id + "\n" + ex.Message);
                }
                try
                {
                    if (new_walls.Count != 0)
                    {
                        line new_line = basic.GetLine(new_walls[0]);
                        new_walls.AddRange(ConnectTwoUncolinearWall(doc, line1, line2, wall_1, wall_2));  //todo: 可优化，写一个过点做垂线的函数
                    }
                }
                catch (Exception ex)
                {
                    new_walls.Clear();
                    TaskDialog.Show("Error:", "连接两条较远且不重合的平行墙失败\n" + wall_1.Id + " / " + wall_2.Id + "\n" + ex.Message);
                }
            }
            else
            {
                try
                {
                    new_walls.AddRange(ConnectTwoUncolinearWall(doc, trans_line, line2, wall_1, wall_2));
                }
                catch (Exception ex)
                {
                    new_walls.Clear();
                    TaskDialog.Show("Error:", "连接两条较远且重合的平行墙失败\n" + wall_1.Id + " / " + wall_2.Id + "\n" + ex.Message);
                }
            }
            return new_walls;
        }
        */


        //延申两条相交线
        public static List<Wall> ExtendCrossWall(Document doc, line line1, line line2, Wall wall_1, Wall wall_2)
        {
            List<Wall> new_walls = new List<Wall>();
            //求交点
            //double D = line1.a * line2.b - line2.a * line1.b;
            //double cross_x = (line1.b * line2.c - line2.b * line1.c) / D;
            //double cross_y = (line2.a * line1.c - line1.a * line2.c) / D;
            XYZ cross = basic.FindIntersection(line1, line2);
            //TaskDialog.Show("debug", ele1.Id.ToString()+":\n"+coordinate1[0].X.ToString() +"\n"+ coordinate1[0].Y.ToString()+"\n" + cross_x.ToString()+"\n"+cross_y.ToString());
            //分别判断交点是否在两线段内,交点不在线段内的则延申墙
            //cross 不在line1中
            if (! basic.IfPointInLine(line1,cross))
            {
                //延申line1到交点
                Wall new_wall = function.Create_wall(doc, basic.FindClosestPoints(line1, cross)[0], cross, wall_1);
                if (new_wall != null)
                {
                    new_walls.Add(new_wall);
                }
            }
            else 
            { //不需要延申
            }
            if (!basic.IfPointInLine(line2, cross))
            {
                Wall new_wall = function.Create_wall(doc, basic.FindClosestPoints(line2, cross)[0], cross, wall_2);
                if (new_wall != null)
                {
                    new_walls.Add(new_wall);
                }
            }
            else
            { //不需要延申
            }
            /*
            if (!((Math.Min(line1.sp.X, line1.ep.X) - 0.01) < cross_x & (Math.Max(line1.sp.X, line1.ep.X) + 0.01) > cross_x & (Math.Min(line1.sp.Y, line1.ep.Y) - 0.01) < cross_y & (Math.Max(line1.sp.Y, line1.ep.Y) + 0.01) > cross_y))
            {
                //cross 不在line2中
                if (!((Math.Min(line2.sp.X, line2.ep.X) - 0.01) < cross_x & (Math.Max(line2.sp.X, line2.ep.X) + 0.01) > cross_x & (Math.Min(line2.sp.Y, line2.ep.Y) - 0.01) < cross_y & (Math.Max(line2.sp.Y, line2.ep.Y) + 0.01) > cross_y))
                {
                    double distance11 = Math.Pow((line1.sp.X - cross.X), 2) + Math.Pow((line1.sp.Y - cross.Y), 2);
                    double distance12 = Math.Pow((line1.ep.X - cross.X), 2) + Math.Pow((line1.ep.Y - cross.Y), 2);
                    double distance21 = Math.Pow((line2.sp.X - cross.X), 2) + Math.Pow((line2.sp.Y - cross.Y), 2);
                    double distance22 = Math.Pow((line2.ep.X - cross.X), 2) + Math.Pow((line2.ep.Y - cross.Y), 2);
                    //TaskDialog.Show("test", "延申不平行的墙");
                    //to do:可以根据生成房间的分辨率设置一个延申墙的分辨率,暂设0.05
                    try
                    {
                        if (Math.Min(distance11, distance12) >= 0.05)
                        {
                            if (distance11 < distance12)
                            {

                                new_walls.Add(function.Create_wall(doc, line1.sp, cross, wall_1));

                            }
                            else
                            {
                                new_walls.Add(function.Create_wall(doc, line1.ep, cross, wall_1));

                            }
                        }
                        if (Math.Min(distance21, distance22) >= 0.05)
                        {
                            if (distance21 < distance22)
                            {

                                new_walls.Add(function.Create_wall(doc, line2.sp, cross, wall_2));
                            }
                            else
                            {

                                new_walls.Add(function.Create_wall(doc, line2.ep, cross, wall_1));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        new_walls.Clear();
                        TaskDialog.Show("Eroor:", "replaced by crossed walls:\n" + wall_1.Id + " / " + wall_2.Id + "\n" + ex.Message);
                    }
                }
                else //cross在line2中，则延申line1到交点
                {
                    double distance11 = Math.Pow((line1.sp.X - cross.X), 2) + Math.Pow((line1.sp.Y - cross.Y), 2);
                    double distance12 = Math.Pow((line1.ep.X - cross.X), 2) + Math.Pow((line1.ep.Y - cross.Y), 2);
                    try
                    {
                        if (distance11 < distance12 & distance11 >= 0.05)
                        {
                            new_walls.Add(function.Create_wall(doc, line1.sp, cross, wall_1));
                        }
                        else if (distance12 >= 0.05)
                        {
                            new_walls.Add(function.Create_wall(doc, line1.ep, cross, wall_1));
                        }
                    }
                    catch (Exception ex)
                    {
                        new_walls.Clear();
                        TaskDialog.Show("Eroor:", "replaced by crossed walls,cross in line2:\n" + wall_1.Id + " / " + wall_2.Id + "\n" + ex.Message);
                    }
                }
            }
            else //cross在line1中，则延申line2到交点
            {
                double distance21 = Math.Pow((line2.sp.X - cross.X), 2) + Math.Pow((line2.sp.Y - cross.Y), 2);
                double distance22 = Math.Pow((line2.ep.X - cross.X), 2) + Math.Pow((line2.ep.Y - cross.Y), 2);
                try
                {
                    if (distance21 < distance22 & distance21 >= 0.05)
                    {
                        new_walls.Add(function.Create_wall(doc, line2.sp, cross, wall_2));
                    }
                    else if (distance22 >= 0.05)
                    {
                        new_walls.Add(function.Create_wall(doc, line2.ep, cross, wall_2));
                    }
                }
                catch (Exception ex)
                {
                    new_walls.Clear();
                    TaskDialog.Show("Eroor:", "replaced by crossed walls,cross in line1:\n" + wall_1.Id + " / " + wall_2.Id + "\n" + ex.Message);
                }
            }
            */
            return new_walls;
        }

        //连接两条平行的墙（共线or不共线）
        public static List<Wall> ExtendTwoParallelWalls(Document doc, Wall wall1,Wall wall2)
        {
            line line1 = basic.GetLine(wall1);
            line line2 = basic.GetLine(wall2);
            List<Wall> new_walls = new List<Wall>();
            bool collineation = position_recognation.IsCollineation(line1, line2, wall1, wall2);
            if (collineation) //平行且共线
            {
                bool superposition = position_recognation.IsSuperposition(line1, line2);
                if (!superposition)
                {
                    new_walls.AddRange(extend.ConnectClosestPointsOfWalls(doc, line1, line2, wall1, wall2));
                }
                else
                {
                    //TaskDialog.Show("prompt", "该两共线墙重合，暂时考虑不做处理：\n" + wall1.Id.ToString() + "/" + wall2.Id.ToString());
                }
            }
            else //平行不共线
            {
                new_walls.AddRange(extend.ExtendParallelUncolinearWall(doc, line1, line2, wall1, wall2));
            }
            return new_walls;
        }



        //将一条线延长到与另一条相交线的垂足
        public static List<Wall> ExtendLineToFoot(Document doc, line line, line line_index, Wall old_wall)
        {
            XYZ point1 = basic.FindClosestDirectDistPoint(line,line_index);
            XYZ point2 = basic.FindFoot(line_index,point1);
            List<Wall> new_walls = new List<Wall>();
            Wall new_wall = function.Create_wall(doc,point1, point2,old_wall);
            if (new_wall != null)
            {
                new_walls.Add(new_wall);
            }
            return new_walls;
        }


        //连接多条同一个方向上（平行）的墙
        public static List<Wall> ConnectOneDirectionWalls(Document doc, List<Element> walls_to_connect)
        {
            List<Wall> new_walls = new List<Wall>();
            List<Element> ordered_walls = basic.ElementsGetOrdered(walls_to_connect);
            for (int i = 0; i < ordered_walls.Count - 1; i++)
            {
                int j = i + 1;
                try
                {
                    new_walls.AddRange(ExtendTwoParallelWalls(doc, ordered_walls[i] as Wall, ordered_walls[j] as Wall));
                }
                catch(Exception ex)
                {
                    TaskDialog.Show("Error", "连接同方向的墙失败：\n" + ex.Message);
                }
            }

            return new_walls;
        }

        public static List<Wall> ExtendOneDirectionWallsToAnother(Document doc, List<Element> walls_to_extend, List<Element> another_direction)
        {
            List<Wall> new_walls = new List<Wall>();
            //找到待连接的线距另一方向最近的墙
            foreach (Element e in walls_to_extend)
            {
                line line1 = basic.GetLine(e as Wall);
                Element closest = another_direction[0];
                double closest_dist = -1;
                foreach (Element anohter_e in another_direction)
                {
                    line line_index = basic.GetLine(anohter_e as Wall);
                    double distance = basic.FindMinVerticeDist(line1, line_index);
                    if (distance < closest_dist)
                    {
                        closest_dist = distance;
                        closest = anohter_e;
                    }
                }
                //连接
                //new_walls.AddRange(extend.ExtendLineToFoot(doc, line1, basic.GetLine(closest as Wall), e as Wall));
                new_walls.AddRange(extend.ExtendCrossWall(doc, line1, basic.GetLine(closest as Wall), e as Wall, closest as Wall));
            }
            return new_walls;
        }


        public static List<Wall> ConnectTwoDirectionWalls(Document doc, List<List<Element>> walls_to_connect)
        {
            List<Wall> new_walls = new List<Wall>();
            //是否有平移后不重合的方向
            int uncoincide_direction = -1;
            for (int i = 0; i < walls_to_connect.Count; i++)
            {
                bool superposition = false;
                for(int m = 0; m < walls_to_connect[i].Count; m++)
                {
                    for (int n = m + 1; n < walls_to_connect[i].Count; n++)
                    {
                        line line1 = basic.GetLine(walls_to_connect[i][m] as Wall);
                        line line2 = basic.GetLine(walls_to_connect[i][n] as Wall);
                        line lin_trans = function.TranslationLine(line1,line2) ;
                        if (position_recognation.IsSuperposition(line1 , lin_trans))
                        {
                            superposition = true;
                        }
                    }
                }
                if (superposition == false)
                {
                    uncoincide_direction = i;
                    break;
                }
            }
            //先连接平移后不重合的方向,再把另一个方向的墙延申到不重合的方向
            if (uncoincide_direction != -1)
            {
                ConnectOneDirectionWalls(doc, walls_to_connect[uncoincide_direction]);
                //找到与另一个方向垂直距离最近的墙并与之连接
                for (int i = 0; i < walls_to_connect.Count; i++)
                {
                    if (i != uncoincide_direction)
                    {
                        new_walls.AddRange(ExtendOneDirectionWallsToAnother(doc, walls_to_connect[i], walls_to_connect[uncoincide_direction]));
                    }
                }
            }
            //所有方向的墙都重合
            else
            {
                //判断是否有一个方向的墙完全在另一方向墙的一侧：找该方向墙与另一方向垂线最近的墙，如果是同一条则在同一个方向
                int not_same_side = -1;
                for (int i = 0; i < walls_to_connect.Count; i++)
                {
                    line linei0 = basic.GetLine(walls_to_connect[i][0] as Wall);
                    List<line> another_lines = new List<line>();
                    for (int a = 0; a < walls_to_connect.Count; a++)
                    {
                        if (a != i)
                        {
                            foreach (Element e_another in walls_to_connect[a])
                            {
                                another_lines.Add(basic.GetLine(e_another as Wall));
                            }
                        }
                    } 
                    line closest_line1 = basic.FindClosestLine_VerticalDist(linei0, another_lines);
                    for (int j = 1; j < walls_to_connect[i].Count; j++)
                    {
                        line eline = basic.GetLine(walls_to_connect[i][j] as Wall);
                        line closest_line2 = basic.FindClosestLine_VerticalDist(eline, another_lines);
                        if (closest_line1 != closest_line2)
                        {
                            not_same_side = i;
                        }
                    }
                }
                //都在同一个方向：先任意连接一个方向的墙
                if (not_same_side == -1)
                {
                    ConnectOneDirectionWalls(doc, walls_to_connect[0]);
                    //找到与另一个方向垂直距离最近的墙并与之连接
                    new_walls.AddRange(ExtendOneDirectionWallsToAnother(doc, walls_to_connect[1], walls_to_connect[0]));
                }
                //不在同一方向：先连接不在同一方向的墙，再延申同一方向的墙
                else
                {
                    ConnectOneDirectionWalls(doc, walls_to_connect[not_same_side]);
                    //找到与另一个方向垂直距离最近的墙并与之连接
                    for (int a = 0; a < walls_to_connect.Count; a++)
                    {
                        if (a != not_same_side)
                        {
                            new_walls.AddRange(ExtendOneDirectionWallsToAnother(doc, walls_to_connect[a], walls_to_connect[not_same_side]));
                        }
                    }
                }
            }
            return new_walls;
        }


        //连接多个方向的墙
        public static List<Wall> ConnectMulDirectionWalls(Document doc, List<List<Element>> dif_direct_walls)
        {
            List<List<Element>> TwoDirections = new List<List<Element>>();
            TwoDirections.Add(dif_direct_walls[0]);
            TwoDirections.Add(dif_direct_walls[1]);
            //先连接两个方向的墙
            List<Wall> new_walls = ConnectTwoDirectionWalls(doc, TwoDirections);
            //再将其他方向的墙延伸到该方向
            for (int i = 2; i < dif_direct_walls.Count; i++)
            {
                new_walls.AddRange(ExtendOneDirectionWallsToAnother(doc, dif_direct_walls[i], dif_direct_walls[i - 1]));
            }
            return new_walls;
        }
    }





    //几何操作的函数
    class function
    {
        public static void DeleteElements(Document doc, List<ElementId> deletes)
        {
            using (var ts = new Transaction(doc, "delete some columns"))
            {
                ts.Start();
                ICollection<ElementId> deletedElements = doc.Delete(deletes);
                ts.Commit();
            }
        }

        public static void DeleteElement(Document doc, ElementId delete)
        {
            using (var ts = new Transaction(doc, "delete some columns"))
            {
                ts.Start();
                ICollection<ElementId> deletedElements = doc.Delete(delete);
                ts.Commit();
            }
        }

        //根据起点和终点创建新的墙，创建成功返回新创建的wall，若创建失败，则返回null
        public static Wall  Create_wall(Document doc, XYZ sp, XYZ ep, Element wall)
        {
            Wall new_wall = null;
            Wall old_wall = wall as Wall;
            //先计算新建的墙起点到终点的距离，如果距离大于墙厚则新建，如果小于墙厚则不新建
            double distance = basic.PointDistance(sp,ep);
            //if (distance >  old_wall.Width/2)
            if (distance > 0.1)
            {
                try
                {
                    //查找默认墙类型
                    //ElementId id = doc.GetDefaultElementTypeId(ElementTypeGroup.WallType);
                    //WallType type = doc.GetElement(id) as WallType;
                    //创建墙
                    using (var ts = new Transaction(doc, "create wall"))
                    {
                        ts.Start();
                        FailureHandlingOptions options = ts.GetFailureHandlingOptions();
                        dealwitherror.MyFailuresPreprocessor failureProcessor = new dealwitherror.MyFailuresPreprocessor();
                        options.SetFailuresPreprocessor(failureProcessor);
                        ts.SetFailureHandlingOptions(options);
                        new_wall = Wall.Create(doc, Line.CreateBound(sp, ep), old_wall.LevelId, false);
                        //ts.Commit();
                        var status = ts.Commit();
                        if (status != TransactionStatus.Committed)
                        {
                            if (failureProcessor.HasError)
                            {
                                
                                //TaskDialog.Show("ERROR", failureProcessor.FailureMessage);
                                //TaskDialog.Show("ERROR", new_wall.Id.ToString()+"\n"+ failureProcessor.FailureMessage);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    new_wall = null;
                    TaskDialog.Show("Error:",old_wall.Id.ToString()+ ":\n创建新墙失败：\n"  + ex.Message);
                }
                
            }
            else
            {
                new_wall = null;
            }

            if (new_wall != null)
            {
                try
                {
                    //修改新创建的墙的属性
                    using (Transaction ts = new Transaction(doc, "modify wall"))
                    {
                        ts.Start();
                        FailureHandlingOptions options = ts.GetFailureHandlingOptions();
                        dealwitherror.MyFailuresPreprocessor failureProcessor = new dealwitherror.MyFailuresPreprocessor();
                        options.SetFailuresPreprocessor(failureProcessor);
                        ts.SetFailureHandlingOptions(options);
                        new_wall.WallType = old_wall.WallType;
                        new_wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).Set(old_wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId());
                        new_wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(old_wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsElementId());
                        //如果顶部约束是未连接，则new wall设置为未连接，如果顶部有约束，则new wall也设置约束
                        if (old_wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsValueString().Contains("未连接")) //是未连接
                        {
                            new_wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(old_wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId());
                            Parameter user_height = new_wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                            user_height.Set(old_wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble());
                        }
                        else //不是未连接
                        {
                            new_wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(old_wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId());
                            Parameter topoffset = new_wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
                            topoffset.Set(old_wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).AsDouble());
                        }
                        //to do: 是否还有其他属性需要修改
                        //ts.Commit();
                        var status = ts.Commit();
                        if (status != TransactionStatus.Committed)
                        {
                        if (failureProcessor.HasError)
                        {
                        TaskDialog.Show("ERROR", failureProcessor.FailureMessage);
                        }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //new_wall = null;
                    TaskDialog.Show("Error:", old_wall.Id.ToString() + ":\n修改墙属性：\n" + ex.Message);
                }   
            }
            return new_wall;
        }
        

        //不共线也不重合的线段平移
        //平移较短的直线
        public static line TranslationLine(line line_index, line line_to_trans)
        {
            List<line> shorter_perpendicular = basic.FindPerpendicularLine(line_index, line_to_trans);
            //将line1和line2中较短的一根线沿着垂线平移
            XYZ trans_spoint = shorter_perpendicular[0].sp + shorter_perpendicular[1].unit_vector * shorter_perpendicular[1].length;
            XYZ trans_epoint = shorter_perpendicular[0].ep + shorter_perpendicular[1].unit_vector * shorter_perpendicular[1].length;
            line trans_line = basic.GetLine(trans_spoint, trans_epoint);
            /*
            XYZ TransPoint(XYZ origin_point, line line_origin, line line_trans)
            {
                //考虑特殊情况：平行于x，y轴
                double vectorx = line_origin.sp.X - line_origin.ep.X;
                double vectory = line_origin.sp.Y - line_origin.ep.Y;
                double new_x = (origin_point.X * line_trans.b * vectorx - line_trans.c * vectory - origin_point.Y * vectory) / (line_trans.b * vectorx - line_trans.a * vectory);
                double new_y = -(line_trans.a / line_trans.b) * new_x - (line_trans.c / line_trans.b);
                XYZ trans_point = new XYZ(new_x, new_y, origin_point.Z);
                return trans_point;
            }
            line translated_line = new line();
            translated_line.a = line1.a;
            translated_line.b = line1.b;
            translated_line.c = line1.c;
            //a=0则平行于X轴
            if (Math.Abs(line1.a) <= 0.01)
            {
                translated_line.sp = new XYZ(line2.sp.X, line1.sp.Y, line2.sp.Z);
                translated_line.ep = new XYZ(line2.ep.X, line1.ep.Y, line2.ep.Z);
            }
            //b=0则平行于Y轴
            else if (Math.Abs(line1.b) <= 0.01)
            {
                translated_line.sp = new XYZ(line1.sp.X,line2.sp.Y,line2.sp.Z); ;
                translated_line.ep = new XYZ(line1.ep.X, line2.ep.Y, line2.ep.Z);
            }
            else
            {
                translated_line.sp = TransPoint(line1.sp, line2, translated_line);
                translated_line.ep = TransPoint(line1.ep, line2, translated_line);
            }
            */
            return trans_line;
        }
    }
}
