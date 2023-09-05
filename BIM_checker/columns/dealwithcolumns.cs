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
    class dealwithcolumns
    {
        public static void DealWithColumns(Document doc, Dictionary<ElementId, List<List<Element>>> Columns_listwalls)
        {
            string error_columns = "以下的柱子没有成功由墙替换，请检查:\n";
            
            foreach (ElementId columnid in Columns_listwalls.Keys)
            {
                List<Wall> new_walls = new List<Wall>();
                bool no_error = true;
                if (Columns_listwalls.Count < 1)
                {
                    try
                    {
                        function.DeleteElement(doc, columnid);
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("ERROR:delete columns", "delete columns,删除相邻墙小于2的柱子报错:\n" + columnid.ToString() + ex.Message);
                    }
                    continue;
                }
                else 
                {
                    try
                    {
                        //用于存新建的墙
                        foreach (List<Element> walllist in Columns_listwalls[columnid])
                        {
                            if (walllist.Count < 2)
                            {
                                continue; //list中只有一面墙则不做处理
                            }
                            else if (walllist.Count == 2)
                            {
                                new_walls.AddRange(TwoWallsNearColumn(doc, walllist));

                            }

                            else if (walllist.Count == 3)
                            {
                                try
                                {
                                    new_walls.AddRange(ThreeWallsNearColumn(doc, walllist));
                                }
                                catch (Exception ex)
                                {
                                    no_error = false;
                                    TaskDialog.Show("Error", "连接三面墙报错：\n" + ex);
                                }

                            }
                            else if (walllist.Count >= 4)
                            {
                                new_walls.AddRange(MultipleWallsNearColumn(doc, walllist));

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("debug",ex.Message);
                    }
                    
                }
                //处理完柱子相关的墙之后再删除相关的柱子
                if (no_error)
                {
                    try
                    {
                        function.DeleteElement(doc, columnid);
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("ERROR:delete columns", "delete columns,删除相邻墙大于等于2的柱子报错:\n" + columnid.ToString() + ex.Message);
                    }
                }
                else
                {
                    error_columns += columnid.ToString() + "\n";
                }
            }
            if (error_columns != "以下的柱子没有成功由墙替换，请检查:\n")
            {
                TaskDialog.Show("Warning", error_columns);
            }
            else
            {
                TaskDialog.Show("prompt", "Replace columns succeed!");
            }
        }


        //只有两面墙与柱子相邻的连接逻辑
        public static List<Wall> TwoWallsNearColumn (Document doc, List<Element> walllist)
        {
            List<Wall> new_walls = new List<Wall>();  //返回新创建的墙
            Wall wall1 = walllist[0] as Wall;
            Wall wall2 = walllist[1] as Wall;
            line line1 = basic.GetLine(wall1);
            line line2 = basic.GetLine(wall2);
            bool parallel = position_recognation.IsParallel(line1, line2);
            if (!parallel)
            {
                new_walls = extend.ExtendCrossWall(doc, line1, line2, wall1, wall2);
            }
            else
            {
                new_walls = extend.ExtendTwoParallelWalls(doc,wall1,wall2);
            }
            return new_walls;
        }



        //当与墙相交的线有三条时，延申相关的三条线
        public static List<Wall> ThreeWallsNearColumn(Document doc,List<Element> walllist)
        {
            //首先判断三面墙的位置关系
            Wall wall_1 = walllist[0] as Wall;
            Wall wall_2 = walllist[1] as Wall;
            Wall wall_3 = walllist[2] as Wall;
            Dictionary<string, List<Wall>> walls_class = position_recognation.ThreeLinesLocation(wall_1, wall_2, wall_3);
            //根据不同的位置关系进行墙的连接
            List<Wall> new_walls = new List<Wall>();
            //存在两条平行线，第三条与第二条垂直或相交，连接策略：
            //首先判断平行的两条线是否共线，如果共线则先连接平行线，如果不共线则先连垂线再垂线和新创建的线相连
            if (walls_class["parallel"].Count == 2)   
            {
                List<Wall> ordered_walls = ThreeWallConnect.OrderTwoColinearOneCrossWalls(walls_class);
                line para_line1 = basic.GetLine(ordered_walls[0]);
                line para_line2 = basic.GetLine(ordered_walls[1]);
                //判断平行的两条线是否共线
                if (position_recognation.IsCollineation(para_line1, para_line2, ordered_walls[0], ordered_walls[1]))  
                {
                    new_walls.AddRange(ThreeWallConnect.TwoColinearOneCross(doc,ordered_walls));
                }
                else //两平行线不共线，先连垂直墙与距垂直墙较远的一平行墙，再延申另一平行墙
                {
                    new_walls.AddRange(ThreeWallConnect.TwoUncolinearOneCross(doc, ordered_walls));
                }
                
            }
            else if (walls_class["parallel"].Count == 3) //三条线平行
            {
                new_walls.AddRange(ThreeWallConnect.ThreeParallelLine(doc, walls_class));
            }
            else if (walls_class["other"].Count == 3) //三条线都既不平行也不垂直
            {
                new_walls.AddRange(ThreeWallConnect.ThreeOtherLine(doc, walls_class));
            }
            return new_walls;
        }


        //大于等于4面墙的连接 todo: 连接顺序还可以再考虑一下，有一些重复的线可以通过算法避免掉
        public static List<Wall> MultipleWallsNearColumn(Document doc, List<Element> walllist)
        {
            List<Wall> new_walls = new List<Wall>();
            //首先找到同一个方向的墙
            List<List<Element>> dif_direct_walls = new List<List<Element>>();
            List<Element> searched = new List<Element>();
            for(int i = 0; i< walllist.Count; i++)
            {
                if (!searched.Contains(walllist[i]))
                {
                    List<Element> para_walls = new List<Element>();
                    para_walls.Add(walllist[i]);
                    searched.Add(walllist[i]);
                    for (int j = i + 1; j < walllist.Count; j++)
                    {
                        if (!searched.Contains(walllist[j]))
                        {
                            if (position_recognation.IsParallel(basic.GetLine(walllist[i] as Wall), basic.GetLine(walllist[j] as Wall)))
                            {
                                para_walls.Add(walllist[j]);
                                searched.Add(walllist[j]);
                            }
                        }
                    }
                    dif_direct_walls.Add(para_walls);
                }
            }
            //根据方向的个数和同一方向墙的数量进行墙的连接
            //如果只有一个方向
            if (dif_direct_walls.Count == 1)
            {
                new_walls.AddRange(extend.ConnectOneDirectionWalls(doc, dif_direct_walls[0]));
            }
            else if (dif_direct_walls.Count == 2) //如果是两个方向
            {
                new_walls.AddRange(extend.ConnectTwoDirectionWalls(doc, dif_direct_walls));
            }
            else   //如果大于两个方向
            {
                new_walls.AddRange(extend.ConnectMulDirectionWalls(doc, dif_direct_walls));
            }
            return new_walls;

        }
    }

     

    /*
    //三面墙中存在两条平行线的情况下,将垂直的墙延申到平行墙
    public static List<Wall> ExtendVerticeOneWall(Document doc, List<Wall>new_walls, Dictionary<string, List<Wall>> walls_class)
    {
        Wall wall1 = walls_class["parallel"][0];
        Wall wall2 = walls_class["parallel"][1];
        line para_line1 = basic.GetLine(wall1);
        line para_line2 = basic.GetLine(wall2);
        Wall wall3 = null;
        if (walls_class["vertical"].Count != 0)
        {
            foreach (Wall wall in walls_class["vertical"])
            {
                if (wall.Id != wall1.Id & wall.Id != wall2.Id)
                {
                    wall3 = wall;
                }
            }
        }
        else if (walls_class["other"].Count != 0)
        {
            foreach (Wall wall in walls_class["other"])
            {
                if (wall.Id != wall1.Id & wall.Id != wall2.Id)
                {
                    wall3 = wall;
                }
            }

            TaskDialog.Show("prompt", "三墙相连：该墙与另两平行墙不垂直\n" + wall3.Id.ToString());
        }
        else
        {
            new_walls.Clear();
            TaskDialog.Show("NEED DEBUG", "三墙相连：找不到第三面墙" + wall1.Id.ToString() + "/" + wall2.Id.ToString());
        }
        line new_line = new line();
        if (new_walls.Count != 0) //成功连接俩平行线
        {
            new_line = basic.GetLine(new_walls[0]);
        }
        else //平行线重合或连接失败
        {
            if (wall3 != null)
            {
                //第三条线与平行线中较近的线相连
                new_line = basic.FindClosestLine(basic.GetLine(wall3), para_line1, para_line1);
            }
        }
        if (wall3 != null)
        {
            try
            {
                line line3 = basic.GetLine(wall3);
                List<Wall> cross_extends = extend.ExtendCrossWall(doc, line3, new_line, wall3, wall2);
                foreach (Wall wall in cross_extends)  //可优化to do：其实连接的墙只有一条的
                {
                    new_walls.Add(wall);
                }
            }
            catch (Exception ex)
            {
                new_walls.Clear();
                TaskDialog.Show("Eroor:", "three line connect: 垂直墙与俩平行连接失败\n" + wall1.Id + " / " + wall2.Id + " / " + wall3.Id + "\n" + ex.Message);
            }
        }
        return new_walls;
    }
    */





    //三条线的连接逻辑
    class ThreeWallConnect
    {

        //两条平行线，另外一个直线与平行线垂直或相交的情况下，返回按照以下顺序排列的wall list
        //按照0:平行线1，1: 平行线2，2: 相交/垂直线段 的顺序返回wallist
        public static List<Wall> OrderTwoColinearOneCrossWalls(Dictionary<string, List<Wall>> walls_classed)
        {
            List<Wall> ordered_walls = new List<Wall>();
            //首先找到平行和相交的线
            ordered_walls.Add(walls_classed["parallel"][0]);
            ordered_walls.Add(walls_classed["parallel"][1]);

            if (walls_classed["vertical"].Count != 0)
            {
                foreach (Wall wall in walls_classed["vertical"])
                {
                    if (wall.Id != ordered_walls[0].Id & wall.Id != ordered_walls[1].Id)
                    {
                        ordered_walls.Add(wall);
                    }
                }
            }
            else if (walls_classed["other"].Count != 0)
            {
                foreach (Wall wall in walls_classed["other"])
                {
                    if (wall.Id != ordered_walls[0].Id & wall.Id != ordered_walls[1].Id)
                    {
                        ordered_walls.Add(wall);
                    }
                }
                //TaskDialog.Show("prompt", "三墙相连：该墙与另两平行墙不垂直\n" + ordered_walls[2].Id.ToString());
            }
            return ordered_walls;
        }

        //连接两条共线的平行线和一条相交的线
        public static List<Wall> TwoColinearOneCross(Document doc, List<Wall> ordered_walls)
        {
            List<Wall> new_walls = new List<Wall>();
            line para_line1 = basic.GetLine(ordered_walls[0]);
            line para_line2 = basic.GetLine(ordered_walls[1]);
            //共线的话线先连接两个平行线，再连接垂直线
            if (!position_recognation.IsSuperposition(para_line1, para_line2))
            {
                new_walls.AddRange(extend.ConnectClosestPointsOfWalls(doc, para_line1, para_line2, ordered_walls[0], ordered_walls[1]));
            }
            else
            {
                //TaskDialog.Show("prompt", "连接三面墙时，平行的两共线墙重合，暂时考虑不做处理：\n" + ordered_walls[0].Id.ToString() + "/" + ordered_walls[1].Id.ToString());
            }
            //连接第三根线
            if (new_walls.Count != 0)
            {
                if (ordered_walls[2] != null)
                {
                    new_walls.AddRange(extend.ExtendCrossWall(doc, basic.GetLine(new_walls[0]), basic.GetLine(ordered_walls[2]), new_walls[0], ordered_walls[2])); ;
                }
                else
                {
                    TaskDialog.Show("NEED DEBUG", "两条平行墙和一条相交墙连接，找不到相交的墙:\n" + ordered_walls[0].Id.ToString() + "/" + ordered_walls[1].Id.ToString());
                }
            }
            else //如果共线的平行线重合或者没有连接上，则没有new_wall产生，则直接与第一条平行线连接
            {
                if (ordered_walls[2] != null)
                {
                    new_walls.AddRange(extend.ExtendCrossWall(doc, basic.GetLine(ordered_walls[0]), basic.GetLine(ordered_walls[2]), ordered_walls[0], ordered_walls[2])); ;
                }
                else
                {
                    TaskDialog.Show("NEED DEBUG", "两条平行墙和一条相交墙连接，找不到相交的墙:\n" + ordered_walls[0].Id.ToString() + "/" + ordered_walls[1].Id.ToString());
                }
            }
            return new_walls;
        }

        //两两垂直但平行的线不共线的情况下,连接三面墙：首先垂直/相交墙与较远的平行线相连，再延长另一条线到垂直或者相交的线
        public static List<Wall> TwoUncolinearOneCross(Document doc, List<Wall> ordered_walls)
        {
            List<Wall> new_walls = new List<Wall>();
            line para_line1 = basic.GetLine(ordered_walls[0]);
            line para_line2 = basic.GetLine(ordered_walls[1]);
            if (ordered_walls[2] != null)
            {
                //step1:垂直墙与较远的平行墙与相连
                line line_vertice = basic.GetLine(ordered_walls[2]);
                //找到垂线离任意平行线最近的点 （如果垂线在两条线中间就会出错）
                XYZ point = basic.FindClosestDirectDistPoint(line_vertice, para_line1);
                //找到较远的平行墙
                double vertice_to_para1 = basic.VertcieDistPointToLine(point, para_line1);
                double vertice_to_para2 = basic.VertcieDistPointToLine(point, para_line2);
                //假设第二条平行线距离垂直/相交线距离较近
                line far_line = para_line2;
                Wall far_wall = ordered_walls[1];
                line closer_line = para_line1;
                Wall closer_wall = ordered_walls[0];
                if (vertice_to_para1 >= vertice_to_para2)
                {
                    far_line = para_line1;
                    far_wall = ordered_walls[0];
                    closer_line = para_line2;
                    closer_wall = ordered_walls[1];
                }
                try
                {
                    new_walls.AddRange(extend.ExtendCrossWall(doc, far_line, line_vertice, far_wall, ordered_walls[2]));

                }
                catch (Exception ex)
                {
                    new_walls.Clear();
                    TaskDialog.Show("Eroor:", "three line connect: 以下垂直/相交墙与较远的平行线连接失败:\n" + ordered_walls[0].Id + " / " + ordered_walls[1].Id + " / " + ordered_walls[2].Id + "\n" + ex.Message);
                }

                //step2: 较近的平行墙与延申到垂直/相交的墙
                if (new_walls.Count != 0)
                {
                    new_walls.AddRange(extend.ExtendLineToFoot(doc, closer_line, line_vertice, closer_wall));
                }
                else
                {
                    new_walls.Clear();
                    TaskDialog.Show("NEED DEBUG", "three line connect: 三条线两两垂直但平行的墙不共线：\n垂直墙连接失败" + closer_wall.Id + " / " + far_wall.Id);
                }
            }
            else
            {
                new_walls.Clear();
                TaskDialog.Show("NEED DEBUG", "three line connect: 三条线两两垂直但平行的墙不共线：找不到垂直的墙\n" + ordered_walls[0].Id + " / " + ordered_walls[1].Id);
            }
            return new_walls;
        }


        //连接策略：找到中间的墙，分别连接中间的和两边的墙
        public static List<Wall> ThreeParallelLine(Document doc, Dictionary<string, List<Wall>> walls_class)
        {
            List<Wall> new_walls = new List<Wall>();

            if (walls_class["parallel"].Count == 3)
            {
                //首先找到位于中间的线
                Wall middlewall = basic.FindMiddleWall(walls_class["parallel"][0], walls_class["parallel"][1], walls_class["parallel"][2]);
                Wall para1, para2;
                if (walls_class["parallel"][0].Id.IntegerValue == middlewall.Id.IntegerValue)
                {
                    para1 = walls_class["parallel"][1];
                    para2 = walls_class["parallel"][2];
                }
                else if (walls_class["parallel"][1].Id.IntegerValue == middlewall.Id.IntegerValue)
                {
                    para1 = walls_class["parallel"][0];
                    para2 = walls_class["parallel"][2];
                }
                else
                {
                    para1 = walls_class["parallel"][0];
                    para2 = walls_class["parallel"][1];
                }
                //将中间的墙分别与两边的墙相连
                new_walls.AddRange(extend.ExtendTwoParallelWalls(doc,middlewall,para1));
                new_walls.AddRange(extend.ExtendTwoParallelWalls(doc, middlewall, para2));
            }
            return new_walls;
        }

        //连接三条既不平行也不相交的墙
        public static List<Wall> ThreeOtherLine(Document doc, Dictionary<string, List<Wall>> walls_class)
        {
            List<Wall> new_walls = new List<Wall>();

            if (walls_class["other"].Count == 3)
            {
                new_walls.AddRange(extend.ExtendCrossWall(doc, basic.GetLine(walls_class["other"][0]), basic.GetLine(walls_class["other"][1]), walls_class["other"][0], walls_class["other"][1]));
                //延长另一条线至不在第三条线上的最近的交点
                //1）先找到第三根线和前两根线的交点
                line line3 = basic.GetLine(walls_class["other"][2]);
                XYZ cross13 = basic.FindIntersection(basic.GetLine(walls_class["other"][0]), basic.GetLine(walls_class["other"][2]));
                XYZ cross23 = basic.FindIntersection(basic.GetLine(walls_class["other"][1]), basic.GetLine(walls_class["other"][2]));
                bool cross13_in_line3 = basic.IfPointInLine(line3, cross13);
                bool cross23_in_line3 = basic.IfPointInLine(line3, cross23);
                if (cross13_in_line3 & cross23_in_line3)
                {
                    //可以不用延申line3 todo：这里可优化，防止延伸的墙与第三面墙相交
                }
                else if (cross13_in_line3 & !cross23_in_line3) //连接wall2 wall3
                {
                    new_walls.AddRange(extend.ExtendCrossWall(doc, basic.GetLine(walls_class["other"][1]), basic.GetLine(walls_class["other"][2]), walls_class["other"][1], walls_class["other"][2]));
                }
                else if (!cross13_in_line3 & cross23_in_line3) //连接wall1 wall3
                {
                    new_walls.AddRange(extend.ExtendCrossWall(doc, basic.GetLine(walls_class["other"][0]), basic.GetLine(walls_class["other"][2]), walls_class["other"][0], walls_class["other"][2]));
                }
                else if (!cross13_in_line3 & !cross23_in_line3) //交点都不在line3中，则连接wall3和任意一个new_wall
                {
                    //判断交点是否在wall1 wall2上, 在那条墙上则连接该墙到交点
                    if (basic.IfPointInLine(basic.GetLine(walls_class["other"][0]), cross13))
                    {
                        new_walls.AddRange(extend.ExtendCrossWall(doc, basic.GetLine(walls_class["other"][0]), basic.GetLine(walls_class["other"][2]), walls_class["other"][0], walls_class["other"][2]));
                    }
                    else if (basic.IfPointInLine(basic.GetLine(walls_class["other"][1]), cross23))
                    {
                        new_walls.AddRange(extend.ExtendCrossWall(doc, basic.GetLine(walls_class["other"][1]), basic.GetLine(walls_class["other"][2]), walls_class["other"][1], walls_class["other"][2]));
                    }
                    else if (new_walls.Count != 0)
                    {
                        new_walls.AddRange(extend.ExtendCrossWall(doc, basic.GetLine(new_walls[0]), basic.GetLine(walls_class["other"][2]), walls_class["other"][2], new_walls[0]));

                    }
                    else //前面连接wall1和wall2失败了，则连接wall2和wall3
                    {
                        new_walls.AddRange(extend.ExtendCrossWall(doc, basic.GetLine(walls_class["other"][1]), basic.GetLine(walls_class["other"][2]), walls_class["other"][1], walls_class["other"][2]));
                    }
                }
            }
            return new_walls;
        }
    }
}
