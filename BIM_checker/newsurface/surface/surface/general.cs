using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;

namespace surface
{
    class line
    {
        public double a;
        public double b;
        public double c;
        public XYZ sp;
        public XYZ ep;
        public XYZ direction;
    }
    class general
    {
        static public line GetLineFromPoint(XYZ sp, XYZ ep)
        {
            line segment = new line();
            segment.sp = sp;
            segment.ep = ep;
            segment.a = segment.sp.Y - segment.ep.Y;
            segment.b = segment.ep.X - segment.sp.X;
            segment.c = segment.sp.X * segment.ep.Y - segment.ep.X * segment.sp.Y;
            segment.direction = new XYZ(ep.X - sp.X, ep.Y - sp.Y, ep.Z - sp.Z);
            return segment;
        }
        static public line GetLineFromWall(Wall wall)
        {
            line segment = new line();
            LocationCurve lcurve = wall.Location as LocationCurve;
            Line wallline = lcurve.Curve as Line;
            IList<XYZ> coordinate = wallline.Tessellate();
            segment.sp = coordinate[0];
            segment.ep = coordinate[1];
            segment.a = segment.sp.Y - segment.ep.Y;
            segment.b = segment.ep.X - segment.sp.X;
            segment.c = segment.sp.X * segment.ep.Y - segment.ep.X * segment.sp.Y;
            segment.direction = wallline.Direction;
            return segment;
        }

        //返回0到180度的angle
        public static double AngleCalThroughLine(line line1, line line2)
        {
            XYZ Adirection1 = line1.direction;
            XYZ Adirection2 = line2.direction;  //direction是终点减去起点
            double productValue1 = (Adirection1.X * Adirection2.X) + (Adirection1.Y * Adirection2.Y);  // 向量的乘积
            double A11 = Math.Sqrt(Adirection1.X * Adirection1.X + Adirection1.Y * Adirection1.Y);  // 向量a的模
            double A21 = Math.Sqrt(Adirection2.X * Adirection2.X + Adirection2.Y * Adirection2.Y);  // 向量b的模
            double cosValue1 = productValue1 / (A11 * A21);      // 余弦公式
            if (cosValue1 < -1 & cosValue1 > -2)
            { cosValue1 = -1; }
            else if (cosValue1 > 1 && cosValue1 < 2)
            { cosValue1 = 1; }
            double angle = Math.Abs(Math.Acos(cosValue1) * (180 / Math.PI));
            return angle;
        }
        public static double DistanceOfTwoPoint(XYZ point1, XYZ point2)
        {
            double distance = Math.Sqrt(Math.Pow((point1.X - point2.X), 2) + Math.Pow((point1.Y - point2.Y), 2) +  Math.Pow((point1.Z - point2.Z), 2));
            return distance;
        }


            //判断距离是否在一个范围内
        public static bool IsAllClose(XYZ orgin, XYZ close_point, double tolerance)
        {
            //系数放大到根号2倍（暂时先不放大）
            tolerance *= 1;
            bool isclose = false;
            double distance =Math.Sqrt(Math.Pow((orgin.X - close_point.X), 2) + Math.Pow((orgin.Y - close_point.Y), 2) + Math.Pow((orgin.Z - close_point.Z), 2));
            if (distance < tolerance)
            {
                isclose = true;
            }
            return isclose;
        }
        public static bool IsXYClose(XYZ orgin, XYZ close_point, double tolerance)
        {
            //系数放大到根号2倍（暂时先不放大）
            tolerance *= 1;
            bool isclose = false;
            double distance = Math.Sqrt(Math.Pow((orgin.X - close_point.X), 2) + Math.Pow((orgin.Y - close_point.Y), 2));
            if (distance < tolerance)
            {
                isclose = true;
            }
            return isclose;
        }
        public static double GetDistanceFromWalls(Wall wall1, Wall wall2)
        {
            line line1 = GetLineFromWall(wall1);
            line line2 = GetLineFromWall(wall2);
            double distance1 = Math.Sqrt(Math.Pow((line1.sp.X - line2.sp.X), 2) + Math.Pow((line1.sp.Y - line2.sp.Y), 2) + Math.Pow((line1.sp.Z - line2.sp.Z), 2));
            double distance2 = Math.Sqrt(Math.Pow((line1.sp.X - line2.ep.X), 2) + Math.Pow((line1.sp.Y - line2.ep.Y), 2) + Math.Pow((line1.sp.Z - line2.ep.Z), 2));
            double distance3 = Math.Sqrt(Math.Pow((line1.ep.X - line2.sp.X), 2) + Math.Pow((line1.ep.Y - line2.sp.Y), 2) + Math.Pow((line1.ep.Z - line2.sp.Z), 2));
            double distance4 = Math.Sqrt(Math.Pow((line1.ep.X - line2.ep.X), 2) + Math.Pow((line1.ep.Y - line2.ep.Y), 2) + Math.Pow((line1.ep.Z - line2.ep.Z), 2));
            List<double> distance = new List<double>();
            distance.Add(distance1); distance.Add(distance2); distance.Add(distance3); distance.Add(distance4);
            double min_dis = distance.Min();
            return min_dis;
        }


    }
}
