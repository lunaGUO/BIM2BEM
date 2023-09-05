using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

//todo: 新建墙之后的返回内容可以更改为一个dictionary,key是columnid，value是new_walls的list:方便如果没有删掉全部的墙保留该柱子
//todo: 在查找column的最后一步可以加上一步，除去相交的墙

namespace columns
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]


    public class Co : IExternalCommand
    {
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document doc = revit.Application.ActiveUIDocument.Document;
            //把柱子筛选出来，柱子可能有建筑柱，也可能有结构柱
            List<ElementId> co = find_walls.FindColumns(doc);
            //找到与柱子相邻或相交的柱子
            Dictionary<ElementId, IList<Element>> Columns_walls = new Dictionary<ElementId, IList<Element>>();
            List<ElementId> deletes = new List<ElementId>();
            find_walls.find_walls_near_co(doc, co, Columns_walls, deletes);
            //将和墙不相邻的柱子删除
            function.DeleteElements(doc, deletes) ;
            //处理与墙相交的柱子 to do: 延申成功了才删除柱子，没有成功不删除
           //将在同一个标高上的墙移到一个dictionary中的一个list中
            Dictionary<ElementId, List<List<Element>>> Columns_listwalls = find_walls.FindSameLevelWall(doc,Columns_walls);
            //todo 判断一个list中的墙是否有相交的，得到除去相交之后的list 
            Dictionary<ElementId, List<List<Element>>> Columns_listwalls_checked = find_walls.RemoveIntersectantWalls(Columns_listwalls);
            //直接删除只有一个墙的list，如果整个柱子相交的墙list都只有一面墙则直接删除柱子
            Dictionary<ElementId, List<List<Element>>> Columns_listwalls_need_connect = find_walls.DeleteColumnsNearOneWall(doc, Columns_listwalls_checked);
            
            
            /*
           string test = "distinguish walls in different levels:\n ";
           foreach (ElementId coid in Columns_listwalls.Keys)
           {
               test += coid.ToString() + " column id\n";
               foreach (List<Element> ellist in Columns_listwalls[coid])
               {
                   test += "************\n";
                   foreach (Element el in ellist)
                   {
                       test += el.Id.ToString() + "\n";
                   }
               }
           }
           TaskDialog.Show("PROMPT",test);
           */

            //根据每个column相交的墙的情况对柱子进行处理

            dealwithcolumns.DealWithColumns(doc, Columns_listwalls_need_connect);
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