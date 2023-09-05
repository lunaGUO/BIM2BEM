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
    class findsurface
    {
        public static double Angle_cal(Element wall1, Element wall2)
        {
            LocationCurve curve1 = wall1.Location as LocationCurve;
            Line line1 = curve1.Curve as Line;
            XYZ Adirection1 = line1.Direction;
            LocationCurve curve2 = wall2.Location as LocationCurve;
            Line line2 = curve2.Curve as Line;
            XYZ Adirection2 = line2.Direction;  //direction是重点减去起点
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
        
        public static Dictionary<Element, List<Element>> find_adjacent_wall(Element wall, Element wall1, List<Element> lwalls, List<string> search_ed, Dictionary<Element, List<Element>> surface_, Dictionary<Element, List<Element>> surface_end_)
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
            //double wall1_z2 = coordinate1[1].Z;
            //XYZ direction1 = wallline1.Direction;
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
                        //将搜寻范围缩小在z坐标相同的wall中
                        if (wall1_z1 == wall2_z1)
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
            //判断墙之间的夹角
            foreach (Element Awall in adjacent_wall)
            {
                
                if (!search_ed.Contains(Awall.Id.ToString()))
                {
                    double angle = Angle_cal(wall1, Awall);
                    /*
                    LocationCurve Awallcurve = Awall.Location as LocationCurve;
                    Line Awallline = Awallcurve.Curve as Line;

                    XYZ Adirection = Awallline.Direction;
                    double productValue = (Adirection.X * direction1.X) + (Adirection.Y * direction1.Y);  // 向量的乘积
                    double A1 = Math.Sqrt(Adirection.X * Adirection.X + Adirection.Y * Adirection.Y);  // 向量a的模
                    double A2 = Math.Sqrt(direction1.X * direction1.X + direction1.Y * direction1.Y);  // 向量b的模
                    double cosValue = productValue / (A1 * A2);      // 余弦公式
                    if (cosValue < -1 & cosValue > -2)
                    { cosValue = -1; }
                    else if (cosValue > 1 & cosValue < 2)
                    { cosValue = 1; }
                    double angle = Math.Acos(cosValue) * (180 / Math.PI);
                    */
                    double min_length = 6.56;//单位是英尺,等于2米
                    if (((1E-6 < angle & angle <= 20) | (160 <= angle & angle < (180 - 1E-6))) & (Awall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() <= min_length))
                    {
                        if (!surface_.ContainsKey(wall))
                        {
                            List<Element> add_wall = new List<Element>();
                            add_wall.Add(Awall);
                            surface_.Add(wall, add_wall);
                            search_ed.Add(wall.Id.ToString());
                            search_ed.Add(Awall.Id.ToString());
                        }
                        else
                        {
                            surface_[wall].Add(Awall);
                            search_ed.Add(Awall.Id.ToString());
                        }
                        if (!surface_end_.ContainsKey(wall))
                        {
                            List<Element> end_wall = new List<Element>();
                            surface_end_.Add(wall, end_wall);
                        }
                        //TaskDialog.Show("digui debug", wall.Id.ToString() + "\nwall1" + wall1.Id.ToString() + "\nAwall: " + Awall.Id.ToString() +"\n"+ surface_.Keys.Count);
                        surface_ = find_adjacent_wall(wall, Awall, lwalls, search_ed, surface_, surface_end_);
                        //递归循环
                    }
                    //若存在该段墙有夹角小于规定角度的角但该墙的相邻墙的角度不小于规定角度则添加到end的dictionary中
                    else
                    {
                        //TaskDialog.Show("else debug", surface_end_.Keys.Count.ToString() + "\nwall" + wall.Id.ToString() + "\nwall1" + wall1.Id.ToString() + "\nAwall: " + Awall.Id.ToString() + "\n");
                        foreach (Element el in surface_end_.Keys)
                        {
                            //if (surface_end_.Keys.Count > 0)
                            //{
                            //}
                            if (wall.Id.ToString() == el.Id.ToString())
                            {
                                surface_[el].Add(Awall); //？在surface的dic中加入了邻近的墙，可能导致多个diction
                                if (!(surface_end_[el].Contains(Awall)) & !(el.Id.ToString() == Awall.Id.ToString()))
                                {

                                    surface_end_[el].Add(Awall);   //to do换成Awall会不会更恰当？
                                }
                            }
                        }
                        if (Awall.Id.ToString() != wall.Id.ToString())
                        {
                            break;
                        } 
                        /*
                        if (surface_end_.ContainsKey(wall))
                        {
                            TaskDialog.Show("debug", "add element");
                            surface_end_[wall].Add(wall1);
                            search_ed.Add(wall1.Id.ToString());
                        }
                        */
                    }
                    //找到共线的墙
                    
                    
                }
            }
            return surface_;
        }

    }
}

