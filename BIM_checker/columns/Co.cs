using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace column_select
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]

    public class Co : IExternalCommand
    {
        //定义一个错误处理类
        public class MyFailuresPreprocessor : IFailuresPreprocessor
        {
            private string _failureMessage;
            private bool _hasError;
            public string FailureMessage
            {
                get { return _failureMessage; }
                set { _failureMessage = value; }
            }
            public bool HasError
            {
                get { return _hasError; }
                set { _hasError = value; }
            }
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                //获取所有的失败信息
                IList<FailureMessageAccessor> failures = failuresAccessor.GetFailureMessages();
                if (failures.Count == 0)
                    return FailureProcessingResult.Continue;

                foreach (FailureMessageAccessor failure in failures)
                {
                    //如果是错误则尝试解决
                    if (failure.GetSeverity() == FailureSeverity.Error)
                    {
                        _failureMessage = failure.GetDescriptionText(); // get the failure description                
                        _hasError = true;
                        TaskDialog.Show("错误警告", "FailureProcessingResult.ProceedWithRollBack");
                        return FailureProcessingResult.ProceedWithRollBack;
                    }
                    //如果是警告，则禁止弹框
                    if (failure.GetSeverity() == FailureSeverity.Warning)
                    {
                        failuresAccessor.DeleteWarning(failure);
                    }
                }
                return FailureProcessingResult.Continue;
            }
        }
        public static void Create_wall(Document doc, XYZ sp, XYZ ep, Element wall)
        {
            Wall old_wall = wall as Wall;
            using (var ts = new Transaction(doc, "create wall"))
            {
                ts.Start();
                Wall new_wall = Wall.Create(doc, Line.CreateBound(sp, ep), old_wall.LevelId, false);
                new_wall.ChangeTypeId(old_wall.GetTypeId());
                //to do:设置顶部约束
                MyFailuresPreprocessor failureProcessor = new MyFailuresPreprocessor();
                ts.Commit();
            }
        }
        public void TrimExtendCurves(Document doc, Element ele1, Element ele2)
        {
            Wall wall_one = ele1 as Wall;
            Wall wall_two = ele2 as Wall;
            LocationCurve wallcurve1 = wall_one.Location as LocationCurve;
            Line wallline1 = wallcurve1.Curve as Line;
            XYZ direction1 = wallline1.Direction;
            IList<XYZ> coordinate1 = wallline1.Tessellate();
            LocationCurve wallcurve2 = wall_two.Location as LocationCurve;
            Line wallline2 = wallcurve2.Curve as Line;
            XYZ direction2 = wallline2.Direction;
            IList<XYZ> coordinate2 = wallline2.Tessellate();
            bool superposition = false;

            //找交点或者判断是否重合
            double a1 = coordinate1[0].Y - coordinate1[1].Y;
            double a2 = coordinate2[0].Y - coordinate2[1].Y;
            double b1 = coordinate1[1].X - coordinate1[0].X;
            double b2 = coordinate2[1].X - coordinate2[0].X;
            double c1 = coordinate1[0].X * coordinate1[1].Y - coordinate1[1].X * coordinate1[0].Y;
            double c2 = coordinate2[0].X * coordinate2[1].Y - coordinate2[1].X * coordinate2[0].Y;
            double D = a1 * b2 - a2 * b1;
            // D = 0表示两直线平行
            if (direction1 == direction2 | direction1 == -direction2)  //line1 line2 are parallel
            {
                bool collineation = false;
                //a=0则平行于X轴
                if ( a1 == 0 & a2 == 0 )
                {
                    if (coordinate1[0].X == coordinate2[0].X)
                    {
                        collineation = true;
                    }
                }
                if (b1 == 0 & b2 == 0)
                {
                    if (coordinate1[0].Y == coordinate2[0].Y)
                    {
                        collineation = true;
                    }
                }
                if (a1 != 0 & a2 != 0 & b1 != 0 & b2 != 0)
                {
                    //判断是否共线 
                    double k1 = (coordinate1[0].Y - coordinate2[1].Y) / (coordinate1[0].X - coordinate2[1].X);
                    double k2 = (coordinate1[1].Y - coordinate2[0].Y) / (coordinate1[1].X - coordinate2[0].X);
                    if (k1 == k2)
                    {
                        collineation = true;
                    }
                }
                if (collineation) //共线
                {
                    //判断是否重合
                    if ((Math.Min(coordinate2[0].X, coordinate2[1].X) <= coordinate1[0].X & coordinate1[0].X <= Math.Max(coordinate2[0].X, coordinate2[1].X)) | (Math.Min(coordinate2[0].X, coordinate2[1].X) <= coordinate1[1].X & coordinate1[1].X <= Math.Max(coordinate2[0].X, coordinate2[1].X)))
                    {
                        if ((Math.Min(coordinate2[0].Y, coordinate2[1].Y) <= coordinate1[0].Y & coordinate1[0].Y <= Math.Max(coordinate2[0].Y, coordinate2[1].Y)) | (Math.Min(coordinate2[0].Y, coordinate2[1].Y) <= coordinate1[1].Y & coordinate1[1].Y <= Math.Max(coordinate2[0].Y, coordinate2[1].Y)))
                        {
                            //point of line1 in line2
                            superposition = true;
                        }
                    }
                    else if ((Math.Min(coordinate1[0].X, coordinate1[1].X) <= coordinate2[0].X & coordinate2[0].X <= Math.Max(coordinate1[0].X, coordinate1[1].X)) | (Math.Min(coordinate1[0].X, coordinate1[1].X) <= coordinate2[1].X & coordinate2[1].X <= Math.Max(coordinate1[0].X, coordinate1[1].X)))
                    {
                        if ((Math.Min(coordinate1[0].Y, coordinate1[1].Y) <= coordinate2[0].Y & coordinate2[0].Y <= Math.Max(coordinate1[0].Y, coordinate1[1].Y)) | (Math.Min(coordinate1[0].Y, coordinate1[1].Y) <= coordinate2[1].Y & coordinate2[1].Y <= Math.Max(coordinate1[0].Y, coordinate1[1].Y)))
                        {
                            //point of line2 in line1
                            superposition = true;
                        }
                    }
                    //共线不重合
                    if (!superposition)
                    {
                        XYZ point1 = new XYZ();
                        XYZ point2 = new XYZ();
                        double distance1 = Math.Pow((coordinate1[0].X - coordinate2[0].X), 2) + Math.Pow((coordinate1[0].Y - coordinate2[0].Y), 2);
                        double distance2 = Math.Pow((coordinate1[0].X - coordinate2[1].X), 2) + Math.Pow((coordinate1[0].Y - coordinate2[1].Y), 2);
                        double distance3 = Math.Pow((coordinate1[1].X - coordinate2[0].X), 2) + Math.Pow((coordinate1[1].Y - coordinate2[0].Y), 2);
                        double distance4 = Math.Pow((coordinate1[1].X - coordinate2[1].X), 2) + Math.Pow((coordinate1[0].Y - coordinate2[1].Y), 2);
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
                            TaskDialog.Show("test",point1.ToString()+point2.ToString());
                            Create_wall(doc, point1, point2, wall_one);
                        }
                        catch(Exception ex)
                        {
                            TaskDialog.Show("Error:","共线墙replace\n"+wall_one.Id+" / "+wall_two.Id+"\n"+ex.Message);
                        }
                    }
                    else //to do:重合的暂时没有处理
                    { TaskDialog.Show("Warning: TRIM", wall_one.Id + " / " + wall_two.Id + "\n与柱子相交的两面墙重合"); }
                }
                else//to do:不共线的暂时没有处理
                {
                    TaskDialog.Show("Warning: TRIM", wall_one.Id + " / " + wall_two.Id + "\n与柱子相交的两面墙平行且不共线");
                }
            }
            else if( Math.Abs(D) >= 0.001 )
            {
                double cross_x = (b1 * c2 - b2 * c1) / D;
                double cross_y = (a2 * c1 - a1 * c2) / D;
                XYZ cross = new XYZ(cross_x, cross_y, coordinate1[0].Z);
                double distance11 = Math.Pow((coordinate1[0].X - cross.X), 2) + Math.Pow((coordinate1[0].Y - cross.Y), 2);
                double distance12 = Math.Pow((coordinate1[1].X - cross.X), 2) + Math.Pow((coordinate1[1].Y - cross.Y), 2);
                double distance21 = Math.Pow((coordinate2[0].X - cross.X), 2) + Math.Pow((coordinate2[0].Y - cross.Y), 2);
                double distance22 = Math.Pow((coordinate2[1].X - cross.X), 2) + Math.Pow((coordinate2[1].Y - cross.Y), 2);
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
                            TaskDialog.Show("Eroor:", "repalced by crossed walls:\n" + wall_one.Id + " / " + wall_two.Id + "\n" +ex.Message);
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
                            TaskDialog.Show("Eroor:", "repalced by crossed walls:\n" + wall_one.Id + " / " + wall_two.Id + "\n" + ex.Message);
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
                            TaskDialog.Show("Eroor:", "repalced by crossed walls:\n"+ wall_one.Id +" / "+wall_two.Id + "\n" + ex.Message);
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
                            TaskDialog.Show("Eroor:", "repalced by crossed walls:\n" + wall_one.Id + " / " + wall_two.Id + "\n" + ex.Message);
                        }
                    }
                }
            }
        }
        public List<Face> GetFacesFromSolid(Solid solid)
        {
            var faces = new List<Face>();
            foreach (Face face in solid.Faces)
            {
                faces.Add(face);
            }
            return faces;
        }

        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document doc = revit.Application.ActiveUIDocument.Document;
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            FilteredElementCollector collector_s = new FilteredElementCollector(doc);
            //把柱子筛选出来，柱子可能有建筑柱，也可能有结构柱
            ElementCategoryFilter Fcolumns = new ElementCategoryFilter(BuiltInCategory.OST_Columns);
            ElementCategoryFilter FScolumns = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);
            ICollection<ElementId> columns = collector.WherePasses(Fcolumns).ToElementIds();
            ICollection<ElementId> Scolumns = collector_s.WherePasses(FScolumns).ToElementIds();
            List<ElementId> co = new List<ElementId>();
            foreach (ElementId el in columns)
            {
                Element element = doc.GetElement(el);
                if (element.GetType().ToString().Contains("Instance"))
                {
                    co.Add(el);
                }
            }
            foreach (ElementId el in Scolumns)
            {
                Element element = doc.GetElement(el);
                if (element.GetType().ToString().Contains("Instance"))
                {
                    co.Add(el);
                }
            }

            //查找与其他元素相交的柱子
            /*
            string co_pro = "Those columns intersecting with walls:\n";
            List<ElementId> co_inter = new List<ElementId>();
            foreach (ElementId el in co)
            {
                Element column = doc.GetElement(el);
                FilteredElementCollector colle = new FilteredElementCollector(doc);
                colle.WherePasses(new ElementIntersectsElementFilter(column));
                if (colle.Count() > 0)
                {
                    co_inter.Add(el);
                    co_pro += el.ToString() +"\n";
                }
            }
            TaskDialog.Show("intersecting columns",co_pro);
            */

            //找到与柱子相邻或相交的柱子
            string prompt = "Those columns near walls:\n" ;
            Dictionary<ElementId, IList<Element>> Columns_walls = new Dictionary<ElementId, IList<Element>>();

            List<ElementId> deletes = new List<ElementId>();
            foreach (ElementId el in co)
            {
                bool near_wall = false;
                Element column = doc.GetElement(el);
                //将column solid并向外延伸，找到与之相交的墙，写入到dictionary中
                Options option = new Options();
                option.ComputeReferences = true;
                option.DetailLevel = ViewDetailLevel.Fine;
                GeometryElement geomElement = column.get_Geometry(option);
                Solid solid = null;
                foreach (GeometryObject geomObj in geomElement)
                { 
                    solid = geomObj as Solid;
                    if (solid != null)
                    {
                        if (solid.SurfaceArea != 0)
                        {
                            break;
                        }
                    } 
                }
                //solid完成
                if (solid != null)
                {
                    var faces = GetFacesFromSolid(solid);
                    foreach (var face in faces)
                    {
                        var planarFace = face as PlanarFace;
                        //忽略掉顶面和底面
                        if (planarFace.FaceNormal.IsAlmostEqualTo(new XYZ(0, 0, 1)) || planarFace.FaceNormal.IsAlmostEqualTo(new XYZ(0, 0, -1)))
                        {
                            continue;
                        }
                        //由face转变为拉伸所需要的截面profile
                        var profiles = planarFace.GetEdgesAsCurveLoops();
                        //生成拉伸体solid
                        var solid_new = GeometryCreationUtilities.CreateExtrusionGeometry(profiles, planarFace.FaceNormal, 0.5);
                        /*
                        using (var ts = new Transaction(doc, "solid_new 可视化"))
                        {
                            ts.Start();
                            var ds = DirectShape.CreateElement(doc, new ElementId(-2000151));
                            ds.AppendShape(new List<GeometryObject> { solid_new });
                            ts.Commit();
                        }
                        */
                        FilteredElementCollector Familycollector = new FilteredElementCollector(doc);
                        ElementCategoryFilter wallfilter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
                        Familycollector.WherePasses(wallfilter);
                        try
                        {
                            IList<Element> near_walls = Familycollector.WherePasses(new ElementIntersectsSolidFilter(solid_new)).ToElements(); // Apply intersection filter to find matches
                            if (near_walls.Count() > 0)
                            {
                                if (!prompt.Contains(el.ToString()))
                                {
                                    prompt += el.ToString() + "\n";
                                    Columns_walls.Add(el, near_walls);
                                }
                                else
                                {
                                    foreach (Element nearwall in near_walls)
                                    {
                                        //去重
                                        bool haveadd = false;
                                        foreach (Element Added_nearwall in Columns_walls[el])
                                        {
                                            if (Added_nearwall.Id.ToString() == nearwall.Id.ToString())
                                            {
                                                haveadd = true;
                                            }
                                        }
                                        if (!haveadd)
                                        {
                                            Columns_walls[el].Add(nearwall);
                                        }
                                    }
                                }
                                near_wall = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("solid intersrction error", ex.Message);
                        }
                    }
                }
                if (! near_wall)
                {
                    if (!deletes.Contains(el))
                    {
                        deletes.Add(el);
                    }
                }
            }
            using (var ts = new Transaction(doc, "delete some columns"))
            {
                ts.Start();
                ICollection<ElementId> deletedElements = doc.Delete(deletes);
                ts.Commit();
            }
            TaskDialog.Show("columns near wall",prompt);
            string wall_level_pro = "以下的墙与同一柱子相交但不在同一标高，请检查:\n";
            //与墙相交的柱子的处理
            foreach (ElementId columnid in Columns_walls.Keys)
            {
                //只有一面墙和柱子相交，则直接删除柱子
                if (Columns_walls[columnid].Count == 1)
                {
                    using (var ts = new Transaction(doc, "delete column"))
                    {
                        ts.Start();
                        ICollection<ElementId> delete = doc.Delete(columnid);
                        ts.Commit();
                    }
                }
                //多面墙：先创建墙再删除柱子
                if (Columns_walls[columnid].Count > 1)
                {
                    for (int i = 0; i < (Columns_walls[columnid].Count - 1); i ++)
                    {
                        Level level1 = doc.GetElement(Columns_walls[columnid][i].LevelId) as Level;
                        Level level2 = doc.GetElement(Columns_walls[columnid][i+1].LevelId) as Level;
                        //if (Math.Ceiling(level1.Elevation) == Math.Ceiling(level2.Elevation))
                        //{
                            TrimExtendCurves(doc, Columns_walls[columnid][i], Columns_walls[columnid][i + 1]);
                        //}
                        //else
                        //{
                            //wall_level_pro += "****************\n";
                            //wall_level_pro += Columns_walls[columnid][i].Id + "标高:" + Columns_walls[columnid][i].LevelId+ doc.GetElement(Columns_walls[columnid][i].LevelId).Name +"\n";
                            //wall_level_pro += Columns_walls[columnid][i+1].Id + "标高:" + Columns_walls[columnid][i+1].LevelId + doc.GetElement(Columns_walls[columnid][i+1].LevelId).Name + "\n";
                        //}
                    }
                    using (var ts = new Transaction(doc, "delete column"))
                    {
                        ts.Start();
                        ICollection<ElementId> delete = doc.Delete(columnid);
                        //MyFailuresPreprocessor failureProcessor = new MyFailuresPreprocessor();
                        ts.Commit();
                    }
                }
            }
            //TaskDialog.Show("Warning", wall_level_pro);
            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}


/*Wall column_to_wall(Element col )
            {
                Options option = new Options();
                option.ComputeReferences = true;
                option.DetailLevel = ViewDetailLevel.Fine;
                GeometryElement geomElement = col.get_Geometry(option);
                Solid solid = null;
                foreach (GeometryObject geomObj in geomElement)
                {
                    solid = geomObj as Solid;
                    if (solid != null)
                    {
                        if (solid.SurfaceArea != 0)
                        {
                            break;
                        }
                    }
                }
                IList<Curve> profile = new List<Curve>();
                foreach (Edge edge in solid.Edges)
                {
                    Curve curve = edge.AsCurve();
                    profile.Add(curve);
                }
                Wall wall;
                using (var ts = new Transaction(doc, "create wall from curve"))
                {
                    ts.Start();
                    ICollection<ElementId> deletedElement = doc.Delete(col.Id);
                    wall = Wall.Create(doc, profile, false);
                    ts.Commit();
                }
                return wall;
            }

            foreach (ElementId el in co)
            {
                Element column = doc.GetElement(el);
                bool not_deleted = true;
                foreach (ElementId deleted in deletes)
                {
                    if (el.ToString() == deleted.ToString())
                    {
                        continue;
                    }
                }
                if (!not_deleted) continue;
                try
                {
                    Wall newwall = column_to_wall(column);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("ERROR:create wall", ex.Message);
                }
            }

    */

/*
XYZ TrimExtendCurves(Element ele1, Element ele2)
        {
            double a = 0, b = 0;
            int state = 0;
            Wall wall_one = ele1 as Wall;
            Wall wall_two = ele2 as Wall;
            LocationCurve wallcurve1 = wall_one.Location as LocationCurve;
            Line wallline1 = wallcurve1.Curve as Line;
            IList<XYZ> coordinate1 = wallline1.Tessellate();
            LocationCurve wallcurve2 = wall_two.Location as LocationCurve;
            Line wallline2 = wallcurve2.Curve as Line;
            IList<XYZ> coordinate2 = wallline2.Tessellate();
            if (coordinate1[0].X != coordinate1[1].X)
            {
                a = (coordinate1[1].Y - coordinate1[0].Y) / (coordinate1[1].X - coordinate1[0].X);
                state |= 1;
            }
            if (coordinate2[0].X != coordinate2[1].X)
            {
                b = (coordinate2[1].Y - coordinate2[0].Y) / (coordinate2[1].X - coordinate2[0].X);
                state |= 2;
            }
            switch (state)
            {
                case 0: //L1与L2都平行Y轴
                    {
                        if (coordinate1[0].X == coordinate1[0].X)
                        {
                            //throw new Exception("两条直线互相重合，且平行于Y轴，无法计算交点。");
                            return new XYZ(0, 0, 0);
                        }
                        else
                        {
                            //throw new Exception("两条直线互相平行，且平行于Y轴，无法计算交点。");
                            return new XYZ(0, 0, coordinate1[0].Z);
                        }
                    }
                case 1: //L1存在斜率, L2平行Y轴
                    {
                        double x = coordinate2[0].X;
                        double y = (coordinate1[0].X - x) * (-a) + coordinate1[0].Y;
                        return new XYZ(x, y, coordinate1[0].Z);
                    }
                case 2: //L1 平行Y轴，L2存在斜率
                    {
                        double x = coordinate1[0].X;
                        //网上有相似代码的，这一处是错误的。你可以对比case 1 的逻辑 进行分析
                        //源code:lineSecondStar * x + lineSecondStar * lineSecondStar.X + p3.Y;
                        double y = (coordinate2[0].X - x) * (-b) + coordinate2[0].Y;
                        return new XYZ(x, y, coordinate1[0].Z);
                    }
                case 3: //L1，L2都存在斜率
                    {
                        if (a == b)
                        {
                            // throw new Exception("两条直线平行或重合，无法计算交点。");
                            return new XYZ(0, 0, coordinate1[0].Z);
                        }
                        double x = (a * coordinate1[0].X - b * coordinate2[0].X - coordinate1[0].Y + coordinate2[0].Y) / (a - b);
                        double y = a * x - a * coordinate1[0].X + coordinate1[0].Y;
                        return new XYZ(x, y, coordinate1[0].Z);
                    }
            }
            // throw new Exception("不可能发生的情况");
            return new XYZ(0, 0, coordinate1[0].Z);
        }

    */