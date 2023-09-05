using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;



using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Architecture;

namespace StandardLayer
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class StandardLayer : IExternalCommand
    {
        public class AttrForCsvColumnLabel : Attribute
        {
            public string Title { get; set; }
        }
        public static class CsvFileUtility
        {
            public static bool Dic_to_csv(Dictionary<int, List<string>> data, string filepath) 
            {
                bool successFlag = false;
                StringBuilder sb_Text = new StringBuilder();
                StringBuilder strColumn = new StringBuilder();
                StringBuilder strValue = new StringBuilder();
                StreamWriter sw = null;
                try
                {
                    //sw = new StreamWriter(filePath);
                    foreach (int i in data.Keys)
                    {
                        strColumn.Clear();
                        string columns = "standard_" + i.ToString();
                        strColumn.Append(columns);
                        strColumn.Append(",");
                        for (int j = 0; j < data[i].Count; j++)
                        {
                            strColumn.Append(data[i][j]);
                            strColumn.Append(",");
                        }
                        strColumn.Remove(strColumn.Length - 1, 1);
                        sb_Text.AppendLine(strColumn.ToString());
                        File.WriteAllText(filepath, sb_Text.ToString(), Encoding.Default);
                    }
                    
                }
                catch (Exception ex)
                {
                    successFlag = false;
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Dispose();
                    }
                }


                return successFlag;
            }


            public static bool SaveDataToCSVFile<T>(List<T> dataList, string filePath) where T : class
            {


                bool successFlag = true;

                StringBuilder sb_Text = new StringBuilder();
                StringBuilder strColumn = new StringBuilder();
                StringBuilder strValue = new StringBuilder();
                StreamWriter sw = null;
                var tp = typeof(T);
                PropertyInfo[] props = tp.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                try
                {
                    //sw = new StreamWriter(filePath);
                    for (int i = 0; i < props.Length; i++)
                    {
                        var itemPropery = props[i];
                        AttrForCsvColumnLabel labelAttr = itemPropery.GetCustomAttributes(typeof(AttrForCsvColumnLabel), true).FirstOrDefault() as AttrForCsvColumnLabel;
                        if (null != labelAttr)
                        {
                            strColumn.Append(labelAttr.Title);
                        }
                        else
                        {
                            strColumn.Append(props[i].Name);
                        }

                        strColumn.Append(",");
                    }
                    strColumn.Remove(strColumn.Length - 1, 1);
                    /*
                    try
                    {
                        //sw = new StreamWriter(filePath);
                        for (int i = 0; i < props.Length; i++)
                        {
                            string title = "standard" + i.ToString();
                            strColumn.Append(title);
                            strColumn.Append(",");
                        }
                        strColumn.Remove(strColumn.Length - 1, 1);
                        */
                    //sw.WriteLine(strColumn);    
                    //write the column name
                    sb_Text.AppendLine(strColumn.ToString());

                    for (int i = 0; i < dataList.Count; i++)
                    {
                        var model = dataList[i];
                        //strValue.Remove(0, strValue.Length);
                        //clear the temp row value
                        strValue.Clear();
                        for (int m = 0; m < props.Length; m++)
                        {
                            var itemPropery = props[m];
                            var val = itemPropery.GetValue(model, null);
                            if (m == 0)
                            {
                                strValue.Append(val);
                            }
                            else
                            {
                                strValue.Append(",");
                                strValue.Append(val);
                            }
                        }


                        //sw.WriteLine(strValue); 
                        //write the row value
                        sb_Text.AppendLine(strValue.ToString());
                    }
                }
                catch (Exception ex)
                {
                    successFlag = false;
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Dispose();
                    }
                }

                File.WriteAllText(filePath, sb_Text.ToString(), Encoding.Default);

                return successFlag;
            }

         }

        public Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document Doc = revit.Application.ActiveUIDocument.Document;
            List<ElementId> levelIdswithroom = new List<ElementId>();
            Dictionary<ElementId, List<Wall>> Dicwalls = new Dictionary<ElementId, List<Wall>>(); 
            FilteredElementIterator wallsIterator = (new FilteredElementCollector(Doc)).OfClass(typeof(Wall)).GetElementIterator();
            wallsIterator.Reset();
            //找到所有墙的levelId
            while (wallsIterator.MoveNext())
            {
                Wall wall = wallsIterator.Current as Wall;
                bool duplication = false;
                if (wall.LevelId != null)
                {
                    for (int j = 0; j < levelIdswithroom.Count; j++)
                    {
                        if (wall.LevelId.IntegerValue == levelIdswithroom[j].IntegerValue)
                        {
                            duplication = true;
                        }
                    }
                    if (duplication == false)
                    {
                        levelIdswithroom.Add(wall.LevelId);
                    }
                }
            }
            //write walls to Dicwall
            foreach (ElementId levelid in levelIdswithroom)
            {
                Dicwalls.Add(levelid, new List<Wall>());
            }
            wallsIterator.Reset();
            while (wallsIterator.MoveNext())
            {
                Wall wall = wallsIterator.Current as Wall;
                if (wall != null)
                {
                    Dicwalls[wall.LevelId].Add(wall);
                }
            }

            //比较每层楼的wall
            List<ElementId> searchlevel = Dicwalls.Keys.ToList();
            Dictionary<int, List<ElementId>> Dicstandardlayers = new Dictionary<int, List<ElementId>>();
            foreach (ElementId levelid in Dicwalls.Keys)
            {
                Level level1 = Doc.GetElement(levelid) as Level;

                List<ElementId> standardlevelids = new List<ElementId>();
                standardlevelids.Add(levelid);
                if (searchlevel.Contains(levelid))
                {
                    searchlevel.Remove(levelid);
                }
                else { continue; }
                foreach (ElementId levelid2 in searchlevel)
                {
                    int samewallno = 0;
                    List<Wall> same = new List<Wall>();
                    foreach (Wall wall in Dicwalls[levelid])
                    {
                        LocationCurve wallcurve = wall.Location as LocationCurve;
                        Line wallline = wallcurve.Curve as Line;
                        XYZ origin = wallline.Origin;
                        int originX = (int)origin.X;
                        //TaskDialog.Show("debug originX", originX);
                        int originY = (int)origin.Y;
                        XYZ direction = wallline.Direction;
                        int directionX = (int)direction.X;
                        int directionY = (int)direction.Y;
                        int length = (int)wallline.Length;
                        int height = (int)wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();
                        foreach (Wall wall2 in Dicwalls[levelid2])
                        {
                            LocationCurve wallcurve2 = wall2.Location as LocationCurve;
                            Line wallline2 = wallcurve2.Curve as Line;
                            XYZ origin2 = wallline2.Origin;
                            int originX2 = (int)origin2.X;
                            //TaskDialog.Show("debug originX2", originX2.ToString());
                            int originY2 = (int)origin2.Y;
                            XYZ direction2 = wallline2.Direction;
                            int directionX2 = (int)direction2.X;
                            int directionY2 = (int)direction2.Y;
                            int length2 = (int)wallline2.Length;
                            int height2 = (int)wall2.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();
                            if (originX == originX2 & originY == originY2 & directionX == directionX2 & directionY == directionY2 & length == length2 & height == height2)
                            {
                                samewallno++;
                                //same.Add(wall);
                                break;
                            }
                        }
                        
                    }
                    //string debug = "debug\n";
                    //foreach (Wall samewall in same)
                    //{
                        //debug = debug + "\n" + samewall.Id.ToString();
                    //}
                    //TaskDialog.Show("debug111" ,  level1.Name+ "\n" + debug);
                    Level level2 = Doc.GetElement(levelid2) as Level;
                    //TaskDialog.Show("debug", level1.Name + "\n"+ level2.Name+"\n" + samewallno.ToString());
                    if (samewallno >= Dicwalls[levelid].Count)
                    {
                        standardlevelids.Add(levelid2);
                    }
                }
                if (standardlevelids.Count > 1)
                {
                    Dicstandardlayers.Add(levelid.IntegerValue, standardlevelids);
                    foreach (ElementId standardlevel in standardlevelids)
                    {
                        if (searchlevel.Contains(standardlevel))
                        {
                            searchlevel.Remove(standardlevel);
                        }
                    }
                }
            }
            //打印标准层信息
            if (Dicstandardlayers.Count != 0)
            {
                string prompt_dicstand = "标准层信息：";
                Dictionary<int, List<string>> DicstandardlayerNames = new Dictionary<int, List<string>>();
                foreach (int i in Dicstandardlayers.Keys)
                {
                    prompt_dicstand += "\n";
                    List<ElementId> b = Dicstandardlayers[i];
                    List<string> LevelName = new List<string>();
                    foreach (ElementId levelid in b)
                    {
                        Level standardlevel = Doc.GetElement(levelid) as Level;
                        LevelName.Add(standardlevel.Name.ToString());
                    }
                    DicstandardlayerNames.Add(i, LevelName);
                    //string filepath = "C:\\Users\\moon_\\Desktop\\standard" + i.ToString() + ".csv";
                    //CsvFileUtility.SaveDataToCSVFile<string>(LevelName, filepath);
                    prompt_dicstand = prompt_dicstand + "标准层" + i.ToString() + ":";
                    foreach (ElementId stand in Dicstandardlayers[i])
                    {
                        prompt_dicstand = prompt_dicstand + "\n" + "\t";
                        Level standard = Doc.GetElement(stand) as Level;
                        prompt_dicstand = prompt_dicstand + standard.Name + "/" + standard.Id.ToString();
                    }
                }
                TaskDialog.Show("test standard", prompt_dicstand);
                string docpath = Doc.PathName;
                string filepath = docpath + "\\standardlevel" + ".csv";
                CsvFileUtility.Dic_to_csv(DicstandardlayerNames, filepath);
                
            }
            else { TaskDialog.Show("test standard", "不存在标准层"); }




            return Result.Succeeded;
        }

        
    }
    
}
