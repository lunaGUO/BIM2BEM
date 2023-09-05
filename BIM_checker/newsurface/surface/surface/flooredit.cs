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
    class flooredit
    {

        public static Dictionary<int,List<List<XYZ>>> EditPolygons_mian(Document doc,Dictionary<Element,Element> start_end,Dictionary<int,List<Element>> floorids_Swalls, Dictionary<int,List<List<XYZ>>> floors_polygons, Dictionary<int, List<List<XYZ>>> floors_newpolygons, Dictionary<Element, List<Element>> surface_edit)
        {
            foreach (int floorid in floorids_Swalls.Keys)
            {
                for (int i = 0; i < floorids_Swalls[floorid].Count; i++)
                //foreach (Element Swall_element in floorids_Swalls[floorid])
                {
                    Wall Swall = floorids_Swalls[floorid][i] as Wall;
                    List<List<XYZ>> edited_polygon = new List<List<XYZ>>();
                    List<List<XYZ>> before_edited_polygon = new List<List<XYZ>>();
                    if (!floors_newpolygons.Keys.Contains(floorid))
                    {
                        before_edited_polygon = floors_polygons[floorid];
                        edited_polygon = flooredit.edit_floor_polygon(floors_polygons[floorid], Swall, start_end[floorids_Swalls[floorid][i]] as Wall); ;
                    }
                    else
                    {
                        before_edited_polygon = floors_newpolygons[floorid];
                        edited_polygon = flooredit.edit_floor_polygon(floors_newpolygons[floorid], Swall, start_end[floorids_Swalls[floorid][i]] as Wall);
                    }
                    if (edited_polygon != before_edited_polygon)
                    {
                        //floors_polygons[floor] = flooredit.edit_floor_polygon(floors_polygons[floor], Swall, start_end[Swall_element] as Wall);
                        if (floors_newpolygons.Keys.Contains(floorid))
                        {
                            floors_newpolygons[floorid] = edited_polygon;
                            //floors_polygons[floorid] = edited_polygon;
                        }
                        else
                        {
                            floors_newpolygons.Add(floorid, edited_polygon);
                            //floors_polygons[floorid] = edited_polygon;
                        }
                    }

                    //如果返回的edited_polygon等于原来的polygon的话，说明没有找到endwall，需要找到surface_edit中全部相关的wall
                    else
                    {
                        Wall endwall = start_end[floorids_Swalls[floorid][i]] as Wall;
                        //TaskDialog.Show("test","没有找到endwall，开始找endwall周围的wall");
                        //查找与startwall最近的曲面内的墙
                        List<Element> relatedwalls = flooredit.FindRelatedWalls(floorids_Swalls[floorid][i], surface_edit);
                        Element nearest = flooredit.FindNearEndWall(floorids_Swalls[floorid][i], start_end[floorids_Swalls[floorid][i]], relatedwalls, floors_polygons[floorid]);
                        if (nearest.Id.IntegerValue != endwall.Id.IntegerValue)
                        {
                            if (floors_newpolygons.Keys.Contains(floorid))
                            {
                                edited_polygon = flooredit.edit_floor_polygon(floors_newpolygons[floorid], Swall, nearest as Wall);
                                floors_newpolygons[floorid] = edited_polygon;
                                //floors_polygons[floorid] = edited_polygon;

                            }
                            else
                            {
                                edited_polygon = flooredit.edit_floor_polygon(floors_polygons[floorid], Swall, nearest as Wall); ;
                                floors_newpolygons.Add(floorid, edited_polygon);
                            }
                        }
                        //else { TaskDialog.Show("prompt",Swall.Id.ToString()+"：相交楼板坐标中找不到与起点墙相关的墙坐标"); }

                        //找到与endwall最近的曲面内的墙及相关楼板
                        line endline = general.GetLineFromWall(endwall);
                        //找到与endwall相交的楼板
                        List<Element> endwall_intersect = new List<Element>();
                        endwall_intersect = findfloor.find_floors_near_surface(doc, endwall);
                        List<Element> endwall_floor = new List<Element>();
                        Options option = new Options();
                        foreach (Element floor in endwall_intersect)
                        {
                            List<List<XYZ>> polygons = flooredit.GetFloorBoundaryPolygons(floor, option);
                            bool wallspvertice_in_floorvertice = false; //endwall的起点和终点都在polygon中说明这一段墙在polygon中
                            bool wallepvertice_in_floorvertice = false;
                            foreach (List<XYZ> polygon in polygons)
                            {
                                foreach (XYZ vertice in polygon)
                                {
                                    if (general.IsXYClose(vertice, endline.sp, endwall.Width))
                                    {
                                        wallspvertice_in_floorvertice = true;
                                    }
                                    if (general.IsXYClose(vertice, endline.ep, endwall.Width))
                                    {
                                        wallepvertice_in_floorvertice = true;
                                    }

                                }
                            }
                            if (wallspvertice_in_floorvertice & wallepvertice_in_floorvertice)
                            {
                                if (!endwall_floor.Contains(floor))
                                {
                                    endwall_floor.Add(floor);
                                }
                            }
                        }
                        List<int> endfloor = new List<int>();
                        foreach (Element floor in endwall_floor)
                        {
                            Element floor1 = floor;
                            Element floor2 = doc.GetElement(new ElementId(floorid));
                            if (floor1.LevelId == floor2.LevelId & floorids_Swalls.Keys.Contains(floor.Id.IntegerValue))
                            {
                                if (!endfloor.Contains(floor.Id.IntegerValue))
                                {
                                    endfloor.Add(floor.Id.IntegerValue);
                                }
                            }
                        }
                        // need improve
                        foreach (int endfloorid in endfloor)
                        {
                            Element nearest_start = flooredit.FindNearEndWall(start_end[floorids_Swalls[floorid][i]], floorids_Swalls[floorid][i], relatedwalls, floors_polygons[endfloorid]);
                            if (nearest_start.Id.IntegerValue != Swall.Id.IntegerValue)
                            {
                                if (floors_newpolygons.Keys.Contains(floorid))
                                {
                                    edited_polygon = flooredit.edit_floor_polygon(floors_newpolygons[floorid], endwall, nearest_start as Wall);
                                    floors_newpolygons[floorid] = edited_polygon;
                                    //floors_polygons[floorid] = edited_polygon;
                                }
                                else
                                {
                                    edited_polygon = flooredit.edit_floor_polygon(floors_polygons[floorid], endwall, nearest_start as Wall); ;
                                    floors_newpolygons.Add(floorid, edited_polygon);
                                }

                            }
                            //else { TaskDialog.Show("prompt", start_end[Swall].Id.ToString() + "：相交楼板坐标中找不到与终点墙相关的墙坐标"); }
                        }
                    }

                }
            }
            return floors_newpolygons;
        }

        public static bool IsSEinSameFloor(int floorid, Dictionary<int, List<List<XYZ>>> floors_polygons, Element swall, Element ewall)
        {
            Options option = new Options();
            Wall Swall = swall as Wall;
            Wall Ewall = ewall as Wall;
            line Sline = general.GetLineFromWall(Swall);
            line Eline = general.GetLineFromWall(Ewall);
            List<List<XYZ>> polygons = floors_polygons[floorid];
            bool Swallspvertice_in_floorvertice = false; //Swall的起点和终点都在polygon中说明这一段墙在polygon中
            bool Swallepvertice_in_floorvertice = false;
            foreach (List<XYZ> polygon in polygons)
            {
                foreach (XYZ vertice in polygon)
                {
                    if (general.IsXYClose(vertice, Sline.sp, Swall.Width))
                    {
                        Swallspvertice_in_floorvertice = true;
                    }
                    if (general.IsXYClose(vertice, Sline.ep, Swall.Width))
                    {
                        Swallepvertice_in_floorvertice = true;
                    }
                }
            }
            bool Ewallspvertice_in_floorvertice = false; //Swall的起点和终点都在polygon中说明这一段墙在polygon中
            bool Ewallepvertice_in_floorvertice = false;
            foreach (List<XYZ> polygon in polygons)
            {
                foreach (XYZ vertice in polygon)
                {
                    if (general.IsXYClose(vertice, Eline.sp, Ewall.Width))
                    {
                        Ewallspvertice_in_floorvertice = true;
                    }
                    if (general.IsXYClose(vertice, Eline.ep, Ewall.Width))
                    {
                        Ewallepvertice_in_floorvertice = true;
                    }
                }
            }
            if (Swall.Id.IntegerValue == 2556530)
            {
                if (floorid == 2564017)
                {
                    TaskDialog.Show("debug", Swallspvertice_in_floorvertice.ToString() + "\n" + Swallepvertice_in_floorvertice + "\n" + Ewallspvertice_in_floorvertice.ToString() + "\n" + Ewallepvertice_in_floorvertice.ToString());
                    TaskDialog.Show("vertice", Eline.sp.ToString());
                }
            }    
            bool InSameFloor = false;
            if (Swallepvertice_in_floorvertice & Swallspvertice_in_floorvertice & Ewallepvertice_in_floorvertice & Ewallspvertice_in_floorvertice)
            {
                InSameFloor = true;
            }
            return InSameFloor;
        }


        public static Dictionary<int, List<List<XYZ>>> EditColinearPolygons_mian(Document doc, Dictionary<Element, Element> start_end, Dictionary<int, List<Element>> floorids_Swalls, Dictionary<int, List<List<XYZ>>> floors_polygons, Dictionary<int, List<List<XYZ>>> floors_newpolygons, Dictionary<Element, List<Element>> colinear,Dictionary<int,List<List<XYZ>>> surface_newpolygons)
        {
            //将曲面相关的楼板修改后的多边形顶点list与原有的list进行融合
            Dictionary<int, List<List<XYZ>>> mixed_floors_polygons = new Dictionary<int, List<List<XYZ>>>();
            foreach (int colinear_floorids in floors_polygons.Keys)
            {
                bool IsinNewpolygons = false;
                foreach (int surface_floorids in surface_newpolygons.Keys)
                {
                    if (colinear_floorids == surface_floorids)
                    {
                        IsinNewpolygons = true;
                    }
                }
                if (IsinNewpolygons)
                {
                    mixed_floors_polygons.Add(colinear_floorids, surface_newpolygons[colinear_floorids]);
                }
                else
                {
                    mixed_floors_polygons.Add(colinear_floorids, floors_polygons[colinear_floorids]);
                }
            }

            Dictionary<int, List<Element>> walls_insamefloor = new Dictionary<int, List<Element>>();
            Dictionary<int, List<Element>> walls_notinsamefloor = new Dictionary<int, List<Element>>();
            //判断是否起始和终点直线墙均在同一个楼板中
            foreach (int floorid in floorids_Swalls.Keys)
            {
                foreach (Element swall in floorids_Swalls[floorid])
                {
                    if(IsSEinSameFloor(floorid, mixed_floors_polygons, swall, start_end[swall]))
                    {
                        if (walls_insamefloor.Keys.Contains(floorid))
                        {
                            walls_insamefloor[floorid].Add(swall);
                        }
                        else
                        {
                            List<Element> swalls_insamefloor = new List<Element>();
                            swalls_insamefloor.Add(swall);
                            walls_insamefloor.Add(floorid,swalls_insamefloor);
                        }
                    }
                    else
                    {
                        if (walls_notinsamefloor.Keys.Contains(floorid))
                        {
                            walls_notinsamefloor[floorid].Add(swall);
                        }
                        else
                        {
                            List<Element> swalls_notinsamefloor = new List<Element>();
                            swalls_notinsamefloor.Add(swall);
                            walls_notinsamefloor.Add(floorid, swalls_notinsamefloor);
                        }
                    }
                }
            }

            string outputsame = "floors which all colinear walls are in:\n";
            foreach (int floorid in walls_insamefloor.Keys)
            {
                outputsame += "floor:" + floorid.ToString()+"\n";
                foreach (Element e in walls_insamefloor[floorid])
                {
                    outputsame += e.Id.ToString() + "\n";
                }
            }
            TaskDialog.Show("debug floor", outputsame);
            string outputdif = "floors which not all colinear walls are in:\n";
            foreach (int floorid in walls_notinsamefloor.Keys)
            {
                outputdif += "floor:" + floorid.ToString() + "\n";
                foreach (Element e in walls_notinsamefloor[floorid])
                {
                    outputdif += e.Id.ToString() + "\n";
                }
            }
            TaskDialog.Show("debug floor", outputdif);
            //先修改直线墙完全在同一个楼板中的楼板多边形
            foreach (int floorid in floorids_Swalls.Keys)
            {
                
            }
            /*
                foreach (int floorid in floorids_Swalls.Keys)
            {
                for (int i = 0; i < floorids_Swalls[floorid].Count; i++)
                //foreach (Element Swall_element in floorids_Swalls[floorid])
                {
                    Wall Swall = floorids_Swalls[floorid][i] as Wall;
                    List<List<XYZ>> edited_polygon = new List<List<XYZ>>();
                    List<List<XYZ>> before_edited_polygon = new List<List<XYZ>>();
                    if (!floors_newpolygons.Keys.Contains(floorid))
                    {
                        before_edited_polygon = mixed_floors_polygons[floorid];
                        edited_polygon = flooredit.EditColinearFloorPolygon(mixed_floors_polygons[floorid], Swall, start_end[floorids_Swalls[floorid][i]] as Wall); ;
                    }
                    else
                    {
                        before_edited_polygon = floors_newpolygons[floorid];
                        edited_polygon = flooredit.EditColinearFloorPolygon(floors_newpolygons[floorid], Swall, start_end[floorids_Swalls[floorid][i]] as Wall);
                    }
                    if (edited_polygon != before_edited_polygon)
                    {
                        //mixed_floors_polygons[floor] = flooredit.EditColinearFloorPolygon(mixed_floors_polygons[floor], Swall, start_end[Swall_element] as Wall);
                        if (floors_newpolygons.Keys.Contains(floorid))
                        {
                            floors_newpolygons[floorid] = edited_polygon;
                            //mixed_floors_polygons[floorid] = edited_polygon;
                        }
                        else
                        {
                            floors_newpolygons.Add(floorid, edited_polygon);
                            //mixed_floors_polygons[floorid] = edited_polygon;
                        }
                    }

                    //如果返回的edited_polygon等于原来的polygon的话，说明没有找到endwall，需要找到surface_edit中全部相关的wall
                    else
                    {
                        Wall endwall = start_end[floorids_Swalls[floorid][i]] as Wall;
                        //TaskDialog.Show("test","没有找到endwall，开始找endwall周围的wall");
                        //查找与startwall最近的曲面内的墙
                        List<Element> relatedwalls = flooredit.FindRelatedWalls(floorids_Swalls[floorid][i], colinear);
                        Element nearest = flooredit.FindNearEndWall(floorids_Swalls[floorid][i], start_end[floorids_Swalls[floorid][i]], relatedwalls, mixed_floors_polygons[floorid]);
                        if (nearest.Id.IntegerValue != endwall.Id.IntegerValue)
                        {
                            if (floors_newpolygons.Keys.Contains(floorid))
                            {
                                edited_polygon = flooredit.EditColinearFloorPolygon(floors_newpolygons[floorid], Swall, nearest as Wall);
                                floors_newpolygons[floorid] = edited_polygon;
                                //mixed_floors_polygons[floorid] = edited_polygon;

                            }
                            else
                            {
                                edited_polygon = flooredit.EditColinearFloorPolygon(mixed_floors_polygons[floorid], Swall, nearest as Wall); ;
                                floors_newpolygons.Add(floorid, edited_polygon);
                            }
                        }
                        //else { TaskDialog.Show("prompt",Swall.Id.ToString()+"：相交楼板坐标中找不到与起点墙相关的墙坐标"); }

                        //找到与endwall最近的曲面内的墙及相关楼板
                        line endline = general.GetLineFromWall(endwall);
                        //找到与endwall相交的楼板
                        List<Element> endwall_intersect = new List<Element>();
                        endwall_intersect = findfloor.find_floors_near_surface(doc, endwall);
                        List<Element> endwall_floor = new List<Element>();
                        Options option = new Options();
                        foreach (Element floor in endwall_intersect)
                        {
                            List<List<XYZ>> polygons = flooredit.GetFloorBoundaryPolygons(floor, option);
                            bool wallspvertice_in_floorvertice = false; //endwall的起点和终点都在polygon中说明这一段墙在polygon中
                            bool wallepvertice_in_floorvertice = false;
                            foreach (List<XYZ> polygon in polygons)
                            {
                                foreach (XYZ vertice in polygon)
                                {
                                    if (general.IsClose(vertice, endline.sp, endwall.Width))
                                    {
                                        wallspvertice_in_floorvertice = true;
                                    }
                                    if (general.IsClose(vertice, endline.ep, endwall.Width))
                                    {
                                        wallepvertice_in_floorvertice = true;
                                    }

                                }
                            }
                            if (wallspvertice_in_floorvertice & wallepvertice_in_floorvertice)
                            {
                                if (!endwall_floor.Contains(floor))
                                {
                                    endwall_floor.Add(floor);
                                }
                            }
                        }
                        List<int> endfloor = new List<int>();
                        foreach (Element floor in endwall_floor)
                        {
                            Element floor1 = floor;
                            Element floor2 = doc.GetElement(new ElementId(floorid));
                            if (floor1.LevelId == floor2.LevelId & floorids_Swalls.Keys.Contains(floor.Id.IntegerValue))
                            {
                                if (!endfloor.Contains(floor.Id.IntegerValue))
                                {
                                    endfloor.Add(floor.Id.IntegerValue);
                                }
                            }
                        }
                        // need improve
                        foreach (int endfloorid in endfloor)
                        {
                            Element nearest_start = flooredit.FindNearEndWall(start_end[floorids_Swalls[floorid][i]], floorids_Swalls[floorid][i], relatedwalls, floors_polygons[endfloorid]);
                            if (nearest_start.Id.IntegerValue != Swall.Id.IntegerValue)
                            {
                                if (floors_newpolygons.Keys.Contains(floorid))
                                {
                                    edited_polygon = flooredit.EditColinearFloorPolygon(floors_newpolygons[floorid], endwall, nearest_start as Wall);
                                    floors_newpolygons[floorid] = edited_polygon;
                                    //mixed_floors_polygons[floorid] = edited_polygon;
                                }
                                else
                                {
                                    edited_polygon = flooredit.EditColinearFloorPolygon(mixed_floors_polygons[floorid], endwall, nearest_start as Wall); ;
                                    floors_newpolygons.Add(floorid, edited_polygon);
                                }

                            }
                            //else { TaskDialog.Show("prompt", start_end[Swall].Id.ToString() + "：相交楼板坐标中找不到与终点墙相关的墙坐标"); }
                        }
                    }

                }
            
            }*/
            return floors_newpolygons;
        }


        /// <summary>
        /// 新创建的边界多边形曲线和楼板边缘之间的偏移
        /// </summary>
        //private const double _offset = 1;
        /// <summary>
        /// 获取指定实体的最下方水平表面的边界多边形
        /// </summary>
        static public List<List<XYZ>> GetFloorBoundaryPolygons(Element floor, Options opt)
        {
            List<List<XYZ>> polygons = new List<List<XYZ>>();
            //获取楼板的几何信息
            GeometryElement geo = floor.get_Geometry(opt);
            foreach (GeometryObject obj in geo)
            {
                Solid solid = obj as Solid;
                if (solid != null)
                {
                    GetBoundary(polygons, solid);
                }
                //break?
            }
        return polygons;
        }
        /// <summary>
        /// 计算最低水平面边界点坐标
        /// </summary>
        /// <param name="polygons">返回坐标点集合，包含边界与开孔</param>
        /// <param name="solid"></param>
        /// <returns>是否找到最低面</returns>
        static private bool GetBoundary(List<List<XYZ>> polygons, Solid solid)  
        {
            //最低面
            PlanarFace lowest = null;
            FaceArray faces = solid.Faces;
            foreach (Face f in faces)
            {
                PlanarFace pf = f as PlanarFace;
                if (null != pf && IsHorizontal(pf))
                {
                    if ((null == lowest) || (pf.Origin.Z < lowest.Origin.Z))
                    {
                        lowest = pf;
                    }
                }
            }
            if (null != lowest)
            {
                //XYZ p, q = XYZ.Zero;
                //bool first;
                int i, n;
                EdgeArrayArray loops = lowest.EdgeLoops;
                foreach (EdgeArray loop in loops)
                {
                    List<XYZ> vertices = new List<XYZ>();
                    //first = true;
                    foreach (Edge e in loop)
                    {
                        IList<XYZ> points = e.Tessellate();
                        //p = points[0];
                        n = points.Count;
                        //q = points[n - 1];
                        for (i = 0; i < n - 1; ++i) //最后一个点与下一个edge的起点重合不需要加进去
                        {
                            XYZ v = points[i];
                            //v -= _offset * XYZ.BasisZ;
                            vertices.Add(v);
                        }
                    }
                    //q -= _offset * XYZ.BasisZ;
                    polygons.Add(vertices);
                }
            }
            return null != lowest;
        }
        //是否是水平面
        static public bool IsHorizontal(PlanarFace f)
        {
            double eps = 1.0e-9;
            XYZ v = f.FaceNormal;
            return eps > Math.Abs(v.X) && eps > Math.Abs(v.Y);
        }

        //把polygon排列成startline的sp为第一个元素，start line的ep为第二个元素
        static public List<XYZ> SortPolygon(List<XYZ> floor_polygon,int position,line startline,Wall startwall)
        {
            //初始化list
            List<XYZ> sorted_polygon = new List<XYZ>();
            for (int i = 0; i < floor_polygon.Count; i++)
            {
                sorted_polygon.Add(floor_polygon[i]);
            }

            int end_position = position;
            for (int i = 0; i < floor_polygon.Count; i++)
            {
                if (general.IsXYClose(floor_polygon[i], startline.ep, startwall.Width))
                {
                    end_position = i;
                    break;
                }
            }
            if (end_position == position)
            {
                TaskDialog.Show("NEED DEBUG", "找不到polygon中起点墙的终点：" + startwall.Id.ToString());
                return floor_polygon;
            }
            else
            {
                
                if (position == 0) // 起点墙的起点是list的起点
                {
                    if (end_position == 1) //不需要调整多边形list
                    {
                        return floor_polygon;
                    }
                    else  //如果终点位置是n-1，修改polygon
                    {
                        sorted_polygon[0] = floor_polygon[0];
                        for (int i = 1; i < floor_polygon.Count; i++)
                        {
                            sorted_polygon[i] = floor_polygon[floor_polygon.Count - i];
                        }
                    }
                }
                else if (position == floor_polygon.Count - 1) // 起点墙的起点是list的终点
                {
                    if (end_position == 0)  //start line's end在start之后,不需要调整方向
                    {
                        sorted_polygon[0] = floor_polygon[position];
                        for (int i = 1; i < floor_polygon.Count; i++)
                        {
                            sorted_polygon[i] = floor_polygon[i - 1];
                        }
                    }
                    else //end position = n-1
                    {
                        for (int i = 0; i < floor_polygon.Count; i++)
                        {
                            sorted_polygon[i] = floor_polygon[floor_polygon.Count - 1 - i];
                        }
                    }
                }
                else // 起点墙的起点既不是list的起点也不是list的终点，一般情况
                {
                    if (end_position > position) //end在start之后
                    {
                        for (int i = 0; i < floor_polygon.Count; i++)
                        {
                            if (i < (floor_polygon.Count - position))
                            {
                                sorted_polygon[i] = floor_polygon[position + i];
                            }
                            else
                            {
                                sorted_polygon[i] = floor_polygon[position + i - floor_polygon.Count];
                            }
                        }
                    }
                    else //end在start之前
                    {
                        for (int i = 0; i < floor_polygon.Count; i++)
                        {
                            if (i <= position)
                            {
                                sorted_polygon[i] = floor_polygon[position - i];
                            }
                            else
                            {
                                sorted_polygon[i] = floor_polygon[floor_polygon.Count + position - i];
                            }
                        }
                    }
                }
            }
            return sorted_polygon;
        }

        //找两条线的交点
        static public XYZ FindIntersection(line line1 , line line2)
        {
            double D = line1.a * line2.b - line2.a * line1.b;
            XYZ cross = new XYZ();
            if (D != 0)
            {
                double cross_x = (line1.b * line2.c - line2.b * line1.c) / D;
                double cross_y = (line2.a * line1.c - line1.a * line2.c) / D;
                cross = new XYZ(cross_x, cross_y, line1.sp.Z);
            }
            else
            {
                cross = new XYZ(line1.sp.X, line1.sp.Y, line1.sp.Z);
            }
            return cross;
        }

        static public List<List<XYZ>> EditColinearFloorPolygon(List<List<XYZ>> floor_polygons, Wall startwall, Wall endwall)
        {
            line start_line = general.GetLineFromWall(startwall);
            line end_line = general.GetLineFromWall(endwall);
            List<List<XYZ>> floor_newpolygons = new List<List<XYZ>>();
            for (int i = 0; i < floor_polygons.Count; i++)  //caution!!! 直接赋值的数组只是浅复制，不是真复制！
            {
                //TaskDialog.Show("why","count:"+ floor_polygons.Count.ToString() +"\ni:"+i.ToString() + "\nfloor_golygons[i]:" + floor_polygons[i].Count.ToString());
                List<XYZ> copy = new List<XYZ>();
                for (int j = 0; j < floor_polygons[i].Count; j++)
                {
                    copy.Add(floor_polygons[i][j]);
                }
                floor_newpolygons.Add(copy);
            }
            //开始查找
            for (int i = 0; i < floor_polygons.Count; i++)
            {
                for (int j = 0; j < floor_polygons[i].Count; j++)
                {
                    if (floor_polygons[i].Count < 3)
                    {
                        TaskDialog.Show("DEBUG", startwall.Id.ToString() + ": the vertices of this floor's polygon are less than 3. It is" + floor_polygons[i].Count.ToString());
                    }
                    int vertice_forward, vertice_reverse;
                    if (j == 0)
                    {
                        vertice_forward = 1;
                        vertice_reverse = floor_polygons[i].Count - 1;
                    }
                    else if (j == floor_polygons[i].Count - 1)
                    {
                        vertice_forward = 0;
                        vertice_reverse = floor_polygons[i].Count - 2;
                    }
                    else
                    {
                        vertice_forward = j + 1;
                        vertice_reverse = j - 1;
                    }
                    if (general.IsXYClose(start_line.sp, floor_polygons[i][j], startwall.Width) & (general.IsXYClose(start_line.ep, floor_polygons[i][vertice_reverse], startwall.Width) | general.IsXYClose(start_line.ep, floor_polygons[i][vertice_forward], startwall.Width) )) //找到楼板中修改的曲面中的点,需要确保起点墙的起点和终点都在polygon里面
                    {
                        //将polygon重新排序
                        List<XYZ> sorted_polygon = SortPolygon(floor_polygons[i], j, start_line, startwall);
                        for (int a = 0; a < sorted_polygon.Count; a++)
                        {
                            floor_newpolygons[i][a] = sorted_polygon[a];
                        }

                        //找到end wall最先在修改后的list中出现的点
                        int endline_fp = 0;
                        for (int m = 0; m < sorted_polygon.Count; m++)
                        {
                            int endwall_vertice_forward;
                            if (m == 0)
                            {
                                endwall_vertice_forward = 1;
                            }
                            else if (m == sorted_polygon.Count - 1)
                            {
                                //TaskDialog.Show("NEED DEBUG", "找终点墙的位置时，找到的终点墙的第一个点是polygon list的最后一个点：" + startwall.Id.ToString() + "/" + endwall.Id.ToString());
                                return floor_polygons;
                            }
                            else
                            {
                                endwall_vertice_forward = m + 1;
                            }
                            //may need check
                            if ((general.IsXYClose(sorted_polygon[m], end_line.sp, endwall.Width) & general.IsXYClose(sorted_polygon[endwall_vertice_forward], end_line.ep, endwall.Width)) | (general.IsXYClose(sorted_polygon[m], end_line.ep, endwall.Width) & general.IsXYClose(sorted_polygon[endwall_vertice_forward], end_line.sp, endwall.Width)))
                            {
                                endline_fp = m;
                                break;
                            }
                        }
                        if (endline_fp == 0)
                        {
                            //TaskDialog.Show("NEED DEBUG", "找不到polygon中终点墙：" + startwall.Id.ToString()+"/"+endwall.Id.ToString());
                            /*
                            string newpolygons_debug = "报错 polygons vertices debug:\n";
                            foreach (List<XYZ> polygon in floor_polygons)
                            {
                                newpolygons_debug += "***************\n";
                                foreach (XYZ vertice in polygon)
                                {
                                    newpolygons_debug += vertice.ToString() + "\n";
                                }
                            }
                            TaskDialog.Show("polygon debug", newpolygons_debug);
                            */
                            return floor_polygons;
                        }
                        bool end_forward;
                        line start_vertices = general.GetLineFromPoint(sorted_polygon[0], sorted_polygon[1]);
                        //查找中间被删除的墙是在start点的上方还是下方:通过墙的距离和角度：改成采用顶点的距离判断
                        XYZ vector_start = sorted_polygon[1]- sorted_polygon[0];
                        XYZ vector_forward = sorted_polygon[2] - sorted_polygon[1];
                        XYZ vector_reverse = sorted_polygon[0] - sorted_polygon[sorted_polygon.Count - 1];
                        double dis_forwardtoend = general.DistanceOfTwoPoint(sorted_polygon[2], sorted_polygon[endline_fp]);
                        double dis_reversetoend = general.DistanceOfTwoPoint(sorted_polygon[sorted_polygon.Count - 1], sorted_polygon[endline_fp]);
                        double forward_length = general.DistanceOfTwoPoint(sorted_polygon[1], sorted_polygon[2]);
                        double reverse_length = general.DistanceOfTwoPoint(sorted_polygon[sorted_polygon.Count - 1], sorted_polygon[0]);
                        //TaskDialog.Show("DEBUG","Swall id:"+startwall.Id.ToString()+"\nforward vertice:"+ sorted_polygon[2] +"\nangle:"+angle_forward.ToString() + "\nforward length:" + forward_length.ToString().ToString());
                        if ((vector_start.IsAlmostEqualTo(vector_forward)) & (forward_length < 6.56))
                        {
                            if (!((vector_start.IsAlmostEqualTo(vector_reverse)) & (reverse_length < 6.56)))
                            {
                                end_forward = true;
                            }
                            else if (dis_forwardtoend < dis_reversetoend)
                            {
                                end_forward = true;
                            }
                            else
                            {
                                end_forward = false;
                            }
                        }
                        else
                        {
                            end_forward = false;
                            
                        }
                        //TaskDialog.Show("debug","end_forward:"+end_forward.ToString());
                        
                        if (end_forward)
                        {
                            //TaskDialog.Show("test", "end wall is forward\n" + cross.ToString());
                            for (int m = 1; m < endline_fp+1; m++)
                            {
                                floor_newpolygons[i].Remove(sorted_polygon[m]); //删除中间的点
                            }
                        }
                        else
                        {
                            //TaskDialog.Show("test", "end wall is not forward\n" + cross.ToString());
                            for (int m = endline_fp + 1; m < sorted_polygon.Count; m++) //fp+1是end line的后面一个点
                            {
                                floor_newpolygons[i].Remove(sorted_polygon[m]); //删除中间的点，相向相反，所以在后面
                            }
                            floor_newpolygons[i].Remove(sorted_polygon[0]);
                        }
                        break;
                    }
                    //}
                }
            }
            return floor_newpolygons;
        }

        static public List<List<XYZ>> edit_floor_polygon(List<List<XYZ>> floor_polygons, Wall startwall, Wall endwall)
        {
            line start_line = general.GetLineFromWall(startwall);
            line end_line = general.GetLineFromWall(endwall);
            List<List<XYZ>> floor_newpolygons = new List<List<XYZ>>();
            for (int i = 0; i < floor_polygons.Count; i++)  //caution!!! 直接赋值的数组只是浅复制，不是真复制！
            {
                //TaskDialog.Show("why","count:"+ floor_polygons.Count.ToString() +"\ni:"+i.ToString() + "\nfloor_golygons[i]:" + floor_polygons[i].Count.ToString());
                List<XYZ> copy = new List<XYZ>();
                for(int j = 0; j < floor_polygons[i].Count;j++)
                {
                    copy.Add(floor_polygons[i][j]);
                }
                floor_newpolygons.Add(copy);
            }
            //开始查找
            for (int i = 0; i < floor_polygons.Count; i++)
            {
                for (int j = 0; j < floor_polygons[i].Count; j++)
                {
                    int vertice_forward, vertice_reverse;
                    if (j == 0)
                    {
                        vertice_forward = 1;
                        vertice_reverse = floor_polygons[i].Count-1;
                    }
                    else if (j == floor_polygons[i].Count-1)
                    {
                        vertice_forward = 0;
                        vertice_reverse = floor_polygons[i].Count - 2;
                    }
                    else 
                    {
                        vertice_forward = j + 1;
                        vertice_reverse = j - 1;
                    }
                    if (general.IsXYClose(start_line.sp, floor_polygons[i][j],startwall.Width) &(general.IsXYClose(start_line.ep, floor_polygons[i][vertice_reverse], startwall.Width) | general.IsXYClose(start_line.ep, floor_polygons[i][vertice_forward], startwall.Width)) ) //找到楼板中修改的曲面中的点,需要确保起点墙的起点和终点都在polygon里面
                    {
                        //将polygon重新排序
                        List<XYZ> sorted_polygon = SortPolygon(floor_polygons[i], j, start_line, startwall);
                        for (int a = 0; a < sorted_polygon.Count; a++)
                        {
                            floor_newpolygons[i][a] = sorted_polygon[a];
                        }

                        //找到end wall最先在修改后的list中出现的点
                        int endline_fp = 0;
                        for (int m = 0; m < sorted_polygon.Count; m++)
                        {
                            int endwall_vertice_forward;
                            if (m == 0)
                            {
                                endwall_vertice_forward = 1;
                            }
                            else if (m == sorted_polygon.Count -1)
                            {
                                //TaskDialog.Show("NEED DEBUG", "找终点墙的位置时，找到的终点墙的第一个点是polygon list的最后一个点：" + startwall.Id.ToString() + "/" + endwall.Id.ToString());
                                return floor_polygons;
                            }
                            else
                            {
                                endwall_vertice_forward = m + 1;
                            }
                            if ((general.IsXYClose(sorted_polygon[m], end_line.sp, endwall.Width) & general.IsXYClose(sorted_polygon[endwall_vertice_forward], end_line.ep, endwall.Width)) | (general.IsXYClose(sorted_polygon[m], end_line.ep, endwall.Width) & general.IsXYClose(sorted_polygon[endwall_vertice_forward], end_line.sp, endwall.Width)))
                            {
                                endline_fp = m;
                                break;
                            }
                        }
                        if (endline_fp == 0)
                        {
                            //TaskDialog.Show("NEED DEBUG", "找不到polygon中终点墙：" + startwall.Id.ToString()+"/"+endwall.Id.ToString());
                            /*
                            string newpolygons_debug = "报错 polygons vertices debug:\n";
                            foreach (List<XYZ> polygon in floor_polygons)
                            {
                                newpolygons_debug += "***************\n";
                                foreach (XYZ vertice in polygon)
                                {
                                    newpolygons_debug += vertice.ToString() + "\n";
                                }
                            }
                            TaskDialog.Show("polygon debug", newpolygons_debug);
                            */
                            return floor_polygons;
                        }
                        bool end_forward;
                        line start_vertices = general.GetLineFromPoint(sorted_polygon[0],sorted_polygon[1]);
                        //查找中间被删除的墙是在start点的上方还是下方:通过墙的距离和角度：改成采用顶点的距离判断
                        line forward1 = general.GetLineFromPoint(sorted_polygon[1], sorted_polygon[2]);
                        line forward2 = general.GetLineFromPoint(sorted_polygon[2], sorted_polygon[3]);
                        line reverse1 = general.GetLineFromPoint(sorted_polygon[sorted_polygon.Count - 1], sorted_polygon[0]);
                        line reverse2 = general.GetLineFromPoint(sorted_polygon[sorted_polygon.Count - 2], sorted_polygon[sorted_polygon.Count - 1]);
                        double angle_forward = general.AngleCalThroughLine(forward1, forward2);
                        //TaskDialog.Show("test",angle_start_forward.ToString());
                        double angle_reverse= general.AngleCalThroughLine(reverse1, reverse2);
                        double dis_forwardtoend = general.DistanceOfTwoPoint(sorted_polygon[2], sorted_polygon[endline_fp]);
                        double dis_reversetoend = general.DistanceOfTwoPoint(sorted_polygon[sorted_polygon.Count - 1], sorted_polygon[endline_fp]);
                        //if (dis_forwardtoend < dis_reversetoend )
                        double forward_length = general.DistanceOfTwoPoint(sorted_polygon[1], sorted_polygon[2]);
                        double reverse_length = general.DistanceOfTwoPoint(sorted_polygon[sorted_polygon.Count - 1], sorted_polygon[0]);
                        //TaskDialog.Show("DEBUG","Swall id:"+startwall.Id.ToString()+"\nforward vertice:"+ sorted_polygon[2] +"\nangle:"+angle_forward.ToString() + "\nforward length:" + forward_length.ToString().ToString());
                        if (((1E-6 < angle_forward & angle_forward <= 20) | (160 <= angle_forward & angle_forward < (180- 1E-6))) & (forward_length < 6.56))
                        {
                            
                            if (!(((1E-6 < angle_reverse & angle_reverse <= 20) | (160 <= angle_reverse & angle_reverse < (180 - 1E-6))) & (reverse_length < 6.56)))
                            {
                                end_forward = true;
                            }
                            else if (dis_forwardtoend < dis_reversetoend)
                            {
                                end_forward = true;
                            }
                            else
                            {
                                end_forward = false;
                            }
                        }
                        else
                        {
                            //if (!(((0.1 < angle_reverse & angle_reverse <= 20) | (160 <= angle_reverse & angle_reverse < 179.9)) & (reverse_length < 6.56)))
                            //{
                                //TaskDialog.Show("debug","no surface found");
                                //return floor_polygons;
                            //}
                            //else
                            //{
                                end_forward = false;
                            //}
                        }
                        //TaskDialog.Show("debug","end_forward:"+end_forward.ToString());
                        //获取交点
                        line end_vertices = general.GetLineFromPoint(sorted_polygon[endline_fp], sorted_polygon[endline_fp+1]);
                        XYZ cross = flooredit.FindIntersection(start_vertices, end_vertices);
                        //TaskDialog.Show("cross debug",startwall.Id.ToString() +"\n"+cross.ToString()+"\n"+endwall.Id.ToString());
                        if (end_forward)
                        {
                            //TaskDialog.Show("test", "end wall is forward\n" + cross.ToString());
                            for (int m = 2; m < endline_fp; m++)
                            {
                                floor_newpolygons[i].Remove(sorted_polygon[m]); //删除中间的点
                            }
                            floor_newpolygons[i].Insert(2,cross); //插入交点
                        }
                        else 
                        {
                            //TaskDialog.Show("test", "end wall is not forward\n" + cross.ToString());
                            for (int m = endline_fp + 2; m < sorted_polygon.Count; m++) //fp+1是end line的后面一个点
                            {
                                floor_newpolygons[i].Remove(sorted_polygon[m]); //删除中间的点，相向相反，所以在后面
                            }
                            floor_newpolygons[i].Add(cross); //交点加在末尾
                        }
                        break;
                    }
                }
            }
            return floor_newpolygons;
        }

        static public List<Element> FindRelatedWalls(Element index_ele, Dictionary<Element,List<Element>> surface_edit)
        {
            List<Element> related_ele = new List<Element>();
            foreach (Element el in surface_edit.Keys)
            {
                bool find_swall = false;
                if (index_ele.Id.IntegerValue == el.Id.IntegerValue)
                {
                    find_swall = true;
                }
                else
                {
                    foreach (Element ele in surface_edit[el])
                    {
                        if (index_ele.Id.IntegerValue == ele.Id.IntegerValue)
                        {
                            find_swall = true;
                        }
                    }
                }
                if (find_swall)
                {
                    related_ele.Add(el);
                    related_ele.AddRange(surface_edit[el]);
                }
            }
            return related_ele;
        }

        static public List<Element> FindWallsinBetween(Element targetwall, Element indexwall, List<Element> nearwalls)
        {
            List<Element> walls_in_between = new List<Element>();
            Wall start_wall = targetwall as Wall;
            Wall end_wall = indexwall as Wall;
            double distance_se = general.GetDistanceFromWalls(start_wall,end_wall);
            foreach (Element ele in nearwalls)
            {
                Wall wall = ele as Wall;
                double distance_s = general.GetDistanceFromWalls(start_wall, wall);
                double distance_e = general.GetDistanceFromWalls(wall, end_wall);
                if (distance_e < distance_se & distance_s < distance_se)
                {
                    walls_in_between.Add(ele);
                }
            }
            return walls_in_between;
        }


        static public Element FindtheClosestWall( Element indexwall, List<Element> walls_in_between)//, List<List<XYZ>> polygons)
        {
            //查找与indexwall最近的在polygon中的wall
            Element closestwall = indexwall;
            double min_distance = 10000;
            foreach (Element ele in walls_in_between)
            {
                Wall wall1 = ele as Wall;
                Wall wall2 = indexwall as Wall;
                double distance = general.GetDistanceFromWalls(wall1, wall2);
                if (distance <= min_distance)
                {
                    min_distance = distance;
                    closestwall = ele;
                }
            }
            return closestwall;
        }

        static public bool IsWallinPolygon(List<List<XYZ>> polygons, Element ele)
        {
            Wall wall = ele as Wall;
            line wallline = general.GetLineFromWall(wall);
            bool IsWallinPolygon = false;
            foreach (List<XYZ> polygon in polygons)
            {
                bool IsSPointinPolygon = false;
                bool IsEPointinPolygon = false;
                foreach (XYZ vertice in polygon)
                {
                    if (general.IsXYClose(vertice, wallline.sp, wall.Width))
                    {
                        IsSPointinPolygon = true;
                    }
                    if (general.IsXYClose(vertice, wallline.ep, wall.Width))
                    {
                        IsEPointinPolygon = true;
                    }
                }
                if (IsEPointinPolygon & IsSPointinPolygon)
                {
                    IsWallinPolygon = true;
                    break;
                }
            }
            return IsWallinPolygon;
        }
        //target wall是在polygon中的墙，index wall指的是target wall对应的不在polygon中的墙
        static public Element FindNearEndWall(Element targetwall, Element indexwall, List<Element> relatedwalls, List<List<XYZ>> polygons)
        {
            List<Element> walls_in_between = FindWallsinBetween(targetwall, indexwall, relatedwalls);
            int count = walls_in_between.Count;
            Element nearest = FindtheClosestWall(indexwall, walls_in_between);
            bool is_wall_in_polygons = IsWallinPolygon(polygons, nearest);
            int i = 0;
            while (!is_wall_in_polygons)
            {
                walls_in_between.Remove(nearest);
                nearest = FindtheClosestWall(indexwall, walls_in_between);
                is_wall_in_polygons = IsWallinPolygon(polygons, nearest);
                i++;
                if (i >= count)
                {
                    break;
                }
            }
            if (!is_wall_in_polygons)
            {
                nearest = indexwall;
            }
            return nearest;
        }

        static public Floor CreateNewFloor(Document doc,List<List<XYZ>> polygons, FloorType floortype, Level floorlevel,double offset)
        {
            //找到最外层
            double max = -10000;
            int max_index = -1; 
            for (int i = 0; i < polygons.Count; i++)
            {
                for (int j = 0; j < polygons[i].Count; j++)
                {
                    if (polygons[i][j].X > max)
                    {
                        max = polygons[i][j].X;
                        max_index = i;
                    }
                }
            }
            
            CurveArray outer_profile = new CurveArray();
            List<CurveArray> inner_profiles = new List<CurveArray>();
            for (int i = 0; i < polygons.Count; i++)
            {
                if (i == max_index)
                {
                    for (int j = 0; j < polygons[i].Count - 1; j++)
                    {
                        outer_profile.Append(Line.CreateBound(polygons[i][j], polygons[i][j + 1]));
                    }
                    outer_profile.Append(Line.CreateBound(polygons[i][polygons[i].Count - 1], polygons[i][0]));
                }
                else
                {
                    CurveArray profile = new CurveArray();
                    for (int j = 0; j < polygons[i].Count - 1; j++)
                    {
                        profile.Append(Line.CreateBound(polygons[i][j], polygons[i][j + 1]));
                    }
                    profile.Append(Line.CreateBound(polygons[i][polygons[i].Count - 1], polygons[i][0]));
                    inner_profiles.Add(profile);
                }
            }
            Floor newfloor = null;
            
            try
            {
                //创建新的楼板
                using (Transaction tran = new Transaction(doc, "create new floor"))
                {
                    tran.Start();
                    //FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                    //dealwitherror.MyFailuresPreprocessor failureProcessor = new dealwitherror.MyFailuresPreprocessor();
                    //options.SetFailuresPreprocessor(failureProcessor);
                    //tran.SetFailureHandlingOptions(options);
                    newfloor = doc.Create.NewFloor(outer_profile, floortype,floorlevel,false);
                    tran.Commit();
                    //var status = tran.Commit();
                    //if (status != TransactionStatus.Committed)
                    //{
                        //if (failureProcessor.HasError)
                        //{
                            //TaskDialog.Show("ERROR", failureProcessor.FailureMessage);
                        //}
                    //}
                }
                //修改标高
                using (Transaction tran = new Transaction(doc, "edit new floor's parameter"))
                {
                    tran.Start();
                    //FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                    //dealwitherror.MyFailuresPreprocessor failureProcessor = new dealwitherror.MyFailuresPreprocessor();
                    //options.SetFailuresPreprocessor(failureProcessor);
                    //tran.SetFailureHandlingOptions(options);
                    newfloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(offset);
                    tran.Commit();
                    //var status = tran.Commit();
                    //if (status != TransactionStatus.Committed)
                    //{
                        //if (failureProcessor.HasError)
                        //{
                            //TaskDialog.Show("ERROR", failureProcessor.FailureMessage);
                        //}
                    //}
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Create new floor", floorlevel.Name + ":create new floor exception\n" + ex.Message);
            }
            //楼板开洞
            if (newfloor != null)
            {
                try
                {
                    foreach (CurveArray innerprofile in inner_profiles)
                {
                        using (Transaction tran = new Transaction(doc, "create opening in new floor"))
                        {
                            tran.Start();
                            doc.Create.NewOpening(newfloor, innerprofile, true);
                            tran.Commit();
                        }
                    }
                
                    
                    
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("create opening in new floor", floorlevel.Name + "\n" + ex.Message);
                }
            }
            return newfloor;
        }

    }


    class roofedit
    {
        static public FootPrintRoof CreateNewFootPrintRoof(Document doc, List<List<XYZ>> polygons, RoofType rooftype, Level floorlevel, double offset)
        {
            /*
            //找到最外层
            double max = -10000;
            int max_index = -1;
            for (int i = 0; i < polygons.Count; i++)
            {
                for (int j = 0; j < polygons[i].Count; j++)
                {
                    if (polygons[i][j].X > max)
                    {
                        max = polygons[i][j].X;
                        max_index = i;
                    }
                }
            }

            CurveArray outer_profile = new CurveArray();
            List<CurveArray> inner_profiles = new List<CurveArray>();
            for (int i = 0; i < polygons.Count; i++)
            {
                if (i == max_index)
                {
                    for (int j = 0; j < polygons[i].Count - 1; j++)
                    {
                        outer_profile.Append(Line.CreateBound(polygons[i][j], polygons[i][j + 1]));
                    }
                    outer_profile.Append(Line.CreateBound(polygons[i][polygons[i].Count - 1], polygons[i][0]));
                }
                else
                {
                    CurveArray profile = new CurveArray();
                    for (int j = 0; j < polygons[i].Count - 1; j++)
                    {
                        profile.Append(Line.CreateBound(polygons[i][j], polygons[i][j + 1]));
                    }
                    profile.Append(Line.CreateBound(polygons[i][polygons[i].Count - 1], polygons[i][0]));
                    inner_profiles.Add(profile);
                }
            }
            */
            CurveArray roofprofile = new CurveArray();
            for (int i = 0; i < polygons.Count; i++)
            {
                for (int j = 0; j < polygons[i].Count - 1; j++)
                {
                    roofprofile.Append(Line.CreateBound(polygons[i][j], polygons[i][j + 1]));
                }
                roofprofile.Append(Line.CreateBound(polygons[i][polygons[i].Count - 1], polygons[i][0]));
            }



            FootPrintRoof newfootroof = null;

            try
            {
                //创建新的楼板
                using (Transaction tran = new Transaction(doc, "create new roof"))
                {
                    tran.Start();
                    //FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                    //dealwitherror.MyFailuresPreprocessor failureProcessor = new dealwitherror.MyFailuresPreprocessor();
                    //options.SetFailuresPreprocessor(failureProcessor);
                    //tran.SetFailureHandlingOptions(options);
                    ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
                    newfootroof = doc.Create.NewFootPrintRoof(roofprofile, floorlevel, rooftype, out footPrintToModelCurveMapping);
                    //todo :设置屋顶坡度?
                    /*
                    ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
                    iterator.Reset();
                    while (iterator.MoveNext())
                    {
                        ModelCurve modelCurve = iterator.Current as ModelCurve;
                        newfootroof.set_DefinesSlope(modelCurve, true);
                        newfootroof.set_SlopeAngle(modelCurve, 0.5);
                    }
                    */
                    tran.Commit();
                    //var status = tran.Commit();
                    //if (status != TransactionStatus.Committed)
                    //{
                    //if (failureProcessor.HasError)
                    //{
                    //TaskDialog.Show("ERROR", failureProcessor.FailureMessage);
                    //}
                    //}
                }
                //修改标高
                using (Transaction tran = new Transaction(doc, "edit new roof's parameter"))
                {
                    tran.Start();
                    //FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                    //dealwitherror.MyFailuresPreprocessor failureProcessor = new dealwitherror.MyFailuresPreprocessor();
                    //options.SetFailuresPreprocessor(failureProcessor);
                    //tran.SetFailureHandlingOptions(options);
                    newfootroof.get_Parameter(BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).Set(offset);
                    tran.Commit();
                    //var status = tran.Commit();
                    //if (status != TransactionStatus.Committed)
                    //{
                    //if (failureProcessor.HasError)
                    //{
                    //TaskDialog.Show("ERROR", failureProcessor.FailureMessage);
                    //}
                    //}
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Create new roof", floorlevel.Name + " create roof exception\n" + ex.Message);
            }

            /*
            //楼板开洞
            if (newfootroof != null)
            {
                try
                {
                    foreach (CurveArray innerprofile in inner_profiles)
                    {
                        using (Transaction tran = new Transaction(doc, "create opening in new floor"))
                        {
                            tran.Start();
                            doc.Create.NewOpening(newfootroof, innerprofile, true);
                            tran.Commit();
                        }
                    }



                }
                catch (Exception ex)
                {
                    TaskDialog.Show("create opening in new floor", floorlevel.Name + "\n" + ex.Message);
                }
            }
            */
            return newfootroof;
        }
    }
}






/*
 //先弄清楚楼板点的方向和墙创建的方向是否一致
                        bool forward;
                        if (general.IsClose(floor_polygons[i][j + 1], start_line.ep,startwall.Width))
                        {
                            forward = true;
                        }
                        else if (general.IsClose(floor_polygons[i][j - 1], start_line.ep, startwall.Width))
                        {
                            forward = false;
                        }
                        else
                        {
                            TaskDialog.Show("NEED DEBUG", "寻找楼板与被修改的墙相关顶点失败：\nFloor:" + startwall.Id.ToString() + "\nstart wall:" + endwall.Id.ToString() + "墙的起点在楼板顶点集合中，但重点不在");
                            break;
                        }
                        bool delete_direction_forward;
                        if (forward)
                        {
                            line line_reverse = general.GetLineFromPoint(floor_polygons[i][j - 1], floor_polygons[i][j]);
                            line line_swall = general.GetLineFromPoint(floor_polygons[i][j], floor_polygons[i][j + 1]);
                            line line_forward = general.GetLineFromPoint(floor_polygons[i][j + 1], floor_polygons[i][j + 2]);
                            double angle_reverse = general.AngleCalThroughLine(line_reverse, line_swall);
                            double angle_forward = general.AngleCalThroughLine(line_swall, line_forward);
                            //查找中间被删除的墙是在start点的上方还是下方
                            if ((0 < angle_reverse & angle_reverse <= 20) | (160 <= angle_reverse & angle_reverse < 180))
                            {
                                delete_direction_forward = false;
                            }
                            else if ((0 < angle_forward & angle_forward <= 20) | (160 <= angle_forward & angle_forward < 180))
                            {
                                delete_direction_forward = true;
                            }
                            else
                            {
                                TaskDialog.Show("NEED DEBUG", "寻找楼板中被修改的起始墙的下一面墙失败：\nFloor:" + startwall.Id.ToString() + "\nstart wall:" + endwall.Id.ToString() + "墙的起点在楼板顶点集合中，但重点不在");
                                break;
                            }
                        }
                        else //墙的方向和polygon的方向相反
                        {
                            line line_reverse = general.GetLineFromPoint(floor_polygons[i][j+1], floor_polygons[i][j]);
                            line line_swall = general.GetLineFromPoint(floor_polygons[i][j], floor_polygons[i][j-1]);
                            line line_forward = general.GetLineFromPoint(floor_polygons[i][j-1], floor_polygons[i][j-2]);
                            double angle_reverse = general.AngleCalThroughLine(line_reverse, line_swall);
                            double angle_forward = general.AngleCalThroughLine(line_swall, line_forward);
                            //查找中间被删除的墙是在start点的上方还是下方
                            if ((0 < angle_reverse & angle_reverse <= 20) | (160 <= angle_reverse & angle_reverse < 180))
                            {
                                delete_direction_forward = false;
                            }
                            else if ((0 < angle_forward & angle_forward <= 20) | (160 <= angle_forward & angle_forward < 180))
                            {
                                delete_direction_forward = true;
                            }
                            else
                            {
                                TaskDialog.Show("NEED DEBUG", "寻找楼板中被修改的起始墙的下一面墙失败：\nFloor:" + startwall.Id.ToString() + "\nstart wall:" + endwall.Id.ToString() + "墙的起点在楼板顶点集合中，但重点不在");
                                break;
                            }
                        }


                        //确定polygon的起点是否在start wall 和end wall
                        List<XYZ> key_vertices = new List<XYZ>();
                        key_vertices.Add(start_line.sp);
                        key_vertices.Add(start_line.ep);
                        key_vertices.Add(end_line.sp);
                        key_vertices.Add(end_line.ep);
                        int position_ss, position_se, position_es, position_ee;
                        for (int position = 0;i < floor_polygons[i].Count; i++)
                        {                           
                            if (general.IsClose(floor_polygons[i][position],start_line.sp, startwall.Width))
                            {
                                position_ss = position;
                            }
                            else if (general.IsClose(floor_polygons[i][position], start_line.ep, startwall.Width))
                            {
                                position_se = position;
                            }
                            else if (general.IsClose(floor_polygons[i][position], end_line.sp, endwall.Width))
                            {
                                position_es = position;
                            }
                            else if (general.IsClose(floor_polygons[i][position], end_line.ep, endwall.Width))
                            {
                                position_ee = position;
                            }
                        }

                        //if (position_ss == 0 | position_ss == floor_polygons[i].Count)
                        //{
                            
                        //}







                        //储存起始线段和终止线段
                        line startvertices;
                        line endvertices;
                        //四种情况的组合
                        if ((forward & delete_direction_forward))//需要修改的点是正向的
                        {
                            startvertices = general.GetLineFromPoint(floor_polygons[i][j], floor_polygons[i][j+1]);
                            for (int k = j; k < floor_polygons[i].Count(); k++)
                            {
                                if (general.IsClose(end_line.sp, floor_polygons[i][k], endwall.Width))  //有可能找不到end point，end出现在start之前
                                {
                                    if (k != floor_polygons[i].Count())
                                    {
                                        endvertices = general.GetLineFromPoint(floor_polygons[i][k], floor_polygons[i][k + 1]);
                                    }
                                    else 
                                    {
                                        endvertices = general.GetLineFromPoint(floor_polygons[i][k], floor_polygons[i][0]);
                                    }
                                    break;
                                }
                                else 
                                {
                                    floor_newpolygons[i].Remove(floor_polygons[i][k]);
                                }
                            }
                            break;
                        }
                        else if (!forward & !delete_direction_forward)//需要修改的点是正向的，但是ep是该点的上一个点
                        {
                            startvertices = general.GetLineFromPoint(floor_polygons[i][j], floor_polygons[i][j - 1]);
                            for (int k = j; k < floor_polygons[i].Count(); k++)
                            {
                                if (general.IsClose(end_line.ep, floor_polygons[i][k], endwall.Width))
                                {
                                    endvertices = general.GetLineFromPoint(floor_polygons[i][k], floor_polygons[i][k + 1]);
                                    break;
                                }
                                else
                                {
                                    floor_newpolygons[i].Remove(floor_polygons[i][k]);
                                }
                            }
                            break;
                        }
                        else if ((forward & delete_direction_forward))//需要修改的点是反向的
                        {

                        }
                        else if (!forward & !delete_direction_forward)//需要修改的点是反向的，但是ep是该点的上一个点
                        {

                        }

 */