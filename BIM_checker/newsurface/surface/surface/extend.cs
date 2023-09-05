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
    class extend
    {
        public static void Create_wall(Document doc, XYZ sp, XYZ ep, Element wall)
        {
            //查找默认墙类型
            //ElementId id = doc.GetDefaultElementTypeId(ElementTypeGroup.WallType);
            //WallType type = doc.GetElement(id) as WallType;
            //创建墙
            Wall old_wall = wall as Wall;
            Wall new_wall;
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
                        TaskDialog.Show("ERROR", failureProcessor.FailureMessage);
                    }
                }
            }

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
        public static void ExtendCurves(Document doc, Element ele1, Element ele2)
        {
            Wall wall_one = ele1 as Wall;
            Wall wall_two = ele2 as Wall;
            //若两面墙都是直线
            LocationCurve wallcurve1 = wall_one.Location as LocationCurve;
            LocationCurve wallcurve2 = wall_two.Location as LocationCurve;
            Curve wallcur1 = wallcurve1.Curve;
            Curve wallcur2 = wallcurve2.Curve;
            if (wallcur1.ToString().Contains("Line") & wallcur2.ToString().Contains("Line"))
            {
                Line wallline1 = wallcur1 as Line;
                XYZ direction1 = wallline1.Direction;
                IList<XYZ> coordinate1 = wallline1.Tessellate();
                Line wallline2 = wallcurve2.Curve as Line;
                XYZ direction2 = wallline2.Direction;
                IList<XYZ> coordinate2 = wallline2.Tessellate();
                //判断平行或相交
                double a1 = coordinate1[0].Y - coordinate1[1].Y;
                double a2 = coordinate2[0].Y - coordinate2[1].Y;
                double b1 = coordinate1[1].X - coordinate1[0].X;
                double b2 = coordinate2[1].X - coordinate2[0].X;
                double c1 = coordinate1[0].X * coordinate1[1].Y - coordinate1[1].X * coordinate1[0].Y;
                double c2 = coordinate2[0].X * coordinate2[1].Y - coordinate2[1].X * coordinate2[0].Y;
                double D = a1 * b2 - a2 * b1;
                // D = 0表示两直线平行
                if (Math.Abs(D) < 0.01)  //line1 line2 are parallel
                {
                    bool collineation = false;
                    bool superposition = false;
                    bool nearX = false;
                    bool nearY = false;
                    //a=0则平行于X轴
                    if (Math.Abs(a1) <= 0.01 & Math.Abs(a2) <= 0.01)
                    {
                        //TaskDialog.Show("debug", ele1.Id.ToString()+"\n"+ ele2.Id.ToString() + "\n"+ "x direction");
                        if (coordinate1[0].Y - 0.5 <= coordinate2[0].Y & coordinate2[0].Y <= coordinate1[0].Y + 0.5)
                        {
                            collineation = true;
                            if (!(coordinate1[0].Y == coordinate2[0].Y))
                            {
                                nearX = true;
                            }
                        }
                        else
                        {
                            //to do: 平行不共线的墙先连到中点然后再转角连到下一个墙
                        }
                    }
                    //b=0则平行于Y轴
                    else if (Math.Abs(b1) <= 0.01 & Math.Abs(b2) <= 0.01)
                    {
                        //TaskDialog.Show("debug", ele1.Id.ToString() + "\n" + ele2.Id.ToString() + "\n" + "y direction");
                        if (coordinate1[0].X - 0.5 <= coordinate2[0].X & coordinate2[0].X <= coordinate1[0].X + 0.5)
                        {
                            collineation = true;
                            if (!(coordinate1[0].X == coordinate2[0].X))
                            {
                                nearY = true;
                            }
                        }
                        else
                        {
                            //to do: 平行不共线的墙先连到中点然后再转角连到下一个墙
                        }
                    }
                    else
                    {
                        //判断是否共线 
                        double k1 = (coordinate1[0].Y - coordinate2[1].Y) / (coordinate1[0].X - coordinate2[1].X);
                        double k2 = (coordinate1[0].Y - coordinate1[1].Y) / (coordinate1[0].X - coordinate1[1].X);
                        if (k1 >= k2 - 0.005 & k1 <= k2 + 0.005)
                        {
                            TaskDialog.Show("debug", ele1.Id.ToString() + "\n" + ele2.Id.ToString() + "\n" + "not x or y direction but colline");
                            collineation = true;
                        }
                        else
                        {
                            TaskDialog.Show("debug", ele1.Id.ToString() + "\n" + ele2.Id.ToString() + "\n" + "非XY方向平行但不共线，暂未延申该类型的墙");
                        }
                    }
                    if (collineation) //共线
                    {
                        double minx1 = Math.Min(coordinate1[0].X, coordinate1[1].X) - 0.01;
                        double maxx1 = Math.Max(coordinate1[0].X, coordinate1[1].X) + 0.01;
                        double miny1 = Math.Min(coordinate1[0].Y, coordinate1[1].Y) - 0.01;
                        double maxy1 = Math.Max(coordinate1[0].Y, coordinate1[1].Y) + 0.01;
                        double minx2 = Math.Min(coordinate2[0].X, coordinate2[1].X) - 0.01;
                        double maxx2 = Math.Max(coordinate2[0].X, coordinate2[1].X) + 0.01;
                        double miny2 = Math.Min(coordinate2[0].Y, coordinate2[1].Y) - 0.01;
                        double maxy2 = Math.Max(coordinate2[0].Y, coordinate2[1].Y) + 0.01;
                        //判断是否有重合部分
                        if ((minx1 <= coordinate2[0].X & coordinate2[0].X <= maxx1 & miny1 <= coordinate2[0].Y & coordinate2[0].Y <= maxy1) | (minx1 <= coordinate2[1].X & coordinate2[1].X <= maxx1 & miny1 <= coordinate2[1].Y & coordinate2[1].Y <= maxy1) | minx2 <= coordinate1[0].X & coordinate1[0].X <= maxx2 & miny2 <= coordinate1[0].Y & coordinate1[0].Y <= maxy2 | minx2 <= coordinate1[1].X & coordinate1[1].X <= maxx2 & miny2 <= coordinate1[1].Y & coordinate1[1].Y <= maxy2)
                        {
                            superposition = true;
                        }
                    }

                    //将不重合的两直线距离最近的点连起来
                    if (collineation & (!superposition) & !(nearY | nearX))
                    {
                        //TaskDialog.Show("debug", ele1.Id.ToString() + "\n" + ele2.Id.ToString() + "\n" + "collineate wall trim");
                        XYZ point1 = new XYZ();
                        XYZ point2 = new XYZ();
                        double distance1 = Math.Pow((coordinate1[0].X - coordinate2[0].X), 2) + Math.Pow((coordinate1[0].Y - coordinate2[0].Y), 2);
                        double distance2 = Math.Pow((coordinate1[0].X - coordinate2[1].X), 2) + Math.Pow((coordinate1[0].Y - coordinate2[1].Y), 2);
                        double distance3 = Math.Pow((coordinate1[1].X - coordinate2[0].X), 2) + Math.Pow((coordinate1[1].Y - coordinate2[0].Y), 2);
                        double distance4 = Math.Pow((coordinate1[1].X - coordinate2[1].X), 2) + Math.Pow((coordinate1[1].Y - coordinate2[1].Y), 2);
                        List<double> distance = new List<double>();
                        distance.Add(distance1); distance.Add(distance2); distance.Add(distance3); distance.Add(distance4);
                        double min_dis = distance.Min();
                        if (distance1 == min_dis)
                        {
                            point1 = coordinate1[0];
                            point2 = coordinate2[0];
                        }
                        else if (distance2 == min_dis)
                        {
                            point1 = coordinate1[0];
                            point2 = coordinate2[1];
                        }
                        else if (distance3 == min_dis)
                        {
                            point1 = coordinate1[1];
                            point2 = coordinate2[0];
                        }
                        else if (distance4 == min_dis)
                        {
                            point1 = coordinate1[1];
                            point2 = coordinate2[1];
                        }
                        else { TaskDialog.Show("Warning: TRIM", wall_one.Id + " / " + wall_two.Id + "\n俩共线墙的point查找失败，不可能发生的情况"); }
                        try
                        {
                            Create_wall(doc, point1, point2, wall_one);
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Error:", "共线墙replace\n" + wall_one.Id + " / " + wall_two.Id + "\n" + ex.Message);
                        }
                    }
                    else if (collineation & (!superposition) & (nearY | nearX))
                    {
                        if (nearX)
                        {
                            XYZ point1 = new XYZ();
                            XYZ point2 = new XYZ();
                            double distance1 = Math.Pow((coordinate1[0].X - coordinate2[0].X), 2) + Math.Pow((coordinate1[0].Y - coordinate2[0].Y), 2);
                            double distance2 = Math.Pow((coordinate1[0].X - coordinate2[1].X), 2) + Math.Pow((coordinate1[0].Y - coordinate2[1].Y), 2);
                            double distance3 = Math.Pow((coordinate1[1].X - coordinate2[0].X), 2) + Math.Pow((coordinate1[1].Y - coordinate2[0].Y), 2);
                            double distance4 = Math.Pow((coordinate1[1].X - coordinate2[1].X), 2) + Math.Pow((coordinate1[1].Y - coordinate2[1].Y), 2);
                            List<double> distance = new List<double>();
                            distance.Add(distance1); distance.Add(distance2); distance.Add(distance3); distance.Add(distance4);
                            double min_dis = distance.Min();
                            if (distance1 == min_dis)
                            {
                                point1 = coordinate1[0];
                                //point2 = coordinate2[0];
                                point2 = new XYZ(coordinate2[0].X, coordinate1[0].Y, coordinate1[0].Z);
                            }
                            else if (distance2 == min_dis)
                            {
                                point1 = coordinate1[0];
                                //point2 = coordinate2[1];
                                point2 = new XYZ(coordinate2[1].X, coordinate1[0].Y, coordinate1[0].Z);
                            }
                            else if (distance3 == min_dis)
                            {
                                point1 = coordinate1[1];
                                //point2 = coordinate2[0];
                                point2 = new XYZ(coordinate2[0].X, coordinate1[1].Y, coordinate1[1].Z);
                            }
                            else if (distance4 == min_dis)
                            {
                                point1 = coordinate1[1];
                                //point2 = coordinate2[1];
                                point2 = new XYZ(coordinate2[1].X, coordinate1[1].Y, coordinate1[1].Z);
                            }
                            try
                            {
                                Create_wall(doc, point1, point2, wall_one);
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show("Error:", "nearX共线墙replace\n" + wall_one.Id + " / " + wall_two.Id + "\n" + ex.Message);
                            }
                        }
                        if (nearY)
                        {
                            XYZ point1 = new XYZ();
                            XYZ point2 = new XYZ();
                            double distance1 = Math.Pow((coordinate1[0].X - coordinate2[0].X), 2) + Math.Pow((coordinate1[0].Y - coordinate2[0].Y), 2);
                            double distance2 = Math.Pow((coordinate1[0].X - coordinate2[1].X), 2) + Math.Pow((coordinate1[0].Y - coordinate2[1].Y), 2);
                            double distance3 = Math.Pow((coordinate1[1].X - coordinate2[0].X), 2) + Math.Pow((coordinate1[1].Y - coordinate2[0].Y), 2);
                            double distance4 = Math.Pow((coordinate1[1].X - coordinate2[1].X), 2) + Math.Pow((coordinate1[1].Y - coordinate2[1].Y), 2);
                            List<double> distance = new List<double>();
                            distance.Add(distance1); distance.Add(distance2); distance.Add(distance3); distance.Add(distance4);
                            double min_dis = distance.Min();
                            if (distance1 == min_dis)
                            {
                                point1 = coordinate1[0];
                                //point2 = coordinate2[0];
                                point2 = new XYZ(coordinate1[0].X, coordinate2[0].Y, coordinate1[0].Z);
                            }
                            else if (distance2 == min_dis)
                            {
                                point1 = coordinate1[0];
                                //point2 = coordinate2[1];
                                point2 = new XYZ(coordinate1[0].X, coordinate2[1].Y, coordinate1[0].Z);
                            }
                            else if (distance3 == min_dis)
                            {
                                point1 = coordinate1[1];
                                //point2 = coordinate2[0];
                                point2 = new XYZ(coordinate1[1].X, coordinate2[0].Y, coordinate1[1].Z);
                            }
                            else if (distance4 == min_dis)
                            {
                                point1 = coordinate1[1];
                                //point2 = coordinate2[1];
                                point2 = new XYZ(coordinate1[1].X, coordinate2[1].Y, coordinate1[1].Z);
                            }
                            try
                            {
                                Create_wall(doc, point1, point2, wall_one);
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show("Error:", "nearY共线墙replace\n" + wall_one.Id + " / " + wall_two.Id + "\n" + ex.Message);
                            }
                        }
                    }
                    else //to do:平行不共线的暂时没有处理
                    {
                        if (!collineation)
                        { TaskDialog.Show("Warning: TRIM", wall_one.Id + " / " + wall_two.Id + "\n与柱子相交的两面墙平行不共线"); }
                        if (superposition)
                        {
                            TaskDialog.Show("Warning: TRIM", wall_one.Id + " / " + wall_two.Id + "\n与柱子相交的两面墙重合");
                        }
                    }
                }
                else if (Math.Abs(D) >= 0.01)
                {
                    double cross_x = (b1 * c2 - b2 * c1) / D;
                    double cross_y = (a2 * c1 - a1 * c2) / D;
                    XYZ cross = new XYZ(cross_x, cross_y, coordinate1[0].Z);
                    //TaskDialog.Show("debug", ele1.Id.ToString()+":\n"+coordinate1[0].X.ToString() +"\n"+ coordinate1[0].Y.ToString()+"\n" + cross_x.ToString()+"\n"+cross_y.ToString());
                    //判断交点是否在两线段内,交点不在线段内的则延申墙
                    //if (!((Math.Min(coordinate1[0].X, coordinate1[1].X) - 0.01) < cross_x & (Math.Max(coordinate1[0].X, coordinate1[1].X) + 0.01) > cross_x & (Math.Min(coordinate1[0].Y, coordinate1[1].Y) - 0.01) < cross_y & (Math.Max(coordinate1[0].Y, coordinate1[1].Y) + 0.01) > cross_y))
                    //{
                        //if (!((Math.Min(coordinate2[0].X, coordinate2[1].X) - 0.01) < cross_x & (Math.Max(coordinate2[0].X, coordinate2[1].X) + 0.01) > cross_x & (Math.Min(coordinate2[0].Y, coordinate2[1].Y) - 0.01) < cross_y & (Math.Max(coordinate2[0].Y, coordinate2[1].Y) + 0.01) > cross_y))
                        //{
                            double distance11 = Math.Pow((coordinate1[0].X - cross.X), 2) + Math.Pow((coordinate1[0].Y - cross.Y), 2);
                            double distance12 = Math.Pow((coordinate1[1].X - cross.X), 2) + Math.Pow((coordinate1[1].Y - cross.Y), 2);
                            double distance21 = Math.Pow((coordinate2[0].X - cross.X), 2) + Math.Pow((coordinate2[0].Y - cross.Y), 2);
                            double distance22 = Math.Pow((coordinate2[1].X - cross.X), 2) + Math.Pow((coordinate2[1].Y - cross.Y), 2);
                            //TaskDialog.Show("test", "延申不平行的墙");
                            //to do:可以根据生成房间的分辨率设置一个延申墙的分辨率
                            if (Math.Min(distance11, distance12) >= 0.05)
                            {
                                if (distance11 < distance12)
                                {
                                    try
                                    {
                                        Create_wall(doc, coordinate1[0], cross, wall_one);
                                    }
                                    catch (Exception ex)
                                    {
                                        TaskDialog.Show("Eroor:", "replaced by crossed walls:\n" + wall_one.Id + " / " + wall_two.Id + "\n" + ex.Message);
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        Create_wall(doc, coordinate1[1], cross, wall_one);
                                    }
                                    catch (Exception ex)
                                    {
                                        TaskDialog.Show("Eroor:", "replaced by crossed walls:\n" + wall_one.Id + " / " + wall_two.Id + "\n" + ex.Message);
                                    }
                                }
                            }
                            if (Math.Min(distance21, distance22) >= 0.05)
                            {
                                if (distance21 < distance22)
                                {
                                    try
                                    {
                                        Create_wall(doc, coordinate2[0], cross, wall_one);
                                    }
                                    catch (Exception ex)
                                    {
                                        TaskDialog.Show("Eroor:", "replaced by crossed walls:\n" + wall_one.Id + " / " + wall_two.Id + "\n" + ex.Message);
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        Create_wall(doc, coordinate2[1], cross, wall_one);
                                    }
                                    catch (Exception ex)
                                    {
                                        TaskDialog.Show("Eroor:", "replaced by crossed walls:\n" + wall_one.Id + " / " + wall_two.Id + "\n" + ex.Message);
                                    }
                                }
                            }
                        //}
                    //}
                }
            }
            else
            {
                TaskDialog.Show("To do", "需要延申的墙是Arc类型的墙，暂未考虑");
            }
        }


        //extend colinear walls
        public static List<XYZ> ColinearPoint(Element ele1, Element ele2)
        {
            Wall wall1 = ele1 as Wall;
            line line1 = general.GetLineFromWall(wall1);
            Wall wall2 = ele2 as Wall;
            line line2 = general.GetLineFromWall(wall2);
            XYZ point1 = new XYZ();
            XYZ point2 = new XYZ();
           
            double distance1 = Math.Sqrt(Math.Pow((line1.sp.X - line2.sp.X), 2) + Math.Pow((line1.sp.Y - line2.sp.Y), 2));
            double distance2 = Math.Pow((line1.sp.X - line2.ep.X), 2) + Math.Pow((line1.sp.Y - line2.ep.Y), 2);
            double distance3 = Math.Pow((line1.ep.X - line2.sp.X), 2) + Math.Pow((line1.ep.Y - line2.sp.Y), 2);
            double distance4 = Math.Pow((line1.ep.X - line2.ep.X), 2) + Math.Pow((line1.ep.Y - line2.ep.Y), 2);
            List<double> distance = new List<double>();
            distance.Add(distance1); distance.Add(distance2); distance.Add(distance3); distance.Add(distance4);
            double max_dis = distance.Max();
            if (distance1 == max_dis)
            {
                point1 = line1.sp;
                point2 = line2.sp;
            }
            else if (distance2 == max_dis)
            {
                point1 = line1.sp;
                point2 = line2.ep;
            }
            else if (distance3 == max_dis)
            {
                point1 = line1.ep;
                point2 = line2.sp;
            }
            else if (distance4 == max_dis)
            {
                point1 = line1.ep;
                point2 = line2.ep;
            }

            List<XYZ> points = new List<XYZ>();
            points.Add(point1);
            points.Add(point2);
            return points;
        }
    }
}

