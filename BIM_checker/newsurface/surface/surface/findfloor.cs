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
    class findfloor
    {
        //判断墙是否影响楼板的形状
        public static void RemoveIrrelevantWalls(Dictionary<int, List<Element>> colinear_floorids_Swalls, Dictionary<Element, List<Element>> colinear, Dictionary<Element, List<Element>> colinear_end, Dictionary<Element, Element> colinear_se)
        {
            List<Element> swall_insection_floors = new List<Element>();
            foreach (int i in colinear_floorids_Swalls.Keys)
            {
                swall_insection_floors.AddRange(colinear_floorids_Swalls[i]);
            }
            List<Element> wall_not_insection = new List<Element>();

            foreach (Element swall_co in colinear_se.Keys)
            {
                if (!swall_insection_floors.Contains(swall_co))
                {
                    wall_not_insection.Add(swall_co);
                }
            }
            foreach (Element i in wall_not_insection)
            {
                colinear_se.Remove(i);
                try
                {
                    colinear.Remove(i);
                }
                catch
                {
                    Element index = null;
                    foreach (Element ele in colinear.Keys)
                    {
                        if (colinear[ele].Contains(i))  //如果element id相同但是是重新生成的话就不行
                        {
                            index = ele;
                        }
                    }
                    if (!(index == null))
                    {
                        colinear.Remove(index);
                    }
                }
                try
                {
                    colinear_end.Remove(i);
                }
                catch
                {
                    Element index = null;
                    foreach (Element ele in colinear_end.Keys)
                    {
                        if (colinear_end[ele].Contains(i))  //如果element id相同但是是重新生成的话就不行
                        {
                            index = ele;
                        }
                    }
                    if (!(index == null))
                    {
                        colinear_end.Remove(index);
                    }
                }
            }
        }
        public static void FindRelatedFloors(Document doc, Dictionary<Element, Element> start_end, Dictionary<int, List<List<XYZ>>> floors_polygons, Dictionary<int, List<Element>> floorids_Swalls)
        {
            foreach (Element Swall_element in start_end.Keys)
            {
                
                Wall Swall = Swall_element as Wall;
                line Sline = general.GetLineFromWall(Swall);
                List<Element> floors_intersect_with_swall = findfloor.find_floors_near_surface(doc, Swall);  //不一定所以与墙相交的楼板都需要edit，有可能是内墙
                Options option = new Options();
                foreach (Element floor in floors_intersect_with_swall)
                {
                    List<List<XYZ>> polygons = flooredit.GetFloorBoundaryPolygons(floor, option);
                    bool wallspvertice_in_floorvertice = false; //Swall的起点和终点都在polygon中说明这一段墙在polygon中
                    bool wallepvertice_in_floorvertice = false;
                    foreach (List<XYZ> polygon in polygons)
                    {
                        foreach (XYZ vertice in polygon)
                        {
                            if (general.IsXYClose(vertice, Sline.sp, Swall.Width))
                            {
                                wallspvertice_in_floorvertice = true;
                            }
                            if (general.IsXYClose(vertice, Sline.ep, Swall.Width))
                            {
                                wallepvertice_in_floorvertice = true;
                            }
                        }
                    }
                    if (wallspvertice_in_floorvertice & wallepvertice_in_floorvertice)
                    {
                        if (!floors_polygons.Keys.Contains(floor.Id.IntegerValue))
                        {
                            List<Element> walls_relatedfloor = new List<Element>();
                            walls_relatedfloor.Add(Swall_element);
                            floors_polygons.Add(floor.Id.IntegerValue, polygons);
                            floorids_Swalls.Add(floor.Id.IntegerValue, walls_relatedfloor);
                        }
                        else
                        {
                            floorids_Swalls[floor.Id.IntegerValue].Add(Swall_element);
                        }
                    }
                }
            }
            //若楼板和好几个标高的墙相交，则按照与楼板标高相同的墙进行修改
            foreach (int floorid in floorids_Swalls.Keys)
            {
                if (floorids_Swalls[floorid].Count > 1)
                {
                    Element floor_or_roof = doc.GetElement(new ElementId(floorid));
                    List<Element> walls_remove = new List<Element>();
                    List<int> levelids = new List<int>();
                    foreach (Element wall in floorids_Swalls[floorid])
                    {
                        Wall swall = wall as Wall;
                        int levelid = swall.LevelId.IntegerValue;
                        if (!levelids.Contains(levelid))
                        {
                            levelids.Add(levelid);
                        }
                    }
                    if (levelids.Count > 1)
                    {
                        foreach (Element wall in floorids_Swalls[floorid])
                        {
                            Wall swall = wall as Wall;
                            if (!(floor_or_roof.LevelId.IntegerValue == swall.LevelId.IntegerValue))
                            {
                                walls_remove.Add(wall);
                            }
                        }
                    }
                    foreach (Element el in walls_remove)
                    {
                        floorids_Swalls[floorid].Remove(el);
                    }
                }
            }

        }

        public static void FindCowallsRelatedFloors(Document doc, Dictionary<Element, Element> start_end, Dictionary<int, List<List<XYZ>>> floors_polygons, Dictionary<int, List<Element>> floorids_Swalls)
        {
            foreach (Element Swall_element in start_end.Keys)
            {
                Wall Swall = Swall_element as Wall;
                line Sline = general.GetLineFromWall(Swall);
                List<Element> floors_intersect_with_swall = findfloor.find_floors_near_surface(doc, Swall);  //不一定所以与墙相交的楼板都需要edit，有可能是内墙
                Options option = new Options();
                foreach (Element floor in floors_intersect_with_swall)
                {
                    List<List<XYZ>> polygons = flooredit.GetFloorBoundaryPolygons(floor, option);
                    bool wallspvertice_in_floorvertice = false; //Swall的起点和终点都在polygon中说明这一段墙在polygon中
                    bool wallepvertice_in_floorvertice = false;
                    foreach (List<XYZ> polygon in polygons)
                    {
                        foreach (XYZ vertice in polygon)
                        {
                            if (general.IsXYClose(vertice, Sline.sp, Swall.Width))
                            {
                                wallspvertice_in_floorvertice = true;
                            }
                            if (general.IsXYClose(vertice, Sline.ep, Swall.Width))
                            {
                                wallepvertice_in_floorvertice = true;
                            }
                        }
                    }
                    if (wallspvertice_in_floorvertice & wallepvertice_in_floorvertice)
                    {
                        if (!floors_polygons.Keys.Contains(floor.Id.IntegerValue))
                        {
                            List<Element> walls_relatedfloor = new List<Element>();
                            walls_relatedfloor.Add(Swall_element);
                            floors_polygons.Add(floor.Id.IntegerValue, polygons);
                            floorids_Swalls.Add(floor.Id.IntegerValue, walls_relatedfloor);
                        }
                        else
                        {
                            floorids_Swalls[floor.Id.IntegerValue].Add(Swall_element);
                        }
                    }
                }
            }
            //若楼板和好几个标高的墙相交，则按照与楼板标高相同的墙进行修改
            foreach (int floorid in floorids_Swalls.Keys)
            {
                if (floorids_Swalls[floorid].Count > 1)
                {
                    Element floor_or_roof = doc.GetElement(new ElementId(floorid));
                    List<Element> walls_remove = new List<Element>();
                    List<int> levelids = new List<int>();
                    foreach (Element wall in floorids_Swalls[floorid])
                    {
                        Wall swall = wall as Wall;
                        int levelid = swall.LevelId.IntegerValue;
                        if (!levelids.Contains(levelid))
                        {
                            levelids.Add(levelid);
                        }
                    }
                    if (levelids.Count > 1)
                    {
                        foreach (Element wall in floorids_Swalls[floorid])
                        {
                            Wall swall = wall as Wall;
                            if (!(floor_or_roof.LevelId.IntegerValue == swall.LevelId.IntegerValue))
                            {
                                walls_remove.Add(wall);
                            }
                        }
                    }
                    foreach (Element el in walls_remove)
                    {
                        floorids_Swalls[floorid].Remove(el);
                    }
                }
            }

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

        public static List<Element> find_floors_near_surface(Document doc, Wall wall)
        {
            //List<Floor> surfacefloors = new List<Floor>();
            List<Element> surface_floors_roofs = new List<Element>();
            //将column solid并向外延伸，找到与之相交的墙，写入到dictionary中
            Options option = new Options();
            option.ComputeReferences = true;
            option.DetailLevel = ViewDetailLevel.Fine;
            List<Solid> solids = new List<Solid>();
            if (wall.Name.Contains("幕墙") | wall.Name.Contains("curtain") | wall.Name.Contains("Curtain"))
            {
                if (wall.CurtainGrid != null)
                {
                    ICollection<ElementId> panels = wall.CurtainGrid.GetPanelIds();
                    foreach (ElementId panelid in panels)
                    {
                        Panel panel = doc.GetElement(panelid) as Panel;
                        GeometryElement panel_geo = panel.get_Geometry(option);
                        foreach (GeometryObject geo in panel_geo)
                        {
                            GeometryInstance geo_ins = geo as GeometryInstance;
                            GeometryElement instance = geo_ins.GetInstanceGeometry();
                            foreach (GeometryObject insgeo in instance)
                            {
                                solids.Add(insgeo as Solid);
                            }
                        }
                    }
                }
            }
            else
            {
                GeometryElement geomElement = wall.get_Geometry(option);
                foreach (GeometryObject geomObj in geomElement)
                {
                    solids.Add(geomObj as Solid);
                }
            }

            //solid完成
            foreach (Solid solid in solids)
            {
                if (solid != null)
                {
                    //首先找到与原本的solid相交的floor and roofs
                    FilteredElementCollector floors_1 = new FilteredElementCollector(doc).OfClass(typeof(Floor)); //注意：filter用过wherepasses之后filter里面的元素会变少
                    FilteredElementCollector baseroof_1 = new FilteredElementCollector(doc).OfClass(typeof(RoofBase));
                    FilteredElementCollector extrusionroof_1 = new FilteredElementCollector(doc).OfClass(typeof(ExtrusionRoof));
                    FilteredElementCollector FootPrintRoof_1 = new FilteredElementCollector(doc).OfClass(typeof(FootPrintRoof));
                    FilteredElementCollector roofs_floors_1 = floors_1.UnionWith(baseroof_1).UnionWith(extrusionroof_1).UnionWith(FootPrintRoof_1);
                    IList<Element> floors_near_surface = roofs_floors_1.WherePasses(new ElementIntersectsSolidFilter(solid)).ToElements();
                    //再找solid的每一个面延伸出去的floor，防止画图误差导致的找不到相近的楼板 to do：这样防止不了幕墙画图有误差的时候
                    var faces = GetFacesFromSolid(solid);
                    foreach (var face in faces)
                    {
                        var planarFace = face as PlanarFace;
                        if (planarFace.FaceNormal.IsAlmostEqualTo(new XYZ(0, 0, 1)) || planarFace.FaceNormal.IsAlmostEqualTo(new XYZ(0, 0, -1)))
                        {
                            //由face转变为拉伸所需要的截面profile
                            var profiles = planarFace.GetEdgesAsCurveLoops();
                            //向两边都生成新的拉伸体solid
                            var solid_new1 = GeometryCreationUtilities.CreateExtrusionGeometry(profiles, planarFace.FaceNormal, 0.5);
                            var solid_new2 = GeometryCreationUtilities.CreateExtrusionGeometry(profiles, -planarFace.FaceNormal, 0.5);
                            FilteredElementCollector floors_2 = new FilteredElementCollector(doc).OfClass(typeof(Floor));
                            FilteredElementCollector baseroof_2 = new FilteredElementCollector(doc).OfClass(typeof(RoofBase));
                            FilteredElementCollector extrusionroof_2 = new FilteredElementCollector(doc).OfClass(typeof(ExtrusionRoof));
                            FilteredElementCollector FootPrintRoof_2 = new FilteredElementCollector(doc).OfClass(typeof(FootPrintRoof));
                            FilteredElementCollector roofs_floors_2 = floors_2.UnionWith(baseroof_2).UnionWith(extrusionroof_2).UnionWith(FootPrintRoof_2);
                            FilteredElementCollector floors_3 = new FilteredElementCollector(doc).OfClass(typeof(Floor));
                            FilteredElementCollector baseroof_3 = new FilteredElementCollector(doc).OfClass(typeof(RoofBase));
                            FilteredElementCollector extrusionroof_3 = new FilteredElementCollector(doc).OfClass(typeof(ExtrusionRoof));
                            FilteredElementCollector FootPrintRoof_3 = new FilteredElementCollector(doc).OfClass(typeof(FootPrintRoof));
                            FilteredElementCollector roofs_floors_3 = floors_3.UnionWith(baseroof_3).UnionWith(extrusionroof_3).UnionWith(FootPrintRoof_3);
                            try
                            {
                                IList<Element> new_solid1 = roofs_floors_2.WherePasses(new ElementIntersectsSolidFilter(solid_new1)).ToElements(); // Apply intersection filter to find matches
                                IList<Element> new_solid2 = roofs_floors_3.WherePasses(new ElementIntersectsSolidFilter(solid_new2)).ToElements();
                                foreach (Element el in new_solid1)
                                {
                                    bool duplication = false;
                                    foreach (Element plane in floors_near_surface)
                                    {
                                        if (el.Id.IntegerValue == plane.Id.IntegerValue)
                                        {
                                            duplication = true;
                                        }
                                    }
                                    //if (!floors_near_surface.Contains(el))
                                    if (!duplication)
                                    {
                                        floors_near_surface.Add(el);
                                    }
                                }
                                foreach (Element el in new_solid2)
                                {
                                    bool duplication = false;
                                    foreach (Element plane in floors_near_surface)
                                    {
                                        if (el.Id.IntegerValue == plane.Id.IntegerValue)
                                        {
                                            duplication = true;
                                        }
                                    }
                                    //if (!floors_near_surface.Contains(el))
                                    if (!duplication)
                                    {
                                        floors_near_surface.Add(el);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show("solid intersrction error", ex.Message);
                            }
                        }
                    }

                    if (floors_near_surface.Count() > 0)
                    {
                        foreach (Element el in floors_near_surface)
                        {
                            //Floor surfacefloor = el as Floor;
                            bool duplication = false;
                            foreach (Element plane in surface_floors_roofs)
                            {
                                if (el.Id.IntegerValue == plane.Id.IntegerValue)
                                {
                                    duplication = true;
                                }
                            }
                            //if (!surface_floors_roofs.Contains(el))
                            if (!duplication)
                            {
                                surface_floors_roofs.Add(el);
                            }
                        }
                    }
                    else
                    {
                        //TaskDialog.Show("NEED DEBUG",wall.Id.ToString()+": 找不到与该墙相交的楼板或屋顶");
                    }
                }
                else
                {
                    TaskDialog.Show("Solid bug", wall.Id.ToString() + ":该墙solid失败");
                }
            }
            return surface_floors_roofs;
        }
    }
}
