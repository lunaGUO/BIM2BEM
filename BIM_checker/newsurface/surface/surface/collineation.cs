using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;

namespace surface
{
    class collineation
    {
        public static Dictionary<Element, List<Element>> find_colinear_wall(Element wall, Element wall1, List<Element> lwalls, List<string> search_ed, Dictionary<Element, List<Element>> surface_colinear_, Dictionary<Element, List<Element>> surface_colinear_end_)
        {
            List<Element> adjacent_wall = new List<Element>();
            LocationCurve wallcurve1 = wall1.Location as LocationCurve;
            Curve curve1 = wallcurve1.Curve;
            Line wallline1 = wallcurve1.Curve as Line;
            IList<XYZ> coordinate1 = wallline1.Tessellate();
            double wall1_x1 = coordinate1[0].X;
            double wall1_y1 = coordinate1[0].Y;
            double wall1_z1 = coordinate1[0].Z;
            double wall1_x2 = coordinate1[1].X;
            double wall1_y2 = coordinate1[1].Y;
            XYZ direction1 = wallline1.Direction;
            //TaskDialog.Show("test", "进入内循环");
            //找到和wall1相邻的墙
            foreach (Element wall2 in lwalls)
            {
                if ((wall2.Id != wall1.Id) & (!search_ed.Contains(wall2.Id.ToString())))
                {

                    LocationCurve wallcurve2 = wall2.Location as LocationCurve;
                    Curve curve2 = wallcurve2.Curve;
                    if (curve2.ToString().Contains("Line"))
                    {
                        Line wallline2 = wallcurve2.Curve as Line;
                        IList<XYZ> coordinate2 = wallline2.Tessellate();
                        double wall2_x1 = coordinate2[0].X;
                        double wall2_y1 = coordinate2[0].Y;
                        double wall2_z1 = coordinate2[0].Z;
                        double wall2_x2 = coordinate2[1].X;
                        double wall2_y2 = coordinate2[1].Y;
                        //double wall2_z2 = coordinate2[1].Z;
                        //XYZ direction2 = wallline2.Direction;
                        //将搜寻范围缩小在z坐标相同的wall中并并且墙的类型一样的wall
                        if ((wall1_z1 == wall2_z1) &(wall1.GetTypeId().IntegerValue == wall2.GetTypeId().IntegerValue))
                        {
                            if ((wall2_x1 - 0.05) <= wall1_x1 & wall1_x1 <= (wall2_x1 + 0.05) & (wall2_y1 - 0.05) <= wall1_y1 & wall1_y1 <= (wall2_y1 + 0.05))
                            {
                                adjacent_wall.Add(wall2);
                            }
                            else if ((wall2_x1 - 0.05) <= wall1_x2 & wall1_x2 <= (wall2_x1 + 0.05) & (wall2_y1 - 0.05) <= wall1_y2 & wall1_y2 <= (wall2_y1 + 0.05))
                            {
                                adjacent_wall.Add(wall2);
                            }
                            else if ((wall2_x2 - 0.05) <= wall1_x1 & wall1_x1 <= (wall2_x2 + 0.05) & (wall2_y2 - 0.05) <= wall1_y1 & wall1_y1 <= (wall2_y2 + 0.05))
                            {
                                adjacent_wall.Add(wall2);
                            }
                            else if ((wall2_x2 - 0.05) <= wall1_x2 & wall1_x2 <= (wall2_x2 + 0.05) & (wall2_y2 - 0.05) <= wall1_y2 & wall1_y2 <= (wall2_y2 + 0.05))
                            {
                                adjacent_wall.Add(wall2);
                            }
                        }
                    }
                }
            }
            if (adjacent_wall.Count == 0)
            {
                foreach (Element el in surface_colinear_end_.Keys)
                {
                    if (wall.Id.ToString() == el.Id.ToString())
                    {
                        if (!(surface_colinear_end_[el].Contains(wall1)) & !(el.Id.ToString() == wall1.Id.ToString()))
                        {
                            surface_colinear_end_[el].Add(wall1);   //to do换成Awall会不会更恰当？
                        }
                    }
                }
            }
            else
            {
                foreach (Element Awall in adjacent_wall)
                {
                    if (!search_ed.Contains(Awall.Id.ToString()))
                    {
                        
                            //TaskDialog.Show("1direction", wall1.Id.ToString()+":"+((wall1.Location as LocationCurve).Curve as Line).Direction.ToString());
                            //TaskDialog.Show("Adirection", Awall.Id.ToString() + ":" + ((Awall.Location as LocationCurve).Curve as Line).Direction.ToString());
                            //break;
                        

                        double min_length = 6.56;//单位是英尺,等于2米
                        if ((((wall1.Location as LocationCurve).Curve as Line).Direction.IsAlmostEqualTo(((Awall.Location as LocationCurve).Curve as Line).Direction))& (Awall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() <= min_length))
                        {
                            if (!surface_colinear_.ContainsKey(wall))
                            {
                                List<Element> add_wall = new List<Element>();
                                add_wall.Add(Awall);
                                surface_colinear_.Add(wall, add_wall);
                                search_ed.Add(wall.Id.ToString());
                                search_ed.Add(Awall.Id.ToString());
                            }
                            else
                            {
                                surface_colinear_[wall].Add(Awall);
                                search_ed.Add(Awall.Id.ToString());
                            }
                            if (!surface_colinear_end_.ContainsKey(wall))
                            {
                                List<Element> end_wall = new List<Element>();
                                surface_colinear_end_.Add(wall, end_wall);
                            }
                            //TaskDialog.Show("digui debug", wall.Id.ToString() + "\nwall1" + wall1.Id.ToString() + "\nAwall: " + Awall.Id.ToString() +"\n"+ surface_.Keys.Count);
                            surface_colinear_ = find_colinear_wall(wall, Awall, lwalls, search_ed, surface_colinear_, surface_colinear_end_);
                            //递归循环
                        }
                        //若存在该段墙有夹角小于规定角度的角但该墙的相邻墙的角度不小于规定角度则添加到end的dictionary中
                        else
                        {
                            foreach (Element el in surface_colinear_end_.Keys)
                            {
                                if (wall.Id.ToString() == el.Id.ToString())
                                {
                                    //TaskDialog.Show("else debug", surface_colinear_end_.Keys.Count.ToString() + "\nwall" + wall.Id.ToString() + "\nwall1" + wall1.Id.ToString() + "\nAwall: " + Awall.Id.ToString() + "\n");
                                    if (!(surface_colinear_end_[el].Contains(wall1)) & !(el.Id.ToString() == wall1.Id.ToString()))
                                    {
                                        //TaskDialog.Show("inner else", "inter add end dictionary");
                                        surface_colinear_end_[el].Add(wall1);   //to do换成Awall会不会更恰当？
                                    }
                                }
                            }
                            //if (wall1.Id.ToString() != wall.Id.ToString())
                            //{
                                //break;
                            //}

                        }
                        //找到共线的墙
                    }
                }
            }
            
            return surface_colinear_;
        }
        

    }
}
