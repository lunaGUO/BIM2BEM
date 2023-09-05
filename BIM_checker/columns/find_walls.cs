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
    class find_walls
    {
        static public List<ElementId>  FindColumns(Document doc)
        {
            List<ElementId> co = new List<ElementId>();
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            FilteredElementCollector collector_s = new FilteredElementCollector(doc);
            //把柱子筛选出来，柱子可能有建筑柱，也可能有结构柱
            ElementCategoryFilter Fcolumns = new ElementCategoryFilter(BuiltInCategory.OST_Columns);
            ElementCategoryFilter FScolumns = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);
            ICollection<ElementId> columns = collector.WherePasses(Fcolumns).ToElementIds();
            ICollection<ElementId> Scolumns = collector_s.WherePasses(FScolumns).ToElementIds();
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
            return co;
        }

        public static List<Face> GetFacesFromSolid(Solid solid)
        {
            var faces = new List<Face>();
            foreach (Face face in solid.Faces)
            {
                faces.Add(face);
            }
            return faces;
        }

        public static void find_walls_near_co(Document doc, List<ElementId> allco, Dictionary<ElementId, IList<Element>> Columns_walls, List<ElementId> deletes)
        {
            string prompt = "Those columns near walls:\n";
            foreach (ElementId el in allco)
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
                        if (solid.Volume != 0)
                        {
                            break;
                        }
                    }
                }
                if (solid != null)
                {
                    //solid完成，先找到与solid相交的墙
                    FilteredElementCollector Familycollector_solid = new FilteredElementCollector(doc);
                    ElementCategoryFilter wallfilter_solid = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
                    Familycollector_solid.WherePasses(wallfilter_solid);
                    IList<Element> near_walls_solid = Familycollector_solid.WherePasses(new ElementIntersectsSolidFilter(solid)).ToElements(); // Apply intersection filter to find matches
                    if (near_walls_solid.Count() > 0)
                    {
                        if (!prompt.Contains(el.ToString()))
                        {
                            prompt += el.ToString() + "\n";
                            Columns_walls.Add(el, near_walls_solid);
                        }
                        else
                        {
                            foreach (Element nearwall in near_walls_solid)
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


                //将solid向外延申
                if (solid != null)
                {
                    var faces = GetFacesFromSolid(solid);
                    if (faces.Count != 0)
                    {
                        bool IsAllPlanar = true;
                        foreach (var face in faces)
                        {
                            if (!(face.GetType().ToString().Contains("PlanarFace"))) //如果是柱形的柱子（Face为CylindricalFace）就不再进行下面的步骤
                            {
                                IsAllPlanar = false;
                            }

                        }
                        if (IsAllPlanar)
                        {
                            foreach (var face in faces)
                            {
                                var planarFace = face as PlanarFace;
                                try
                                {
                                    //忽略掉顶面和底面
                                    if (planarFace.FaceNormal.IsAlmostEqualTo(new XYZ(0, 0, 1)) || planarFace.FaceNormal.IsAlmostEqualTo(new XYZ(0, 0, -1)))
                                    {
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    TaskDialog.Show("extend solid debug", face.GetType().ToString() + "\n" + column.Id.ToString() +"\n"+ ex.Message);
                                }
                                //由face转变为拉伸所需要的截面profile
                                var profiles = planarFace.GetEdgesAsCurveLoops();
                                //生成拉伸体solid
                                try
                                {
                                    var solid_new = GeometryCreationUtilities.CreateExtrusionGeometry(profiles, planarFace.FaceNormal, 0.5);
                                    FilteredElementCollector Familycollector = new FilteredElementCollector(doc);
                                    ElementCategoryFilter wallfilter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
                                    Familycollector.WherePasses(wallfilter);
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
                                    //TaskDialog.Show("ERROR:extend solid", "extend solid error:\n" + el.ToString() + "\n" + ex.Message) ;
                                //有的表面vertex距离太小，可能延申不了
                                }
                                /*
                                using (var ts = new Transaction(doc, "solid_new 可视化"))
                                {
                                    ts.Start();
                                    var ds = DirectShape.CreateElement(doc, new ElementId(-2000151));
                                    ds.AppendShape(new List<GeometryObject> { solid_new });
                                    ts.Commit();
                                }
                                */
                                
                                
                            }


                        }
                    }
                }
                if (!near_wall)
                {
                    if (!deletes.Contains(el))
                    {
                        deletes.Add(el);
                    }
                }
            }
            if (prompt != "Those columns near walls:\n")
            {
                TaskDialog.Show("columns near wall", prompt);
            }
        }

        public static Dictionary<ElementId, List<List<Element>>> FindSameLevelWall(Document doc, Dictionary<ElementId, IList<Element>> Columns_walls)
        {
            Dictionary<ElementId, List<List<Element>>> Columns_listwalls = new Dictionary<ElementId, List<List<Element>>>();
            //将column中不同标高的wall放置于不同的list中
            foreach (ElementId columnid in Columns_walls.Keys)
            {
                //FamilyInstance col = doc.GetElement(columnid) as FamilyInstance;
                //Level levelco = doc.GetElement(col.LevelId) as Level;
                List<Element> added = new List<Element>();
                for (int i = 0; i < (Columns_walls[columnid].Count); i++)
                {
                    if (!(added.Contains(Columns_walls[columnid][i])))
                    {
                        List<Element> listwall = new List<Element>();
                        listwall.Add(Columns_walls[columnid][i]);
                        if (Columns_listwalls.Keys.Contains(columnid))
                        {
                            Columns_listwalls[columnid].Add(listwall);
                        }
                        else
                        {
                            List<List<Element>> list_listwall = new List<List<Element>>();
                            list_listwall.Add(listwall);
                            Columns_listwalls.Add(columnid, list_listwall);
                        }
                        added.Add(Columns_walls[columnid][i]);
                        Level level1 = doc.GetElement(Columns_walls[columnid][i].LevelId) as Level;
                        for (int j = i + 1; j < (Columns_walls[columnid].Count); j++)
                        {
                            if (!added.Contains(Columns_walls[columnid][j]))
                            {
                                Level level2 = doc.GetElement(Columns_walls[columnid][j].LevelId) as Level;
                                try
                                {
                                    if (level2.Elevation - 0.5 <= level1.Elevation & level1.Elevation <= level2.Elevation + 0.5)
                                    {
                                        Columns_listwalls[columnid][Columns_listwalls[columnid].Count - 1].Add(Columns_walls[columnid][j]);
                                        added.Add(Columns_walls[columnid][j]);
                                    }
                                }
                                catch(Exception ex)
                                {
                                    TaskDialog.Show("debug",ex.Message);
                                }
                            }
                        }
                    }
                }
            }
            Dictionary<ElementId, List<List<Element>>> Columns_listwalls_edit  = DifLevelWallsRecognition(doc, Columns_listwalls); ;

            return Columns_listwalls_edit;
        }

        public static Dictionary<ElementId, List<List<Element>>> DifLevelWallsRecognition(Document doc, Dictionary<ElementId, List<List<Element>>> Columns_listwalls)
        {
            //Dictionary<ElementId, List<List<Element>>> Columns_listwalls_edit = new Dictionary<ElementId, List<List<Element>>>();
            //判断和柱子不在同一个标高的墙是否实际上很大部分与柱子相交
            foreach (ElementId columnid in Columns_listwalls.Keys)
            {
                if (Columns_listwalls[columnid].Count > 1)
                {
                    Element col = doc.GetElement(columnid);
                    Level levelco = doc.GetElement(col.LevelId) as Level;
                    Solid solid = GetSolid(col);

                    if (solid != null)
                    {
                        FilteredElementCollector collector = new FilteredElementCollector(doc);
                        ElementCategoryFilter Fcolumns = new ElementCategoryFilter(BuiltInCategory.OST_Columns);
                        ElementCategoryFilter FScolumns = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);
                        List<ElementFilter> filterSet = new List<ElementFilter>();
                        filterSet.Add(Fcolumns);
                        filterSet.Add(FScolumns);
                        LogicalOrFilter columns = new LogicalOrFilter(filterSet);
                        collector.WherePasses(columns);
                        IList<Element> crossed_columns = collector.WherePasses(new ElementIntersectsSolidFilter(solid)).ToElements();
                        Solid solid_new = solid;
                        var faces = GetFacesFromSolid(solid);
                        if (faces.Count != 0)
                        {
                            foreach (var face in faces)
                            {
                                if (face.GetType().ToString().Contains("PlanarFace"))
                                {
                                    PlanarFace planarFace = face as PlanarFace;
                                    try
                                    {
                                        //只延申顶面和底面
                                        if (planarFace.FaceNormal.IsAlmostEqualTo(new XYZ(0, 0, 1)) || planarFace.FaceNormal.IsAlmostEqualTo(new XYZ(0, 0, -1)))
                                        {
                                            var profiles = planarFace.GetEdgesAsCurveLoops();
                                            //生成拉伸体solid
                                            solid_new = GeometryCreationUtilities.CreateExtrusionGeometry(profiles, planarFace.FaceNormal, 1);
                                        }
                                    }
                                    catch
                                    {
                                        TaskDialog.Show("debug", face.GetType().ToString() + "\n" + columnid.ToString());
                                    }
                                    FilteredElementCollector collector2 = new FilteredElementCollector(doc);
                                    ElementCategoryFilter Fcolumns2 = new ElementCategoryFilter(BuiltInCategory.OST_Columns);
                                    ElementCategoryFilter FScolumns2 = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);
                                    List<ElementFilter> filterSet2 = new List<ElementFilter>();
                                    filterSet2.Add(Fcolumns);
                                    filterSet2.Add(FScolumns);
                                    LogicalOrFilter columns2 = new LogicalOrFilter(filterSet);
                                    collector2.WherePasses(columns2);
                                    foreach (Element el in collector2.WherePasses(new ElementIntersectsSolidFilter(solid_new)).ToElements())
                                    {
                                        if (!crossed_columns.Contains(el))
                                        {
                                            crossed_columns.Add(el);
                                        }
                                    }
                                }
                            }
                        }
                        /*
                        Level levelwall, level_other_co;
                        foreach (List<Element> walllist in Columns_listwalls[columnid])
                        {
                            if (walllist.Count >= 2)
                            {
                                levelwall = doc.GetElement(walllist[0].LevelId) as Level;
                                if (!(levelco.Elevation - 0.5 <= levelwall.Elevation & levelwall.Elevation <= levelco.Elevation + 0.5))
                                {
                                    foreach (Element co in crossed_columns)
                                    {
                                        level_other_co = doc.GetElement(co.LevelId) as Level;
                                        if (level_other_co.Elevation - 0.5 <= levelwall.Elevation & levelwall.Elevation <= level_other_co.Elevation + 0.5)
                                        {
                                            if (Columns_listwalls[columnid].Contains(walllist))
                                            {
                                                Columns_listwalls[columnid].Remove(walllist);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        */
                    }
                }
            }
            
            return Columns_listwalls;
        }

        static public Solid GetSolid(Element element)
        {
            Options option = new Options();
            option.ComputeReferences = true;
            option.DetailLevel = ViewDetailLevel.Fine;
            GeometryElement geomElement = element.get_Geometry(option);
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
            return solid;
        }

        //还有一种思路就是做一个solid的并集，计算并集的表面积和并集前的表面积是否相等
        


        //若list有相交的墙则去除其中较短的一个，得到新的需要处理的墙的list
        public static Dictionary<ElementId, List<List<Element>>> RemoveIntersectantWalls(Dictionary<ElementId, List<List<Element>>> Columns_listwalls)
        {
            Dictionary<ElementId, List<List<Element>>> Columns_listwalls_checked = new Dictionary<ElementId, List<List<Element>>>();
            foreach (ElementId columnid in Columns_listwalls.Keys)
            {
                foreach (List<Element> walllist in Columns_listwalls[columnid])
                {
                    
                }
            }
            return Columns_listwalls;
        }

        public static Dictionary<ElementId, List<List<Element>>> DeleteColumnsNearOneWall(Document doc, Dictionary<ElementId, List<List<Element>>> Columns_listwalls)
        {
            Dictionary<ElementId, List<List<Element>>> Columns_listwalls_simp = new Dictionary<ElementId, List<List<Element>>>();
            //把只有一个wall的List去除
            List<ElementId> columns_near_one_wall = new List<ElementId>();
            foreach (ElementId columnid in Columns_listwalls.Keys)
            {
                foreach (List<Element> walllist in Columns_listwalls[columnid])
                {
                    if (walllist.Count >= 2)
                    {
                        if (!Columns_listwalls_simp.Keys.Contains(columnid))
                        {
                            List<List<Element>> list_walllist = new List<List<Element>>();
                            list_walllist.Add(walllist);
                            Columns_listwalls_simp.Add(columnid, list_walllist);
                        }
                        else
                        {
                            Columns_listwalls_simp[columnid].Add(walllist);
                        }
                    }
                    else
                    {
                        if (!columns_near_one_wall.Contains(columnid))
                        {
                            columns_near_one_wall.Add(columnid);
                        }
                    }
                }
            }

            function.DeleteElements(doc, columns_near_one_wall);
            return Columns_listwalls_simp;
        }

    }
}
