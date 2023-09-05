using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class surface_optimization : IExternalCommand
    {
        public Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            UIApplication uiApp = revit.Application;
            UIDocument UIdoc = revit.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;
            FilteredElementCollector walls = new FilteredElementCollector(doc).OfClass(typeof(Wall));
            Dictionary<Element, List<Element>> surface = new Dictionary<Element, List<Element>>();
            Dictionary<Element, List<Element>> surface_end = new Dictionary<Element, List<Element>>();
            //FilteredElementCollector walls_copy = new FilteredElementCollector(doc).OfClass(typeof(Wall));
            //List<string> surface_searched = new List<string>();
            List<Element> arcwalls = new List<Element>();
            List<Element> linewalls = new List<Element>();
            
            //先找Arc的墙
            string debug_pro = "以下ID的墙既不是Arc也不是Line，需要debug\n";
            foreach (Element el in walls)
            {
                Wall wall = el as Wall;
                LocationCurve locationcurve = wall.Location as LocationCurve;
                Curve curwall = locationcurve.Curve;
                if (curwall.ToString().Contains("Arc"))
                {
                    arcwalls.Add(el);
                }

                else if (curwall.ToString().Contains("Line"))
                {
                    linewalls.Add(el);
                }
                else { debug_pro += el.Id.ToString() + "\n"; }
            }
            if (debug_pro != "以下ID的墙既不是Arc也不是Line，需要debug\n")
            {
                TaskDialog.Show("NEED DEBUG", debug_pro);
            }
            //to do:处理ARCwall
            //找linewall中的小转角墙（即曲面墙）
            List<string> searched = new List<string>();
            double min_length = 6.56;//单位是英尺,等于2米
            foreach (Element wall in linewalls)
            {
                //TaskDialog.Show("index wall",wall.Id.ToString());
                if (!searched.Contains(wall.Id.ToString()) & wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() <= min_length)
                {
                    //searched.Add(wall.Id.ToString());
                    Element wall1 = wall;
                    surface = findsurface.find_adjacent_wall(wall, wall1, linewalls, searched, surface, surface_end);
                }
            }

            //在找出来的surface中去掉只有两个面的小转角面  to do（important）在这一步就需要判断
            Dictionary<Element, List<Element>> surface_edit = new Dictionary<Element, List<Element>>();
            Dictionary<Element, List<Element>> surface_end_edit = new Dictionary<Element, List<Element>>();
            foreach (Element el in surface.Keys)
            {
                if (surface[el].Count > 1)
                {
                    surface_edit.Add(el, surface[el]);
                    surface_end_edit.Add(el, surface_end[el]);
                    //foreach (Element el_end in surface_end.Keys)
                    //{
                        //if (el_end.Id.ToString() == el.Id.ToString())
                        //{
                            //surface_end_edit.Add(el_end, surface_end[el_end]);
                        //}
                    //}
                }
            }

            

            //TaskDialog.Show("COLINEAR", colinear_se.Keys.Count.ToString());
           
            //修改Line类型的曲面墙
            //先找到端点墙
            Dictionary<Element, Element> start_end = new Dictionary<Element, Element>();
            List<Element> keywalls = new List<Element>();
            foreach (Element Swall in surface_end_edit.Keys)
            {
                if (surface_end_edit[Swall].Count == 1)
                {
                    start_end.Add(Swall, surface_end_edit[Swall][0]);
                    keywalls.Add(Swall);
                    keywalls.Add(surface_end_edit[Swall][0]);
                }
                else if (surface_end_edit[Swall].Count == 2)
                {
                    //double min_angle = 30;
                    if ((Swall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() <= min_length) )//& (findsurface.Angle_cal(surface_end_edit[Swall][0], surface_end_edit[Swall][1]) >= min_angle))
                    {
                        start_end.Add(surface_end_edit[Swall][0], surface_end_edit[Swall][1]);
                        keywalls.Add(surface_end_edit[Swall][0]);
                        keywalls.Add(surface_end_edit[Swall][1]);
                    }
                    else //表示这两个墙可能是其实是由一个很长的墙连起来的两个转角
                    {
                        start_end.Add(Swall, surface_end_edit[Swall][0]);
                        start_end.Add(surface_end_edit[Swall][1],Swall);
                        keywalls.Add(Swall);
                        keywalls.Add(surface_end_edit[Swall][0]);
                        keywalls.Add(surface_end_edit[Swall][1]);
                    }
                }
                else
                {
                    if (surface_end_edit[Swall].Count == 0)
                    {
                        start_end.Add(Swall, surface_edit[Swall][surface_edit[Swall].Count - 1]);
                        keywalls.Add(Swall);
                        keywalls.Add(surface_edit[Swall][surface_edit[Swall].Count - 1]);
                        TaskDialog.Show("prompt", Swall.Id.ToString() + ":该墙仅存在一面端点墙，这是不应该发生的，暂时将起始墙和末尾墙连起来");
                    }
                    else if(surface_end_edit[Swall].Count > 2) 
                    { 
                        TaskDialog.Show("NEED DEBUG", Swall.Id.ToString() + ":该墙两面以上的端点墙，说明代码通用性不够，暂且没有修改");
                        keywalls.Add(Swall);
                        foreach (Element el in surface_edit[Swall])
                        {
                            keywalls.Add(el);
                        }
                    }
                    
                }
            }

            //输出信息
            string prompt = "surface dictionary(modified) output\n";
            foreach (Element surfacewall in surface_edit.Keys)
            {
                prompt += "surface " + surfacewall.Id.ToString() + ":\n";
                foreach (Element adwall in surface_edit[surfacewall])
                {
                    prompt += adwall.Id.ToString();
                    prompt += "\n";
                }
            }
            if (prompt == "surface dictionary(modified) output\n")
            {
                TaskDialog.Show("Dic surface", "无Line类型的曲面墙");
            }
            else
            {
                TaskDialog.Show("Dicsurface", prompt);
            }

            //prompt
            string endwall_pro = "start wall and end wall dictionary(modified) output\n";
            foreach (Element startwall in start_end.Keys)
            {
                endwall_pro += "start wall: " + startwall.Id.ToString() + ":\n";
                endwall_pro += "end wall: " + start_end[startwall].Id.ToString() + ":\n";
                endwall_pro += "\n";
            }
            if (endwall_pro != "start wall and end wall dictionary(modified) output\n")
            {
                TaskDialog.Show("end walls output", endwall_pro);
            }
            //输出信息结束


            //查找与修改了的曲面墙相关的楼板,写入dictionary并生成需要修改的新的dictionary
            Dictionary<int, List<List<XYZ>>> floors_polygons = new Dictionary<int, List<List<XYZ>>>();
            Dictionary<int, List<Element>> floorids_Swalls = new Dictionary<int, List<Element>>();
            //List<Element> indexwall_flooredit = new List<Element>();
            //先找到surface相关floor，将其polygon写入到dictionary中
            findfloor.FindRelatedFloors(doc,start_end,floors_polygons,floorids_Swalls);
            TaskDialog.Show("debug prompt","用Swall查找相交的floor并写入polygon成功");

            //edit surface related polygons
            Dictionary<int, List<List<XYZ>>> floors_newpolygons = new Dictionary<int, List<List<XYZ>>>();
            floors_newpolygons = flooredit.EditPolygons_mian(doc, start_end, floorids_Swalls, floors_polygons, floors_newpolygons, surface_edit);


            //开始处理直线墙
            //找共线的墙
            Dictionary<Element, List<Element>> colinear = new Dictionary<Element, List<Element>>();
            Dictionary<Element, List<Element>> colinear_end = new Dictionary<Element, List<Element>>();
            List<Element> surfacewalls = new List<Element>();
            foreach (Element ele in surface_edit.Keys)
            {
                if (!keywalls.Contains(ele))
                {
                    surfacewalls.Add(ele);
                }
                foreach (Element el in surface_edit[ele])
                {
                    if (!keywalls.Contains(el))
                    {
                        surfacewalls.Add(el);
                    }
                }
            }
            List<Element> restwalls = new List<Element>();
            List<string> colinear_searched = new List<string>();
            foreach (Element ele in linewalls)
            {
                if (!surfacewalls.Contains(ele))
                {
                    restwalls.Add(ele);
                }
            }
            foreach (Element wall in restwalls)
            {
                if (!colinear_searched.Contains(wall.Id.ToString()) & wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() <= min_length)
                {
                    colinear_searched.Add(wall.Id.ToString());
                    Element wall1 = wall;
                    colinear = collineation.find_colinear_wall(wall, wall1, restwalls, colinear_searched, colinear, colinear_end);
                }
            }
            Dictionary<Element, Element> colinear_se = new Dictionary<Element, Element>();
            List<Element> keywall_collinear = new List<Element>();
            foreach (Element ele in colinear_end.Keys)
            {
                if (colinear_end[ele].Count == 1)
                {
                    colinear_se.Add(ele, colinear_end[ele][0]);
                    keywall_collinear.Add(ele);
                    keywall_collinear.Add(colinear_end[ele][0]);
                }
                else if (colinear_end[ele].Count == 2)
                {
                    colinear_se.Add(colinear_end[ele][0], colinear_end[ele][1]);
                    keywall_collinear.Add(colinear_end[ele][0]);
                    keywall_collinear.Add(colinear_end[ele][1]);
                }
                else
                {
                    //to do:have not consider the case of less than one and more than two 
                    keywall_collinear.Add(ele);
                    if (colinear_end[ele].Count > 0)
                    {
                        keywall_collinear.AddRange(colinear_end[ele]);
                    }
                }
            }
            

            //找直线墙相关的floor
            Dictionary<int, List<List<XYZ>>> colinear_floorids_polygons = new Dictionary<int, List<List<XYZ>>>();
            Dictionary<int, List<Element>> colinear_floorids_Swalls = new Dictionary<int, List<Element>>();
            findfloor.FindRelatedFloors(doc, colinear_se, colinear_floorids_polygons, colinear_floorids_Swalls);
            //删除不影响楼板形状的直线墙
            findfloor.RemoveIrrelevantWalls(colinear_floorids_Swalls,colinear,colinear_end,colinear_se);
            //输出共线墙信息
            string colinear_debug = "clinear:\n";
            foreach (Element ele in colinear.Keys)
            {
                colinear_debug += ele.Id.ToString() + " :key\n";
                foreach (Element el in colinear[ele])
                {
                    colinear_debug += el.Id.ToString() + "\n";
                }
            }
            TaskDialog.Show("test", colinear_debug);
            string colinear_end_debug = "clinear_end:\n";
            foreach (Element ele in colinear_end.Keys)
            {
                colinear_end_debug += ele.Id.ToString() + " :key\n";
                foreach (Element el in colinear_end[ele])
                {
                    colinear_end_debug += el.Id.ToString() + "\n";
                }
            }
            TaskDialog.Show("test", colinear_end_debug);

            /*
            //show polygon
            string polygons_debug = "origin polygons vertices debug:\n";
            foreach (int floorid in floors_polygons.Keys)
            {
                foreach (List<XYZ> polygon in floors_polygons[floorid])
                {
                    polygons_debug += "***************\n";
                    foreach (XYZ vertice in polygon)
                    {
                        polygons_debug += vertice.ToString() + "\n";
                    }
                }
            }
            TaskDialog.Show("polygon debug", polygons_debug);
            */



            //edit coliear wall related floor's polygon
            Dictionary<int, List<List<XYZ>>> colinear_newpolygons = new Dictionary<int, List<List<XYZ>>>();
            colinear_newpolygons = flooredit.EditColinearPolygons_mian(doc, colinear_se, colinear_floorids_Swalls, colinear_floorids_polygons, colinear_newpolygons, colinear, floors_newpolygons);
            return Result.Succeeded;
            //合成总的修改之后的polygons
            Dictionary<int, List<List<XYZ>>> mixed_newpolygons = new Dictionary<int, List<List<XYZ>>>();
            //foreach (int colinearfloorid in colinear_newpolygons.Keys)
            //{
               // mixed_newpolygons.Add(colinearfloorid, colinear_newpolygons[colinearfloorid]);  
            //}
            foreach (int surfacefloorid in floors_newpolygons.Keys)
            {
                if (!mixed_newpolygons.Keys.Contains(surfacefloorid))
                {
                    mixed_newpolygons.Add(surfacefloorid, floors_newpolygons[surfacefloorid]);
                }
            }
            
            //return Result.Succeeded;
            TaskDialog.Show("debug prompt", "修改楼板的polygon成功");
            //return Result.Succeeded;



            //储存FloorType和Floor的levelid,删除原有的floor
            Dictionary<int, FloorType> floortype_dic = new Dictionary<int, FloorType>();
            Dictionary<int, RoofType> rooftype_dic = new Dictionary<int, RoofType>();
            Dictionary<int, Level> floorlevel_dic = new Dictionary<int, Level>();
            Dictionary<int, double> floors_offset = new Dictionary<int, double>();
            foreach (int floorid in mixed_newpolygons.Keys)
            {
                Element floororroof = doc.GetElement(new ElementId(floorid));
                if (floororroof.GetType().ToString().Contains("Floor"))
                {
                    Floor floor = doc.GetElement(new ElementId(floorid)) as Floor;
                    floortype_dic.Add(floor.Id.IntegerValue, floor.FloorType);
                    floorlevel_dic.Add(floor.Id.IntegerValue, doc.GetElement(floor.LevelId) as Level);
                    floors_offset.Add(floor.Id.IntegerValue, floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble());
                    try
                    {
                        //进行原有楼板的删除
                        using (Transaction tran = new Transaction(doc, "Delete old floor"))
                        {
                            tran.Start();
                            ICollection<ElementId> DeletedElements = doc.Delete(floor.Id);
                            tran.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Delete old floor", floor.Id.ToString() + "\n" + ex.Message);
                    }
                }
                //if the element is footprintroof, save the roof type and 
                else if (floororroof.GetType().ToString().Contains("FootPrintRoof"))
                {
                    FootPrintRoof footroof = doc.GetElement(new ElementId(floorid)) as FootPrintRoof;
                    rooftype_dic.Add(footroof.Id.IntegerValue, footroof.RoofType);
                    floorlevel_dic.Add(footroof.Id.IntegerValue, doc.GetElement(footroof.LevelId) as Level);
                    floors_offset.Add(footroof.Id.IntegerValue, footroof.get_Parameter(BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).AsDouble());
                    try
                    {
                        //进行原有屋顶的删除
                        using (Transaction tran = new Transaction(doc, "Delete old floor"))
                        {
                            tran.Start();
                            ICollection<ElementId> DeletedElements = doc.Delete(floororroof.Id);
                            tran.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Delete old FootPrintRoof", floororroof.Id.ToString() + "\n" + ex.Message);
                    }
                }
                else if (floororroof.GetType().ToString().Contains("ExtrusionRoof"))
                {
                    TaskDialog.Show("TO DO",floororroof.Id.ToString()+"/"+ floororroof.GetType().ToString()+":\n存在与曲面相交的拉伸屋顶或其他类型的屋顶，暂未修改该类型屋顶");
                }
            }
            TaskDialog.Show("debug prompt", "储存与曲面相交的楼板信息并删除相交的楼板成功");
            

            //重建floor
            foreach (int floorid in floortype_dic.Keys)
            {
                try
                {
                    Floor newfloor = flooredit.CreateNewFloor(doc, mixed_newpolygons[floorid], floortype_dic[floorid], floorlevel_dic[floorid], floors_offset[floorid]);
                    if (newfloor == null)
                    {
                        flooredit.CreateNewFloor(doc, floors_polygons[floorid], floortype_dic[floorid], floorlevel_dic[floorid], floors_offset[floorid]);
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("create new floor", floorid.ToString() + "\n" + ex.Message);
                    flooredit.CreateNewFloor(doc, floors_polygons[floorid], floortype_dic[floorid], floorlevel_dic[floorid], floors_offset[floorid]);

                }
                //如果创建失败则恢复之前的楼板 to do：如果可以捕捉revit的错误可以先创建楼板再进行楼板的删除

            }
            //重建roof
            foreach (int floorid in rooftype_dic.Keys)
            {
                try
                {
                    FootPrintRoof newroof = roofedit.CreateNewFootPrintRoof(doc, floors_newpolygons[floorid], rooftype_dic[floorid], floorlevel_dic[floorid], floors_offset[floorid]);
                    if (newroof == null)
                    {
                        roofedit.CreateNewFootPrintRoof(doc, floors_polygons[floorid], rooftype_dic[floorid], floorlevel_dic[floorid], floors_offset[floorid]);
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("create new roof", floorid.ToString() + "\n" + ex.Message);
                    roofedit.CreateNewFootPrintRoof(doc, floors_polygons[floorid], rooftype_dic[floorid], floorlevel_dic[floorid], floors_offset[floorid]);

                }
                //如果创建失败则恢复之前的楼板 to do：如果可以捕捉revit的错误可以先创建楼板再进行楼板的删除,可用result.cancel

            }
            TaskDialog.Show("debug prompt", "重建楼板和屋顶成功");

            List<Element> allsurfaces = new List<Element>();
            //删除非端点的曲面墙
            foreach (Element surfacewall in surface_edit.Keys)
            {
                allsurfaces.Add(surfacewall);
                foreach (Element adwall in surface_edit[surfacewall])
                {
                    allsurfaces.Add(adwall);
                }
            }
            foreach (Element el in allsurfaces)
            {
                bool iskey = false;
                foreach (Element key in keywalls)
                {
                    if (el.Id.ToString() == key.Id.ToString())
                    {
                        iskey = true;
                    }
                }
                if (!iskey)
                {
                    try
                    {
                        //进行元素的删除
                        using (Transaction tran = new Transaction(doc, "Delete surface"))
                        {
                            tran.Start();
                            FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                            dealwitherror.MyFailuresPreprocessor failureProcessor = new dealwitherror.MyFailuresPreprocessor();
                            options.SetFailuresPreprocessor(failureProcessor);
                            tran.SetFailureHandlingOptions(options);
                            ICollection<ElementId> DeletedElements = doc.Delete(el.Id);
                            //tran.Commit();
                            var status = tran.Commit();
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
                        TaskDialog.Show("Delete Wall Warning", el.Id.ToString() + "\n" + ex.Message);
                    }
                }
            }

            //延申墙
            foreach (Element start in start_end.Keys)
            {
                extend.ExtendCurves(doc, start, start_end[start]);
            }

            //修改共线墙
            List<Element> allcolinear = new List<Element>();
            foreach (Element ele in colinear.Keys)
            {
                allcolinear.Add(ele);
                allcolinear.AddRange(colinear[ele]);
            }
            foreach (Element el in allcolinear)
            {
                bool iskey = false;
                foreach (Element key in keywall_collinear)
                {
                    if (el.Id.ToString() == key.Id.ToString())
                    {
                        iskey = true;
                    }
                }
                if (!iskey)
                {
                    try
                    {
                        //进行元素的删除
                        using (Transaction tran = new Transaction(doc, "Delete surface"))
                        {
                            tran.Start();
                            FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                            dealwitherror.MyFailuresPreprocessor failureProcessor = new dealwitherror.MyFailuresPreprocessor();
                            options.SetFailuresPreprocessor(failureProcessor);
                            tran.SetFailureHandlingOptions(options);
                            ICollection<ElementId> DeletedElements = doc.Delete(el.Id);
                            //tran.Commit();
                            var status = tran.Commit();
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
                        TaskDialog.Show("Delete Colinear Wall Warning", el.Id.ToString() + "\n" + ex.Message);
                    }
                }
            }
            
            //延申共线墙
            foreach (Element one in colinear_se.Keys)
            {
                List<XYZ> newpoints = new List<XYZ>();
                newpoints = extend.ColinearPoint(one,colinear_se[one]);
                extend.Create_wall(doc,newpoints[0],newpoints[1],one as Wall);
            }
            //删除起点和终点墙
            foreach (Element one in colinear_se.Keys)
            {
                try
                {
                    //进行元素的删除
                    using (Transaction tran = new Transaction(doc, "Delete start and end wall"))
                    {
                        tran.Start();
                        ICollection<ElementId> DeletedElement1 = doc.Delete(one.Id);
                        ICollection<ElementId> DeletedElement2 = doc.Delete(colinear_se[one].Id);
                        tran.Commit();
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Delete Colinear start and end wall Warning", one.Id.ToString() + "\n" + ex.Message);
                }
            }


            /*
                    if (wallinvertices)
                    {
                        floor_polygons.Add(floor, flooredit.GetFloorBoundaryPolygons(floor, option));
                        floor_newpolygons.Add(floor, floor_polygons[floor]);
                        allfloorstoedit.Add(floor);
                    }




                    //写入dictionary
                    if (!allfloorstoedit.Contains(floor))
                    {
                        //判断floor polygon的vertices是否包含wall的点（如果墙为内墙，有可能不包含在polygon中），如果在则添加到dictionary中
                        bool wallinvertices = false;
                        List<List<XYZ>> polygons = flooredit.GetFloorBoundaryPolygons(floor, option);
                        foreach (List<XYZ> polygon in polygons)
                        {
                            foreach (XYZ vertice in polygon)
                            {
                                if (general.IsClose(vertice, Sline.sp, Swall.Width))
                                {
                                    wallinvertices = true;
                                    break;
                                }
                            }
                            if (wallinvertices)
                            {
                                break;
                            }
                        }
                        if (wallinvertices)
                        {
                            floor_polygons.Add(floor, flooredit.GetFloorBoundaryPolygons(floor, option));
                            floor_newpolygons.Add(floor, floor_polygons[floor]);
                            allfloorstoedit.Add(floor);
                        }
                    }
                }

                //修改floor
                foreach (Floor floor in floor_polygons.Keys)
                {
                    //修改floor:注意Z坐标是不一样的，floor的Z是最底面的Z，所以应该用标高比较合适 to do：现在只修改了墙所在标高上的floor，顶面的没有做修改
                    if (floor.LevelId.ToString() == Swall.LevelId.ToString())
                    {
                        for (int i = 0; i < floor_polygons[floor].Count; i++)
                        //foreach (List<XYZ> pylogon_vertice in floor_polygons[floor])
                        {
                            for (int j = 0; j < floor_polygons[floor][i].Count; j++)
                            {
                                line start_line = general.GetLineFromWall(Swall);
                                line end_line = general.GetLineFromWall(start_end[Swall] as Wall);
                                if (general.IsClose(start_line.sp, floor_polygons[floor][i][j], Swall.Width)) //找到楼板中修改的曲面所在的闭合曲线
                                {
                                    floor_newpolygons.Add(floor,floor_polygons[floor]);
                                    floor_newpolygons[floor][i] = flooredit.edit_floor_polygon(floor_polygons[floor][i], Swall,start_end[Swall_element] as Wall);
                                }
                            }
                        }
                    }
                }
            }
            */




            //在UI上操作，可用于后期改进修改房间功能
            /*
            RevitCommandId id = RevitCommandId.LookupPostableCommandId(PostableCommand.TrimOrExtendToCorner);

            if (uiApp.CanPostCommand(id))

            {
                //调用UIApplication.PostCommand（） 来发送Revit自导的命令。

                uiApp.PostCommand(id);
                
            }
            */

            TaskDialog.Show("prompt", "succeeded");

            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}

//创建墙
/*
public static void Create_wall(Document doc, XYZ sp, XYZ ep, Element wall)
{
    Wall old_wall = wall as Wall;
    Wall new_wall = Wall.Create(doc, Line.CreateBound(sp, ep), old_wall.LevelId, false);
    new_wall.ChangeTypeId(old_wall.GetTypeId());
    ElementId up = old_wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId();
    new_wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(up);
    MyFailuresPreprocessor failureProcessor = new MyFailuresPreprocessor();
}
*/

/*
//找与其末端相连的墙
                foreach (Element wall2 in walls)
                {
                    if (wall2.Id != wall1.Id)
                    {
                        LocationCurve wallcurve2 = wall2.Location as LocationCurve;
                        Line wallline2 = wallcurve2.Curve as Line;
                        IList<XYZ> coordinate2 = wallline2.Tessellate();
                        double wall2_x1 = coordinate2[0].X;
                        double wall2_y1 = coordinate2[0].Y;
                        double wall2_z1 = coordinate2[0].Z;
                        double wall2_x2 = coordinate2[1].X;
                        double wall2_y2 = coordinate2[1].Y;
                        double wall2_z2 = coordinate2[1].Z;
                        XYZ direction2 = wallline2.Direction;
                        //将搜寻范围缩小在z坐标相同的wall中
                        if (wall1_z1 == wall2_z1)
                        {
                            if (wall2_x1 == wall1_x1 & wall2_y1 == wall1_y1)
                            {
                                adjacent_wall.Add(wall2);
                                double sin = direction1.X * direction2.Y - direction2.X * direction1.Y;
                                double cos = direction1.X * direction2.X - direction2.Y * direction1.Y;
                                double angle = Math.Abs(Math.Atan2(sin, cos) * (180 / Math.PI));
                                TaskDialog.Show("test1", "adjacent wall:" + angle.ToString());
                            }
                            else if (wall2_x1 == wall1_x2 & wall2_y1 == wall1_y2)
                            {
                                adjacent_wall.Add(wall2);
                                double sin = direction1.X * direction2.Y - direction2.X * direction1.Y;
                                double cos = direction1.X * direction2.X - direction2.Y * direction1.Y;
                                double angle = Math.Abs(Math.Atan2(sin, cos) * (180 / Math.PI));
                                TaskDialog.Show("test2", "adjacent wall:" + angle.ToString());
                            }
                            else if (wall2_x2 == wall1_x1 & wall2_y2 == wall1_y1)
                            {
                                adjacent_wall.Add(wall2);
                                double sin = direction1.X * direction2.Y - direction2.X * direction1.Y;
                                double cos = direction1.X * direction2.X - direction2.Y * direction1.Y;
                                double angle = Math.Abs(Math.Atan2(sin, cos) * (180 / Math.PI));
                                TaskDialog.Show("test3", "adjacent wall:" + angle.ToString());
                            }
                            else if (wall2_x2 == wall1_x2 & wall2_y2 == wall1_y2)
                            {
                                adjacent_wall.Add(wall2);
                                double sin = direction1.X * direction2.Y - direction2.X * direction1.Y;
                                double cos = direction1.X * direction2.X - direction2.Y * direction1.Y;
                                double angle = Math.Abs(Math.Atan2(sin, cos) * (180 / Math.PI));
                                TaskDialog.Show("test4", "adjacent wall:" + angle.ToString());
                            }
                        }
                    }
                }
                foreach (Element Awall in adjacent_wall)
                {
                    LocationCurve wallcurve = Awall.Location as LocationCurve;
                    Line wallline = wallcurve.Curve as Line;
                    IList<XYZ> coordinate = wallline.Tessellate();
                    double wall2_x1 = coordinate[0].X;
                    double wall2_y1 = coordinate[0].Y;
                    double wall2_z1 = coordinate[0].Z;
                    double wall2_x2 = coordinate[1].X;
                    double wall2_y2 = coordinate[1].Y;
                    double wall2_z2 = coordinate[1].Z;
                    XYZ direction = wallline.Direction;
                    double sin = direction1.X * direction.Y - direction.X * direction1.Y;
                    double cos = direction1.X * direction.X - direction.Y * direction1.Y;
                    double angle = Math.Abs(Math.Atan2(sin, cos) * (180 / Math.PI));
                    if (0 <angle & angle <= 10)
                    {

                    }
                }
    */
/*
 * 
            foreach (Element surfacewall in surface.Keys)
            {
                try
                {
                    foreach (Element adwall in surface[surfacewall])
                    {
                        //进行元素的删除
                        using (Transaction tran = new Transaction(doc, "Delete surface"))
                        {
                            tran.Start();
                            ICollection<ElementId> DeletedElements = doc.Delete(adwall.Id);
                            tran.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Delete Wall Warning", ex.Message);
                }
            }
            ElementId TwallId1 = new ElementId(2794895);
            ElementId TwallId2 = new ElementId(2794917);
            Element Twall1 = doc.GetElement(TwallId1);
            Element Twall2 = doc.GetElement(TwallId2);
            LocationCurve curve1 = Twall1.Location as LocationCurve;
            Line line1 = curve1.Curve as Line;
            XYZ Adirection1 = line1.Direction;
            LocationCurve curve2 = Twall2.Location as LocationCurve;
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
            double Tangle = Math.Abs(Math.Acos(cosValue1) * (180 / Math.PI));
            TaskDialog.Show("test angle:",Tangle.ToString());
            */

/*
            foreach (Element keywall in surface_end.Keys)
            {
                if (surface_end[keywall].Count > 1)
                {
                    TaskDialog.Show("debug", "端点不在key中");
                    try
                    {
                        using (Transaction tran = new Transaction(doc, "Delete key surface"))
                        {
                            tran.Start();
                            ICollection<ElementId> DeletedElements = doc.Delete(keywall.Id);
                            tran.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Delete Key Wall Warning", ex.Message);
                    }
                    if (surface_end[keywall].Count == 2)
                    {

                    }
                    else
                    {
                        TaskDialog.Show("Error!", keywall.Id.ToString() + ": 存在两个以上的端点墙");
                    }

                }
                else
                {
                    XYZ intersection = TrimExtendCurves(keywall, surface_end[keywall][0]);
                    //TaskDialog.Show("debug", keywall.Id.ToString() + "端点在key中:\n " + intersection.ToString());
                    LocationCurve kwallcurve = keywall.Location as LocationCurve;
                    Line kwallline = kwallcurve.Curve as Line;
                    IList<XYZ> kcoordinate = kwallline.Tessellate();

                    LocationCurve wallcurve_end = surface_end[keywall][0].Location as LocationCurve;
                    Line wallline_end = wallcurve_end.Curve as Line;
                    IList<XYZ> coordinate_end = wallline_end.Tessellate();

                    double distance0 = (kcoordinate[0].X - intersection.X) * (kcoordinate[0].X - intersection.X) + (kcoordinate[0].Y - intersection.Y) * (kcoordinate[0].Y - intersection.Y);
                    double distance1 = (kcoordinate[1].X - intersection.X) * (kcoordinate[1].X - intersection.X) + (kcoordinate[1].Y - intersection.Y) * (kcoordinate[1].Y - intersection.Y);
                    double distance_end0 = (coordinate_end[0].X - intersection.X) * (coordinate_end[0].X - intersection.X) + (coordinate_end[0].Y - intersection.Y) * (coordinate_end[0].Y - intersection.Y);
                    double distance_end1 = (coordinate_end[1].X - intersection.X) * (coordinate_end[1].X - intersection.X) + (coordinate_end[1].Y - intersection.Y) * (coordinate_end[1].Y - intersection.Y);
                    if (distance0 > distance1)
                    {
                        try
                        {
                            using (Transaction tran = new Transaction(doc, "extend wall"))
                            {
                                tran.Start();
                                Create_wall(doc, intersection, kcoordinate[0], keywall);
                                tran.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Extend wall Error", ex.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                            using (Transaction tran = new Transaction(doc, "extend wall"))
                            {
                                tran.Start();
                                Create_wall(doc, intersection, kcoordinate[1], keywall);
                                tran.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Extend wall Error3", ex.Message);
                        }
                    }

                    if (distance_end0 > distance_end1)
                    {
                        try
                        {
                            using (Transaction tran = new Transaction(doc, "extend wall"))
                            {
                                tran.Start();
                                Create_wall(doc, intersection, coordinate_end[0], surface_end[keywall][0]);
                                tran.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Extend wall Error4", ex.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                            using (Transaction tran = new Transaction(doc, "extend wall"))
                            {
                                tran.Start();
                                Create_wall(doc, intersection, coordinate_end[1], surface_end[keywall][0]);
                                tran.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Extend wall Error", ex.Message);
                        }
                    }

                    //JoinGeometryUtils.JoinGeometry(doc, keywall, surface_end[keywall][0]);
                }
            }
            */

/*
 foreach (Element Swall_element in start_end.Keys)
            {
                Wall Swall = Swall_element as Wall;
                line Sline = general.GetLineFromWall(Swall);
                List<Floor> floors_intersect_with_swall = findfloor.find_floors_near_surface(doc, Swall);  //不一定所以与墙相交的楼板都需要edit，有可能是内墙
                List<Floor> floors_need_edit = new List<Floor>();
                foreach (Floor floor in floors_intersect_with_swall)
                {
                    List<List<XYZ>> polygons = flooredit.GetFloorBoundaryPolygons(floor, option);
                    foreach (List<XYZ> polygon in polygons)
                    {
                        foreach (XYZ vertice in polygon)
                        {
                            if (general.IsClose(vertice, Sline.sp, Swall.Width))
                            {
                                floors_need_edit.Add(floor);
                                if (!floors_polygons.Keys.Contains(floor))
                                {
                                    floors_polygons.Add(floor, polygons);
                                }
                                break;
                            }
                        }
                        if (floors_polygons.Keys.Contains(floor))
                        {
                            break;
                        }
                    }
                }
                //edit floor's polygon
                foreach (Floor floor in floors_need_edit)
                {
                    //floors_polygons[floor] = flooredit.edit_floor_polygon(floors_polygons[floor], Swall, start_end[Swall_element] as Wall);
                    floors_newpolygons.Add(floor, flooredit.edit_floor_polygon(floors_polygons[floor], Swall, start_end[Swall_element] as Wall));
                }
                
                //edit floor
                foreach (Floor floor in floors_newpolygons.Keys)
                {
                    FloorType floortype = floor.FloorType;
                    Level floorlevel = doc.GetElement(floor.LevelId) as Level;
                    
                    try
                    {
                        //进行原有楼板的删除
                        using (Transaction tran = new Transaction(doc, "Delete old floor"))
                        {
                            tran.Start();
                            ICollection<ElementId> DeletedElements = doc.Delete(floor.Id);
                            tran.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Delete old floor", floor.Id.ToString() + "\n" + ex.Message);
                    }
                    Floor newfloor = flooredit.CreateNewFloor(doc, floors_newpolygons[floor], floortype, floorlevel);
                    //如果创建失败则恢复之前的楼板 to do：如果可以捕捉revit的错误可以先创建楼板再进行楼板的删除
                    if (newfloor == null)
                    {
                        flooredit.CreateNewFloor(doc, floors_polygons[floor], floortype, floorlevel);
                    }
                }

            }
*/