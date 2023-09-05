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
    class deprecated_code
    {
        public static void TrimExtendCurves(Document doc, Element ele1, Element ele2)
        {
            /*
            double min_distance(XYZ l11, XYZ l12, XYZ l21, XYZ l22)
            {
                double distance1 = Math.Pow((l11.X - l21.X), 2) + Math.Pow((l11.Y - l21.Y), 2);
                double distance2 = Math.Pow((l11.X - l22.X), 2) + Math.Pow((l11.Y - l22.Y), 2);
                double distance3 = Math.Pow((l12.X - l21.X), 2) + Math.Pow((l12.Y - l21.Y), 2);
                double distance4 = Math.Pow((l12.X - l22.X), 2) + Math.Pow((l12.Y - l22.Y), 2);
                List<double> distance = new List<double>();
                distance.Add(distance1); distance.Add(distance2); distance.Add(distance3); distance.Add(distance4);
                double min_dis = distance.Min();
                return distance1 distance2  distance3  distance4  min_dis;
            }
            */
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
                            TaskDialog.Show("debug", ele1.Id.ToString() + "\n" + ele2.Id.ToString() + "\n" + "非XY方向平行但不共线，暂为延申该类型的墙");
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
                            function.Create_wall(doc, point1, point2, wall_one);
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
                                function.Create_wall(doc, point1, point2, wall_one);
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
                                function.Create_wall(doc, point1, point2, wall_one);
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
                    if (!((Math.Min(coordinate1[0].X, coordinate1[1].X) - 0.01) < cross_x & (Math.Max(coordinate1[0].X, coordinate1[1].X) + 0.01) > cross_x & (Math.Min(coordinate1[0].Y, coordinate1[1].Y) - 0.01) < cross_y & (Math.Max(coordinate1[0].Y, coordinate1[1].Y) + 0.01) > cross_y))
                    {
                        if (!((Math.Min(coordinate2[0].X, coordinate2[1].X) - 0.01) < cross_x & (Math.Max(coordinate2[0].X, coordinate2[1].X) + 0.01) > cross_x & (Math.Min(coordinate2[0].Y, coordinate2[1].Y) - 0.01) < cross_y & (Math.Max(coordinate2[0].Y, coordinate2[1].Y) + 0.01) > cross_y))
                        {
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
                                        function.Create_wall(doc, coordinate1[0], cross, wall_one);
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
                                        function.Create_wall(doc, coordinate1[1], cross, wall_one);
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
                                        Wall new_wall = function.Create_wall(doc, coordinate2[0], cross, wall_one);
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
                                        function.Create_wall(doc, coordinate2[1], cross, wall_one);
                                    }
                                    catch (Exception ex)
                                    {
                                        TaskDialog.Show("Eroor:", "replaced by crossed walls:\n" + wall_one.Id + " / " + wall_two.Id + "\n" + ex.Message);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                TaskDialog.Show("To do", "需要延申的墙是Arc类型的墙，暂未考虑");
            }
        }

        //原来的main函数中的
        /*
         foreach (ElementId columnid in Columns_walls.Keys)
            {
                //TaskDialog.Show("debug", "柱子分割线" + columnid.ToString());
                //只有一面墙和柱子相交，则直接删除柱子
                if (Columns_walls[columnid].Count == 1)
                {
                    try
                    {
                        using (var ts = new Transaction(doc, "delete column"))
                        {
                            ts.Start();
                            ICollection<ElementId> delete = doc.Delete(columnid);
                            ts.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("ERROR:delete columns", "delete columns,该柱子删除报错：只有一面墙和柱子相交:\n" + columnid.ToString() + ex.Message);
                    }
                    //TaskDialog.Show("debug", "一面墙相交");
                }


                
                //多面墙：判断墙与柱子是否在一个标高上，若一个标高上只有一个墙则直接删除柱子，两面以上才延申。在一个标高的先延申墙再删除柱子
                else if (Columns_walls[columnid].Count > 1)
                {
                    List<Element> samelevelwall = new List<Element>();
                    FamilyInstance col = doc.GetElement(columnid) as FamilyInstance;
                    Level levelco = doc.GetElement(col.LevelId) as Level;
                    for (int i = 0; i < (Columns_walls[columnid].Count); i++)
                    {
                        Level level1 = doc.GetElement(Columns_walls[columnid][i].LevelId) as Level;
                        if (levelco.Elevation-0.5 <= level1.Elevation & level1.Elevation <= levelco.Elevation+0.5)
                        {
                            samelevelwall.Add(Columns_walls[columnid][i]);
                        }
                        else
                        {
                            wall_level_pro += "柱子id："+ columnid + " 标高:" + levelco.Name + "\n";
                            wall_level_pro += "与柱子不在同一标高的墙id："+Columns_walls[columnid][i].Id + " 标高:"+ level1.Name + "\n";
                            wall_level_pro += "*************************\n";
                        }
                    }
                    if (samelevelwall.Count == 1)
                    {
                        try
                        {
                            using (var ts = new Transaction(doc, "delete column"))
                            {
                                ts.Start();
                                ICollection<ElementId> delete = doc.Delete(columnid);
                                ts.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("ERROR:delete columns", "delete columns,删除柱子报错：和该柱子相交的墙有两个以上，但和柱子相交且在同一个标高上的墙只有一个\n" + columnid.ToString() + ex.Message);
                        }
                        //TaskDialog.Show("debug", "一面墙相交2");
                    }
                    else
                    {
                        bool extend_succeed = false;
                        List<Wall> new_walls = new List<Wall>();
                        if (samelevelwall.Count == 2)
                        {
                            Wall wall1 = samelevelwall[0] as Wall;
                            Wall wall2 = samelevelwall[1] as Wall;
                            line line1 = basic.GetLine(wall1);
                            line line2 = basic.GetLine(wall2);
                            bool parallel = function.IsParallel(line1, line2);
                            if (!parallel)
                            {
                                new_walls = function.ExtendCrossWall(doc, line1, line2, wall1, wall2);
                            }
                            else
                            {
                                bool collineation = function.IsCollineation(line1, line2, wall1, wall2);
                                if (collineation)
                                {
                                    bool superposition = function.IsSuperposition(line1, line2);
                                    if (!superposition)
                                    {
                                        new_walls.Add(function.ConnectTwoWalls(doc, line1, line2, wall1, wall2));
                                    }
                                    else
                                    {
                                        TaskDialog.Show("prompt", "该两共线墙重合，暂时考虑不做处理：\n" + wall1.Id.ToString() + "/" + wall2.Id.ToString());
                                        extend_succeed = true;
                                    }
                                }
                                else
                                {
                                    double distance = function.gener.LineDistance(line1, line2, wall1, wall2);
                                    line trans_line = function.TranslationLine(line1, line2);
                                    bool superposition = function.IsSuperposition(line1, trans_line);
                                    if (distance <= (wall1.Width+wall2.Width)) //先平移，再当作共线墙处理
                                    {
                                        new_walls.Add(function.ExtendNearParallelWall(doc, line1, line2, wall1, wall2));
                                    }
                                    else //两距离太远的平行墙，将其连起来
                                    {
                                        new_walls = function.ExtendFarParallelWall(doc, line1, line2, wall1, wall2);
                                    }
                                }
                            }
                        }

                        else if (samelevelwall.Count == 3)
                        {
                            
                            function.ConnectThreeWalls(doc,samelevelwall[0] as Wall, samelevelwall[1] as Wall, samelevelwall[2] as Wall);
                        }
                        else if (samelevelwall.Count > 3)
                        {
                            //先debug，暂时还没写，用的以前的代码
                            for (int i = 0; i < samelevelwall.Count - 1; i++)
                            {
                                Wall wall1 = samelevelwall[i] as Wall;
                                Wall wall2 = samelevelwall[i + 1] as Wall;
                                LocationCurve wallcurve1 = wall1.Location as LocationCurve;
                                LocationCurve wallcurve2 = wall2.Location as LocationCurve;
                                Curve wallcur1 = wallcurve1.Curve;
                                Curve wallcur2 = wallcurve2.Curve;
                                if (wallcur1.ToString().Contains("Line") & wallcur2.ToString().Contains("Line"))
                                {
                                    deprecated_code.TrimExtendCurves(doc, samelevelwall[i], samelevelwall[i + 1]);
                                    extend_succeed = true;
                                }
                                else
                                {
                                    TaskDialog.Show("to do", wall1.Id.ToString() + "/" + wall2.Id.ToString() + ": \n与柱子相交的墙中存在ARC类型的墙，暂未考虑");
                                }
                            }
                        }
                        if (new_walls.Count != 0)
                        {
                            extend_succeed = true;
                        }
                        if (extend_succeed)
                        {
                            try
                            {
                                using (var ts = new Transaction(doc, "delete column"))
                                {
                                    ts.Start();
                                    ICollection<ElementId> delete = doc.Delete(columnid);
                                    dealwitherror.MyFailuresPreprocessor failureProcessor = new dealwitherror.MyFailuresPreprocessor();
                                    ts.Commit();
                                }
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show("ERROR:delete columns", "delete columns,删除柱子报错：和柱子相交且在同一标高的柱子有多个\n" + columnid.ToString() + ex.Message);
                            }
                            //TaskDialog.Show("debug", "多面墙相交");
                        }
                        else
                        {
                             Keeped_columns_pro += columnid.ToString() + "\n";
                        }
                    }
                }
                
            }
         */
    }
}

