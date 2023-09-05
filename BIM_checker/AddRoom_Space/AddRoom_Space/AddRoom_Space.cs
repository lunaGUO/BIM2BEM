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
/// 

namespace AddRoom_Space
{

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]

    public class AddSpaces : IExternalCommand
    {

        public static Dictionary<Room, string> RoomsWithWrongName = new Dictionary<Room, string>();
        
        public Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document Doc = revit.Application.ActiveUIDocument.Document;
            

            /// 提取文档中的主要信息
            //get levels and rooms
            List<Level> AllLevels = new List<Level>();
            List<Level> AllLevels_copy = new List<Level>();

            Parameter phase = revit.Application.ActiveUIDocument.Document.ActiveView.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.VIEW_PHASE);
            ElementId phaseId = phase.AsElementId();
            Phase defaultPhase = revit.Application.ActiveUIDocument.Document.GetElement(phaseId) as Phase;
            Dictionary<int, List<Room>> roomDictionary = new Dictionary<int, List<Room>>();
            //Dictionary<int, List<Space>> spaceDictionary = new Dictionary<int, List<Space>>();
            FilteredElementIterator levelsIterator = (new FilteredElementCollector(Doc)).OfClass(typeof(Level)).GetElementIterator();
            ICollection<ElementId> roomsOriginal = (new FilteredElementCollector(Doc)).WherePasses(new RoomFilter()).WhereElementIsNotElementType().ToElementIds();
            //FilteredElementIterator roomsIterator = (new FilteredElementCollector(Doc)).WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_Rooms)).WhereElementIsNotElementType().GetElementIterator();
            //FilteredElementIterator spacesIterator = (new FilteredElementCollector(Doc)).WherePasses(new SpaceFilter()).GetElementIterator();


            //删除不合格房间并得到标高上建立了房间的标高列表

            List<Level> LevelsWithRoom = new List<Level>();
            for (int i =0; i<roomsOriginal.Count;i++)
            {
                Room room = Doc.GetElement(roomsOriginal.ElementAt(i)) as Room;
                if (room.Level == null | room.Area == 0)
                {
                    try
                    {
                        //进行元素的删除
                        using (Transaction tran = new Transaction(Doc, "Delete unplaced room."))
                        {
                            tran.Start();
                            ICollection<ElementId> DeletedElements = Doc.Delete(room.Id);
                            tran.Commit();
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Unplaced Room Delete Warning", room.Id.ToString() + "\n" + ex.Message);
                    }
                }
                //删除未闭合的房间，Area = 0
                /*
                else
                {
                    if (room.Area == 0)
                    {
                        try
                        {
                            //进行元素的删除
                            using (Transaction tran = new Transaction(Doc, "Delete not enclosed room."))
                            {
                                tran.Start();
                                ICollection<ElementId> DeletedElements = Doc.Delete(room.Id);
                                tran.Commit();
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("not enclosed Room Delete Warning", ex.Message);
                        }
                    }
                   
                }
                */
            }
            
            //找到有房间的标高
            ICollection<ElementId> NewroomsOriginal_ = (new FilteredElementCollector(Doc)).WherePasses(new RoomFilter()).WhereElementIsNotElementType().ToElementIds();

            for (int i = 0; i < NewroomsOriginal_.Count; i++)
            {
                Room room = Doc.GetElement(NewroomsOriginal_.ElementAt(i)) as Room;
                bool duplication = false;
                if (room.Level != null)
                {
                    for (int j = 0; j < LevelsWithRoom.Count; j++)
                    {
                        if (room.Level.Name == LevelsWithRoom[j].Name)
                        {
                            duplication = true;
                        }
                    }
                    if (duplication == false)
                    {
                        LevelsWithRoom.Add(room.Level);
                    }
                }
            }
            TaskDialog.Show("test: levelswithroom.count", LevelsWithRoom.Count.ToString());


            /*
            for (int i = 0; i < LevelsWithRoom.Count; i++)  //外循环是循环的次数
            {
                for (int j = LevelsWithRoom.Count - 1; j > i; j--)  //内循环是 外循环一次比较的次数
                {

                    if (LevelsWithRoom[i] == LevelsWithRoom[j])
                    {
                        LevelsWithRoom.RemoveAt(j);
                    }
                }
            }
            */


            //得到所有标高
            levelsIterator.Reset();
            while (levelsIterator.MoveNext())
            {
                Level level = levelsIterator.Current as Level;
                if (level != null)
                {
                    AllLevels.Add(level);
                    AllLevels_copy.Add(level);
                    //spaceDictionary.Add(level.Id.IntegerValue, new List<Space>());
                }
            }

            //删除与LevelsWithRoom高度相同的标高
            foreach (Level LevelWithRoom in LevelsWithRoom)
            {
                foreach (Level level in AllLevels_copy)
                {
                    if (LevelWithRoom.Elevation == level.Elevation & LevelWithRoom.Name != level.Name)
                    {
                        AllLevels.Remove(level);
                    }
                }
            }


            /*
            //测试OwnerViewId
            ElementId Id355 = new ElementId(355);
            Element Element355 = Doc.GetElement(Id355);
            Level level355 = Element355 as Level;

            TaskDialog.Show("测试owner viewID:", level355.OwnerViewId.ToString());
            */
            //删除重复的标高
            levelsIterator.Reset();
            foreach (Level level1 in AllLevels_copy)
            {
                int countlevel = 0;
                double level1Elevation = level1.Elevation;
                foreach (Level level2 in AllLevels)
                {
                    double level2Elevation = level2.Elevation;
                    if (level1Elevation == level2Elevation)
                    {
                        countlevel++;
                    }
                }
                if (countlevel > 1)
                {
                    AllLevels.Remove(level1);
                }
                
            }
            TaskDialog.Show("标高个数", AllLevels.Count().ToString());

            //对标高按高度排序
            AllLevels.Sort(delegate (Level x, Level y)
            {
                return x.Elevation.CompareTo(y.Elevation);
            });

            //将每个room按楼层写入roomDictionary
            foreach (Level level in AllLevels)
            {
                roomDictionary.Add(level.Id.IntegerValue, new List<Room>());
            }
            FilteredElementIterator roomsIterator = (new FilteredElementCollector(Doc)).WherePasses(new RoomFilter()).WhereElementIsNotElementType().GetElementIterator();
            roomsIterator.Reset();
            while (roomsIterator.MoveNext())
            {
                Room room = roomsIterator.Current as Room;
                if (room != null)
                {
                    roomDictionary[room.LevelId.IntegerValue].Add(room);
                }
            }


            //筛选出有楼板的标高
            List<Level> LevelsWithFloor = new List<Level>();
            ICollection<ElementId> floorIcollection = (new FilteredElementCollector(Doc)).OfClass(typeof(Floor)).WhereElementIsNotElementType().ToElementIds();
            if (floorIcollection.Count != 0)
            {
                Floor FirstFloor = Doc.GetElement(floorIcollection.ElementAt(0)) as Floor;
                Level FirstFloorLevel = Doc.GetElement(FirstFloor.LevelId) as Level;
                LevelsWithFloor.Add(FirstFloorLevel);
                for (int i = 0; i < floorIcollection.Count; i++)
                {
                    Floor FloorCurrent = Doc.GetElement(floorIcollection.ElementAt(i)) as Floor;
                    Level FloorLevel = Doc.GetElement(FloorCurrent.LevelId) as Level;
                    bool SameElevation = false;
                    for (int j = 0; j < LevelsWithFloor.Count; j++)
                    {
                        if (FloorLevel.Elevation == LevelsWithFloor[j].Elevation)
                        {
                            SameElevation = true;
                        }
                    }
                    if (SameElevation == false)
                    {
                        LevelsWithFloor.Add(FloorLevel);
                    }
                }
            }
            

            //删除没有楼板的标高上的本不应建立的房间  感觉这里可以优化一下！！！
            foreach (Level roomLevel in LevelsWithRoom)
            {
                bool sameElevation = false;
                foreach (Level floorLevel in LevelsWithFloor)
                {
                    if (roomLevel.Elevation == floorLevel.Elevation)
                    {
                        sameElevation = true;
                    }
                }
                if (!sameElevation)
                {
                    List<Room> WrongRooms = roomDictionary[roomLevel.Id.IntegerValue];    //一旦Dictionary里面的元素被删除，WrongRooms中也会做相应的变化
                    TaskDialog.Show("prompt","楼层："+roomLevel.Name+"/"+roomLevel.Id.ToString()+"上无楼板，所创建的房间将被删除");
                    List<Room> WrongRooms_copy = WrongRooms;
                    List < ElementId > WrongRoomIds = new List<ElementId>();
                    try
                    {
                        foreach(Room room in WrongRooms)
                        //for (int i = 0; i<WrongRooms.Count;i++)
                        {
                            using (Transaction tran = new Transaction(Doc, "Delete wrong room"))
                            {
                                //roomDictionary[roomLevel.Id.IntegerValue].Remove(room);
                                tran.Start();
                                ICollection<ElementId> deletedElements1 = Doc.Delete(room.Id);
                                tran.Commit();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Warning:Delete Wrong Room Failed", ex.Message);
                    }
                }
            }


            //重新写roomdictionary
            //将每个room按楼层写入roomDictionary
            foreach (Level level in AllLevels)
            {
                roomDictionary[level.Id.IntegerValue].Clear();
            }
            
            FilteredElementIterator roomsIterator_new = (new FilteredElementCollector(Doc)).WherePasses(new RoomFilter()).WhereElementIsNotElementType().GetElementIterator();
            roomsIterator_new.Reset();
            while (roomsIterator_new.MoveNext())
            {
                Room room = roomsIterator_new.Current as Room;
                if (room != null)
                {
                    roomDictionary[room.LevelId.IntegerValue].Add(room);
                }
            }

            //筛选出AllLevels中有楼板的标高
            List<Level> AllLevelsWithFloor = new List<Level>();
            foreach (Level level in AllLevels)
            {
                foreach (Level LevelWithFloor in LevelsWithFloor)
                {
                    if (level.Elevation == LevelWithFloor.Elevation)
                    {
                        AllLevelsWithFloor.Add(level);
                    }
                }
            }

            //计算层高(Revit中层高用的是英制单位：英尺,1英尺=304.8毫米)
            //StoryHeight 字典key是标高的ID，value是该标高为底的层高
            Dictionary<int, double> StoryHeight = new Dictionary<int, double>();
            int count_FloorLevel = AllLevelsWithFloor.Count;
            int countAllLeves = AllLevels.Count;
            int temp = 0;
            //top是最顶层的编号
            void cal_storyheight(int top)
            {
                for (int i = 0; i < top; i++)
                {
                    Level eachlevel = AllLevelsWithFloor[i];
                    if (eachlevel != null)
                    {
                        temp++;
                        double lowElevation = AllLevelsWithFloor[i].Elevation;
                        double highElevation = AllLevelsWithFloor[i].Elevation + (3000 / 304.8); //初始化层高为3
                        if (i != top - 1)
                        {
                            highElevation = AllLevelsWithFloor[i + 1].Elevation;
                        }
                        else
                        {
                            int j = 0;
                            foreach (Level level in AllLevels)
                            {
                                if (level.Elevation == AllLevelsWithFloor[i].Elevation)
                                {
                                    highElevation = AllLevels[j + 1].Elevation;
                                }
                                j++;
                            }
                        }
                        //TaskDialog.Show("test",Math .Round ( (highElevation*304.8 - lowElevation*304.8)).ToString ());
                        StoryHeight.Add(eachlevel.Id.IntegerValue, (highElevation - lowElevation));
                    }
                }
                TaskDialog.Show("计算层高次数", temp.ToString());
            }

            if (count_FloorLevel != 0)
            {
                //如果最顶层的标高上有楼板
                if (AllLevelsWithFloor[count_FloorLevel - 1].Elevation == AllLevels[countAllLeves - 1].Elevation)
                {
                    cal_storyheight(count_FloorLevel - 1);
                }
                //如果最顶层标高不是楼板
                if (AllLevelsWithFloor[count_FloorLevel - 1].Elevation < AllLevels[countAllLeves - 1].Elevation)
                {
                    cal_storyheight(count_FloorLevel);
                }
            }
            
            


            //将AllLevelsWithFloor按照标高高度进行从小到大排序
            AllLevelsWithFloor.Sort(delegate (Level x, Level y)
            {
                return x.Elevation.CompareTo(y.Elevation);
            });

            //修改标高名称
            void changeLevelName(List<Level> LevelList)
            {
                //初始化标高名称
                int i = 1;
                foreach (Level level in AllLevels)
                {
                    try
                    {
                        using (Transaction tran = new Transaction(Doc, "ChangeLevelName"))
                        {

                            tran.Start();
                            level.Name = "initialize" + i.ToString();

                            tran.Commit();
                        }
                        i++;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Warning:initialize Level Name", ex.Message);
                    }
                }
                int positiveLevel = 0;
                int negativeLevel = 0;
                foreach (Level level in LevelList)
                {
                    if (level.Elevation >= 0)
                    {
                        positiveLevel++;
                        try
                        {
                            using (Transaction tran = new Transaction(Doc, "ChangeLevelName"))
                            {
                                tran.Start();
                                level.Name = positiveLevel.ToString() + "F";
                                tran.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Warning:Change Level Name", ex.Message);
                        }
                    }
                    else
                    {
                        negativeLevel--;
                    }
                }
                int negativeLevel_copy = negativeLevel;
                if (negativeLevel != 0)
                {
                    for (int j = 0; j < Math.Abs(negativeLevel); j++)
                    {
                        try
                        {
                            using (Transaction tran = new Transaction(Doc, "ChangeLevelName"))
                            {
                                tran.Start();
                                LevelList[j].Name = negativeLevel_copy.ToString() + "F";
                                negativeLevel_copy++;

                                //TaskDialog.Show("j",j.ToString());
                                tran.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Warning:Change Level Name", ex.Message);
                        }
                    }
                }
            }

            //！！！若标高0处没有楼板的图纸在非楼板处设置了标高将会引起命名错误！！！
            bool contain0 = false;
            ElementId Level0Id = new ElementId(0);
            foreach (Level level in AllLevelsWithFloor)
            {
                if (level.Elevation == 0)
                {
                    contain0 = true;
                    Level0Id = level.Id;
                }
            }
            if (contain0)
            {
                changeLevelName(AllLevelsWithFloor);
            }
            else
            {
                changeLevelName(AllLevels);
            }

            

            //为AllLevelsWithFloor中没有平面视图的标高添加平面视图:  
            FilteredElementCollector ViewCollector = new FilteredElementCollector(Doc).OfClass(typeof(ViewFamilyType));
            ElementId PlaneTypeId = new ElementId(0) ;
            //TaskDialog.Show("初始化平面typeID",PlaneTypeId.ToString());
            foreach (ViewFamilyType viewFamilyType in ViewCollector.ToList())
            {
                if ("楼层平面".Equals(viewFamilyType.Name) || "结构平面".Equals(viewFamilyType.Name) || "天花板平面".Equals(viewFamilyType.Name) || "Floor Plan".Equals(viewFamilyType.Name) || "Ceiling Plan".Equals(viewFamilyType.Name))
                {
                    PlaneTypeId = viewFamilyType.Id;
                    break;
                }
            }
            //TaskDialog.Show("寻找到的平面typeID", PlaneTypeId.ToString());
            if (PlaneTypeId.ToString() == "0")
            {
                TaskDialog.Show("Warning：平面视图","不存在平面视图，请先至少设置一个平面视图");
            }
            foreach (Level level in AllLevelsWithFloor)
            {
                ElementId levelPlan = level.FindAssociatedPlanViewId();
                if (levelPlan.ToString() == "-1")
                {
                    TaskDialog.Show("notice:平面视图", "将自动创建平面视图");
                    try
                    {
                        using (Transaction tran = new Transaction(Doc, "Add ViewPlan"))
                        {
                            tran.Start("创建平面视图");
                            ViewPlan LevelView = ViewPlan.Create(Doc, PlaneTypeId, level.Id);
                            tran.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error：创建重命名后标高的平面视图失败", level.Name.ToString() + ":\n" + ex.ToString());
                    }
                }
            }


            /*
            //output levels
            string levelstest = "LevelSorted:\n";
            foreach (Level level in AllLevels)
            {
                levelstest += level.Name.ToString() + "\n";
            }
            TaskDialog.Show("test", levelstest);
            */

            /*
                        //output roomDictionary
                        string room_str = "Rooms:\n\n";
                        levelsIterator.Reset();
                        while (levelsIterator.MoveNext())
                        {
                            room_str += "\nLevel:";
                            Level level = levelsIterator.Current as Level;
                            room_str += level.Name.ToString() + "\n";
                            List<Room> rooms = roomDictionary[level.Id.IntegerValue];
                            foreach (Room room in rooms)
                            {
                                room_str = room_str + room.Name.ToString() + "\n";
                            }

                        }
                        TaskDialog.Show("room", room_str);
            */
            


            //在没有房间的位置添加房间：即调用添加房间函数，面积小于0.5m^2的区域不会添加房间
            //在有Floor的标高上添加房间
            int countfloor = 0;
            foreach (Level FloorLevel in AllLevelsWithFloor)
            {
                CreateRoom(FloorLevel);
                countfloor++;
            }
            TaskDialog.Show("创建房间完成", "有"+countfloor.ToString()+"层楼板");
                
            
            void CreateRoom(Level level)
            {
                PlanTopology pt;
                var RoomWallList = new List<Element>();
                List < Room > newrooms = new List<Room>();
                try
                {
                    using (Transaction tran = new Transaction(Doc, "AddNewRooms"))
                    {
                        tran.Start();
                        pt = Doc.get_PlanTopology(level, defaultPhase);
                        tran.Commit();
                    }
                    //int j = 0;
                    foreach (PlanCircuit pc in pt.Circuits)
                    {
                        double area = pc.Area;    //aera的单位是平方英尺
                        double area_sqm = UnitUtils.Convert(area, DisplayUnitType.DUT_SQUARE_FEET, DisplayUnitType.DUT_SQUARE_METERS);
                        if (area_sqm > 0.5)
                        {
                            if (pc == null)
                            {
                                continue;
                            }
                            if (!pc.IsRoomLocated)
                            {
                                using (Transaction tran = new Transaction(Doc, "AddNewRooms"))
                                {
                                    tran.Start();
                                    Room newRoom = Doc.Create.NewRoom(null, pc);
                                    newrooms.Add(newRoom);
                                    tran.Commit();
                                    //TaskDialog.Show("test",newRoom.Name);
                                    //j++;
                                }
                            }
                        }
                        //TaskDialog.Show("创建房间个数", j.ToString());
                    }
                }
                catch (Exception ex)
                {
                    string ex1 = "Could not find a point within this circuit";
                    if (ex.ToString().Contains(ex1))
                    {
                        TaskDialog.Show("Error:Create Room failed", level.Id.ToString()+"/"+level.Name+"\n");
                    }
                    TaskDialog.Show("Error:Create Room failed", level.Id.ToString() + "/" + level.Name + "\n"+ex.ToString());
                }

            }

            ICollection<ElementId> room_afteradd = (new FilteredElementCollector(Doc)).WherePasses(new RoomFilter()).WhereElementIsNotElementType().ToElementIds();
            for (int i = 0; i < room_afteradd.Count; i++)
            {
                Room room = Doc.GetElement(room_afteradd.ElementAt(i)) as Room;
                if (room.Area == 0)
                {
                    try
                    {
                        //进行元素的删除
                        using (Transaction tran = new Transaction(Doc, "Delete not enclosed room."))
                        {
                            tran.Start();
                            ICollection<ElementId> DeletedElements = Doc.Delete(room.Id);
                            tran.Commit();
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("not enclosed Room Delete Warning", ex.Message);
                    }
                }
            }
            
            //重新写roomdictionary
            //将每个room按楼层写入roomDictionary
            foreach (Level level in AllLevels)
            {
                roomDictionary[level.Id.IntegerValue].Clear();
            }

            FilteredElementIterator roomsIterator_afteradd = (new FilteredElementCollector(Doc)).WherePasses(new RoomFilter()).WhereElementIsNotElementType().GetElementIterator();
            roomsIterator_new.Reset();
            while (roomsIterator_afteradd.MoveNext())
            {
                Room room = roomsIterator_afteradd.Current as Room;
                if (room != null)
                {
                    roomDictionary[room.LevelId.IntegerValue].Add(room);
                }
            }

            //先初始化房间高度
            int count_room = 0;
            foreach (int i in StoryHeight.Keys)
            {
                List<Room> EachLevelRooms = roomDictionary[i];
                //double story_m = UnitUtils.Convert(StoryHeight[i], DisplayUnitType.DUT_DECIMAL_FEET, DisplayUnitType.DUT_MILLIMETERS);
                //TaskDialog.Show("初始房间高度", story_m.ToString());
                foreach (Room eachLevelRoom in EachLevelRooms)
                {
                    try
                    {
                        using (Transaction tran = new Transaction(Doc, "InitializeRoomHeight"))
                        {
                            tran.Start();
                            eachLevelRoom.LimitOffset = StoryHeight[i];
                            count_room++;
                            tran.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("warning: 初始化房间高度错误", ex.Message);
                    }
                }
            }
            TaskDialog.Show("初始化高度的房间个数为", count_room.ToString());

            //查找每个房间对应的楼板并计算房间净高
            string WarningRooms ="没有找到下列房间的上边界楼板，将采用层高作为房间高度\n";
            SpatialElementBoundaryOptions se = new SpatialElementBoundaryOptions();
            Dictionary<int, double> RoomHeight = new Dictionary<int, double>();
            //先将RoomHeight字典初始化
            foreach (Level level in AllLevels)
            {
                
                foreach (Room room in roomDictionary[level.Id.IntegerValue])
                {
                    double initial = (double)room.LimitOffset;
                    RoomHeight.Add(room.Id.IntegerValue, initial);
                }
            }

            //计算对应房间的净高！！！在同一高程的楼板厚度才会被减掉！！！
            int count_wrongroom = 0;
            string prompt = "房间中的楼板Waning:\n";
            foreach (Level level in AllLevels)
            {
                foreach (Room room in roomDictionary[level.Id.IntegerValue])
                {
                    bool findFloor = false;
                    se.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;
                    SpatialElementGeometryCalculator calculator = new SpatialElementGeometryCalculator(Doc, se);
                    try
                    {
                        Solid solid = calculator.CalculateSpatialElementGeometry(room).GetGeometry(); //！！！有报错！！！
                        //var list = new FilteredElementCollector(Doc).WhereElementIsNotElementType().WherePasses(new ElementIntersectsSolidFilter(solid)).ToList();
                        FilteredElementCollector RoomElementCollector = new FilteredElementCollector(Doc).WhereElementIsNotElementType().WherePasses(new ElementIntersectsSolidFilter(solid));
                        //将房间中所有的element中floor过滤出来
                        ElementClassFilter FloorFilter = new ElementClassFilter(typeof(Floor));
                        RoomElementCollector = RoomElementCollector.WherePasses(FloorFilter);
                        var RoomFloorList = RoomElementCollector.ToList();
                        //var RoomFloorList_copy = RoomFloorList;
                        var RoomFloorList_copy = new List<Floor>();
                        foreach (Floor element in RoomFloorList)
                        {
                            RoomFloorList_copy.Add(element);
                        }
                        /*
                        //test code
                        TaskDialog.Show("test1",RoomFloorList_copy.Count.ToString ());
                        RoomFloorList.RemoveAt(0);
                        TaskDialog.Show("test2",RoomFloorList_copy.Count.ToString());
                        //test code
                        */

                        //将房间的floor中高程相同的Floor中厚度较大的floor删除
                         int floorcount = RoomFloorList.Count;
                        if (floorcount < 1)
                        {
                            prompt += room.Name + "：存在" + floorcount.ToString() + "块楼板，可能顶部无边界\n";
                        }
                        if (floorcount > 1)
                        {
                            prompt += room.Name + "：存在" + floorcount.ToString() + "块楼板，将按厚度最小的楼板计算净高\n";
                            //TaskDialog.Show("warning:房间有多块楼板", room.Name + "：存在" + floorcount.ToString() + "块楼板，将按厚度最小的楼板计算净高");
                            for (int i = 0; i < RoomFloorList_copy.Count; i++)
                            {
                                Floor roomfloor_copy = RoomFloorList_copy[i] as Floor;
                                ElementId FloorLevelId = roomfloor_copy.LevelId as ElementId;
                                Level FloorLevel = Doc.GetElement(FloorLevelId) as Level;
                                double FloorLevelE = FloorLevel.Elevation;
                                int SomeLevelE = 0;
                                List<double> floor_thickness = new List<double>();
                                for (int j = 0; j < RoomFloorList.Count; j++)
                                {
                                    Floor roomfloor = RoomFloorList[j] as Floor;
                                    ElementId FloorLevelId2 = roomfloor.LevelId as ElementId;
                                    Level FloorLevel2 = Doc.GetElement(FloorLevelId2) as Level;
                                    double FloorLevelE2 = FloorLevel2.Elevation;
                                    if (FloorLevelE == FloorLevelE2)
                                    {
                                        SomeLevelE ++;
                                        floor_thickness.Add((roomfloor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM)).AsDouble());
                                    }
                                }
                                if (SomeLevelE > 1)
                                {
                                    if (floor_thickness.Count != 0)
                                    {
                                        if ((roomfloor_copy.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM)).AsDouble() != floor_thickness.Min())
                                        {
                                            RoomFloorList.Remove(roomfloor_copy);
                                        }
                                        else
                                        {
                                            int MinThickness = 0;
                                            for (int a=0; a < floor_thickness.Count; a++)
                                            {
                                                if ((roomfloor_copy.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM)).AsDouble() == floor_thickness.Min())
                                                {
                                                    MinThickness++;
                                                }
                                                if (MinThickness > 1)
                                                {
                                                    RoomFloorList.Remove(roomfloor_copy);
                                                }
                                            }

                                        }
                                    }
                                    
                                }
                            }

                        }
                        
                        //TaskDialog.Show("test2", RoomFloorList.Count.ToString());

                        //开始计算净高
                        foreach (var element in RoomFloorList)
                        {
                            Floor floor = element as Floor;
                            if (floor != null)
                            {
                                findFloor = true;
                                ElementId floorlevel = floor.LevelId;
                                Parameter thickness = floor.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
                                double thickness_FEET = thickness.AsDouble();   //使用英制单位：英尺
                                double thickness_MILLIMETER = UnitUtils.Convert(thickness_FEET, DisplayUnitType.DUT_DECIMAL_FEET, DisplayUnitType.DUT_MILLIMETERS);
                                //TaskDialog.Show("FloorThickness", floor.Id.ToString() +"/"+ thickness_MILLIMETER.ToString());
                                //RoomHeight.Add(room.Id.IntegerValue, (StoryHeight[level.Id.IntegerValue] - thickness_FEET));
                                RoomHeight[room.Id.IntegerValue] = RoomHeight[room.Id.IntegerValue] - thickness_FEET;
                                if (RoomHeight[room.Id.IntegerValue] <=0)
                                {
                                    TaskDialog.Show("Warning:计算房间净高","房间"+room.Name+room.Id.ToString()+":计算净高结果小于等于0");
                                }
                            }
                        }
                        if (!findFloor)
                        {
                            //RoomHeight.Add(room.Id.IntegerValue, StoryHeight[level.Id.IntegerValue]);
                            if (StoryHeight.Keys.Contains(level.Id.IntegerValue))
                            {
                                RoomHeight[room.Id.IntegerValue] = StoryHeight[level.Id.IntegerValue];
                            }
                            /*
                            else
                            {
                                //若无层高则将第一层房间高度赋给room
                                RoomHeight[room.Id.IntegerValue] = StoryHeight[Level0Id.IntegerValue];
                            }
                            */
                            WarningRooms += room.Id.ToString() + "\t" + room.Name + "\n";
                            count_wrongroom++;
                        }
                    }
                    catch (Exception ex)
                    {
                        string messege = ex.ToString();
                        if (message.Contains("calculate the room 3d geometry"))
                        {
                            TaskDialog.Show("Warning: 计算房间净高", "可能是存在体积为0的房间，报错房间为：\n" + room.Name + ":\n" + ex.ToString());
                        }
                        else
                        {
                            TaskDialog.Show("Warning: 计算房间净高", "计算房间净高报错，报错房间为：\n"+room.Name + ":\n" + ex.ToString());
                        }
                    }
                }
            }
            if (count_wrongroom != 0)
            {
                TaskDialog.Show("Warning: ", "没有找到   " + WarningRooms + "   所包含的楼板,将采用层高作为上述房间的高度");
            }
            if (prompt != "房间中的楼板Waning:\n")
            {
                TaskDialog.Show("warning:房间楼板", prompt);
            }            

            //判断已有房间的高度是否等于房间净高，修改高度不等于房间净高的房间   （要排除wellhole和staircase）
            List <Room> WrongRoonHeight = new List<Room>();
            try
            {
                foreach (int i in StoryHeight.Keys)
                {
                    //ElementId levelId = new ElementId(i);
                    //Element level = Doc.GetElement(levelId );
                    List<Room> EachLevelRooms = roomDictionary[i];
                    foreach (Room eachLevelRoom in EachLevelRooms)
                    {
                        if (RoomHeight[eachLevelRoom.Id.IntegerValue] != eachLevelRoom.LimitOffset && eachLevelRoom.Name != "staircase" && eachLevelRoom.Name != "wellhole")
                        {
                            WrongRoonHeight.Add(eachLevelRoom);
                            using (Transaction tran = new Transaction(Doc, "ChangeRoomHeight"))
                            {
                                tran.Start();
                                eachLevelRoom.LimitOffset = RoomHeight[eachLevelRoom.Id.IntegerValue];
                                tran.Commit();
                            }
                        }
                        /*
                        if(eachLevelRoom.Name.Contains( "staircase" )|| eachLevelRoom.Name.Contains("wellhole"))
                        {
                            using (Transaction tran = new Transaction(Doc, "ChangeRoom(staircase/wellhole)Height"))
                            {
                                tran.Start();
                                eachLevelRoom.LimitOffset =  AllLevels.Last().Elevation;  //楼梯间和竖井的高度假设为最高标高到地面的高度
                                tran.Commit();
                            }
                        }
                        */
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("ChangeRoomHeight Warning",ex.Message);
            }

            /// <summary>
            /// 修改房间名字
            /// </summary>
             
            //房间名字预设
            string[] roomnames = { "办公室", "会议室","走廊", "消防前室","楼梯间", "卫生间", "空调机房", "不设空调的房间","其他不可穿管的房间","空调水管井","排风井","排烟井","新风井","加压风井","风井","强电间","弱电间","消防电梯","电梯"};
            string[] roomEnglishNames = { "office", "meeting", "corridor", "fire_front_room", "staircase", "toilet", "AC_plant", "NoAC", "NoDuct","KTSJ","PF","PY","XF","JY","FJ","QD","RD","XDT","DT"};

            List<string> RoomNames = new List<string>(roomnames);
            List<string> RoomEnglishNames = new List<string>(roomEnglishNames);
            Dictionary<string, string> roomNamesDictionary = new Dictionary<string, string>();
            for (int i = 0; i < RoomNames.Count; i++)
            {
                roomNamesDictionary.Add(RoomNames[i], RoomEnglishNames[i]);
            }
            //将房间名字修改为英文
            void ChangeRoomName(string OnlyName, Room room)
            {
                try
                { 
                    //TaskDialog.Show("test", "Translate RoomName to English");
                    using (Transaction tran = new Transaction(Doc, "Translate RoomName to English"))
                    {

                        tran.Start();
                        if (!roomEnglishNames.Contains(OnlyName))
                        {
                            room.Name = roomNamesDictionary[OnlyName];
                        }
                        tran.Commit();
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("ChangeRoomNames Waring", ex.ToString());
                }

            }

            //首先判断已有的房间名字是否规范

            //List<string> RoomWithWrongName = new List<string>();
            foreach (int levelId in roomDictionary.Keys)
            {
                List<Room> Rooms_EachLevel = roomDictionary[levelId];
                foreach (Room eachRoom in Rooms_EachLevel)
                {
                    string RoomNumber = " " + eachRoom.Number;
                    string OnlyName = eachRoom.Name.Replace(RoomNumber, "");
                    //TaskDialog.Show("test1", OnlyName );
                    //TaskDialog.Show("test2", RoomNames[0]);
                    //if (RoomNames.Contains(OnlyName))
                    //{
                        //ChangeRoomName(OnlyName, eachRoom);
                    //}
                    if (! (RoomNames.Contains(OnlyName) || RoomEnglishNames.Contains(OnlyName)))
                    {
                        RoomsWithWrongName.Add(eachRoom, eachRoom.Name);
                        
                    }

                }
            }

            while (RoomsWithWrongName.Count != 0)
            {
                
                    TaskDialog.Show("Test","房间名字不规范，请修改房间名字");
                   
                                //设计选择窗口

                                    try
                                    {
                                        Transaction documentTransaction = new Transaction(Doc, "AmendRoomName");
                                        documentTransaction.Start();

                                        // Create a new instance of class DataManager
                                        DataManager dataManager = new DataManager(revit);

                                        System.Windows.Forms.DialogResult result;

                                        // Create a form
                                        using (AmendRoomName mainForm = new AmendRoomName(dataManager))
                                        {
                                            result = mainForm.ShowDialog();
                                        }
                                        documentTransaction.Commit();
                    /*
                                                            if (result == System.Windows.Forms.DialogResult.OK)
                                                            {
                                                                documentTransaction.Commit();
                                                                return Autodesk.Revit.UI.Result.Succeeded;
                                                            }
                                                            else
                                                            {
                                                                documentTransaction.RollBack();
                                                                return Autodesk.Revit.UI.Result.Cancelled;
                                                            }
                    */
                }

                                    catch (Exception ex)
                                    {
                                        // If there are something wrong, give error information and return failed
                                        message = ex.Message;
                                        TaskDialog.Show("catch",message);
                                        return Autodesk.Revit.UI.Result.Failed;
                                    }
                RoomsWithWrongName.Clear();
                foreach (int levelId in roomDictionary.Keys)
                {
                    List<Room> Rooms_EachLevel = roomDictionary[levelId];
                    foreach (Room eachRoom in Rooms_EachLevel)
                    {
                        string RoomNumber = " " + eachRoom.Number;
                        string OnlyName = eachRoom.Name.Replace(RoomNumber, "");
                        
                        //if (RoomNames.Contains(OnlyName))
                        //{
                        //ChangeRoomName(OnlyName, eachRoom);
                        //}
                        if (!(RoomNames.Contains(OnlyName) || RoomEnglishNames.Contains(OnlyName)))
                        {
                            RoomsWithWrongName.Add(eachRoom, eachRoom.Name);
                        }

                    }
                }
            }
            if(RoomsWithWrongName.Count == 0)
            {
                TaskDialog.Show("Test", "房间名字修改完成");
            }

            roomsIterator.Reset();
            while(roomsIterator.MoveNext())
            {
                Room eachRoom = roomsIterator.Current as Room;
                string RoomNumber = " " + eachRoom.Number;
                string OnlyName = eachRoom.Name.Replace(RoomNumber, "");
                //TaskDialog.Show("test1", OnlyName );
                //TaskDialog.Show("test2", RoomNames[0]);
                
                ChangeRoomName(OnlyName, eachRoom);
                
            }
        
            return Autodesk.Revit.UI.Result.Succeeded;
        }

    }
}






/*
            void Initialize()
            {

                Dictionary<int, List<Room>> roomDictionary = new Dictionary<int, List<Room>>();
                Dictionary<int, List<Space>> spaceDictionary = new Dictionary<int, List<Space>>();
                FilteredElementIterator levelsIterator = (new FilteredElementCollector(Doc)).OfClass(typeof(Level)).GetElementIterator();
                FilteredElementIterator roomsIterator = (new FilteredElementCollector(Doc)).WherePasses(new RoomFilter()).GetElementIterator();
                FilteredElementIterator spacesIterator = (new FilteredElementCollector(Doc)).WherePasses(new SpaceFilter()).GetElementIterator();
                levelsIterator.Reset();
                while (levelsIterator.MoveNext())
                {
                    Level level = levelsIterator.Current as Level;
                    if (level != null)
                    {
                        AllLevels.Add(level);
                        roomDictionary.Add(level.Id.IntegerValue, new List<Room>());
                        spaceDictionary.Add(level.Id.IntegerValue, new List<Space>());
                    }
                }

                roomsIterator.Reset();
                string room_str = "Rooms:\n\n";
                while (roomsIterator.MoveNext())
                {

                    Room room = roomsIterator.Current as Room;
                    if (room != null)
                    {
                        roomDictionary[room.LevelId.IntegerValue].Add(room);
                    }
                }
                //output roomDictionary
                levelsIterator.Reset();
                while (levelsIterator.MoveNext())
                {
                    room_str += "\nLevel:";
                    Level level = levelsIterator.Current as Level;
                    room_str += level.Name.ToString()+"\n";
                    List<Room> rooms = roomDictionary[level.Id.IntegerValue];
                    foreach (Room room in rooms)
                    {
                        room_str = room_str + room.Name.ToString() + "\n";
                    }

                }

               TaskDialog.Show("room", room_str);

             Add_roomManager = new RoomManager(revit, roomDictionary);

                spacesIterator.Reset();
                while (spacesIterator.MoveNext())
                {

                    Space space = roomsIterator.Current as Space;
                    if (space != null)
                    {
                        spaceDictionary[space.LevelId.IntegerValue].Add(space);
                    }
                }


            }
*/
//Add_roomManager = new RoomManager(revit, roomDictionary);
/*
spacesIterator.Reset();
while (spacesIterator.MoveNext())
{

    Space space = spacesIterator.Current as Space;   //考虑Iterator为0的情况
    if (space != null)
    {
        spaceDictionary[space.LevelId.IntegerValue].Add(space);
    }
}
*/
/*
            //output spaceDictionary
            string space_str = "Spaces:\n\n";
            levelsIterator.Reset();
            while (levelsIterator.MoveNext())
            {
                space_str += "\nLevel:";
                Level level = levelsIterator.Current as Level;
                space_str += level.Name.ToString() + "\n";
                List<Space> spaces = spaceDictionary[level.Id.IntegerValue];
                foreach (Space space in spaces)
                {
                    space_str = space_str + space.Name.ToString() + "\n";
                }

            }
            TaskDialog.Show("space", space_str);
*/


/*
                   else
                   {
                       //删除面积小于5的房间并修改对应墙的房间边界属性
                       double area = room.Area;    //aera的单位是平方英尺
                       double area_sqm = UnitUtils.Convert(area, DisplayUnitType.DUT_SQUARE_FEET, DisplayUnitType.DUT_SQUARE_METERS);
                       double small_area = 5;
                       //bool changeBounding = false;
                       if (area_sqm < small_area)
                       {
                           SpatialElementBoundaryOptions seb = new SpatialElementBoundaryOptions();
                           seb.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;
                           SpatialElementGeometryCalculator calculator = new SpatialElementGeometryCalculator(Doc, seb);
                           try
                           {
                               Solid solid = calculator.CalculateSpatialElementGeometry(room).GetGeometry(); //！！！有报错！！！
                               FilteredElementCollector RoomElementCollector = new FilteredElementCollector(Doc).WhereElementIsNotElementType().WherePasses(new ElementIntersectsSolidFilter(solid));
                               //将房间中所有的element中wall过滤出来
                               ElementClassFilter WallFilter = new ElementClassFilter(typeof(Wall));
                               RoomElementCollector = RoomElementCollector.WherePasses(WallFilter);
                               var RoomWallList = RoomElementCollector.ToList();
                               foreach (Wall wall in RoomWallList)
                               {
                                   double length = wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                                   double length_meter = UnitUtils.Convert(length, DisplayUnitType.DUT_DECIMAL_FEET, DisplayUnitType.DUT_METERS);
                                   //to do：还需要减去墙的厚度！！！！
                                   if (length_meter < small_area)
                                   {
                                       //changeBounding = true;
                                       Parameter roomBounding = wall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
                                       try
                                       {
                                           //修改墙的房间边界参数
                                           using (Transaction tran = new Transaction(Doc, "WALL_ATTR_ROOM_BOUNDING set to true"))
                                           {
                                               tran.Start();
                                               roomBounding.Set(0);
                                               tran.Commit();
                                           }
                                       }
                                       catch (Exception ex)
                                       {
                                           TaskDialog.Show("Warning:Room bounding", ex.Message);
                                       }
                                   }
                               }
                           }
                           catch (Exception ex)
                           {
                               TaskDialog.Show("Warning:Solid Original Small Area", room.Id + "\n" + ex.ToString());
                           }

                           //删除面积小于5的房间
                           if (changeBounding)
                           {
                               try
                               {
                                   //进行元素的删除
                                   using (Transaction tran = new Transaction(Doc, "Delete small room."))
                                   {
                                       tran.Start();
                                       ICollection<ElementId> DeletedElements = Doc.Delete(room.Id);
                                       tran.Commit();
                                   }
                               }
                               catch (Exception ex)
                               {
                                   TaskDialog.Show("Original Small Room Delete Warning", ex.Message);
                               }
                           }

                       }
                   }
                   */

/*
           LinkElementId elemid = new LinkElementId(newRoom.Id);
           Location location = newRoom.Location;
           LocationPoint locationPoint = location as LocationPoint;
           XYZ point3d = locationPoint.Point;
           UV point2d = new UV(point3d.X, point3d.Y);
           RoomTag roomTag = doc.Create.NewRoomTag(elemid, point2d, view.Id);
           if (family != null)
           {
               try
               {
                   FamilySymbol symbol = family.Document.GetElement(family.GetFamilySymbolIds().First()) as FamilySymbol;
                   if (symbol != null)
                   {
                       roomTag.ChangeTypeId(symbol.Id);
                   }
               }
               catch { }
           }
           */
/*//进行小房间的删除并修改墙的房间边界属性
     foreach (Room newRoom in newrooms)
     {

         double roomarea = newRoom.Area;    //aera的单位是平方英尺
         double roomarea_sqm = UnitUtils.Convert(roomarea, DisplayUnitType.DUT_SQUARE_FEET, DisplayUnitType.DUT_SQUARE_METERS);
         if (roomarea_sqm == 0)
         {
             try
             {
                 //进行元素的删除
                 using (Transaction tran = new Transaction(Doc, "Delete small room."))
                 {
                     tran.Start();
                     ICollection<ElementId> DeletedElements = Doc.Delete(newRoom.Id);
                     tran.Commit();
                 }
             }
             catch (Exception ex)
             {
                 TaskDialog.Show("Small Room Delete Extra Warning", ex.Message);
             }
         }
         else
         {
             //bool changeBounding = false;
             double small_area = 5;
             if (roomarea_sqm < small_area )
             {
                 SpatialElementBoundaryOptions seb = new SpatialElementBoundaryOptions();
                 seb.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;
                 SpatialElementGeometryCalculator calculator = new SpatialElementGeometryCalculator(Doc, seb);
                 try
                 {
                     Solid solid = calculator.CalculateSpatialElementGeometry(newRoom).GetGeometry(); //！！！有报错！！！
                     FilteredElementCollector RoomElementCollector = new FilteredElementCollector(Doc).WhereElementIsNotElementType().WherePasses(new ElementIntersectsSolidFilter(solid));
                     //将房间中所有的element中wall过滤出来
                     ElementClassFilter WallFilter = new ElementClassFilter(typeof(Wall));
                     RoomElementCollector = RoomElementCollector.WherePasses(WallFilter);
                     RoomWallList = RoomElementCollector.ToList();
                 }
                 catch (Exception ex)
                 {
                     TaskDialog.Show("Warning:Solid Small Area", level.Id.ToString() + "/" + level.Name + "\n" + newRoom.Id + "\n" + ex.ToString());
                 }
                 //修改墙的房间边界参数
                 foreach (Wall wall in RoomWallList)
                 {
                     double length = wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                     double length_meter = UnitUtils.Convert(length, DisplayUnitType.DUT_DECIMAL_FEET, DisplayUnitType.DUT_METERS);
                     if (length_meter < small_area)
                     {
                         //changeBounding = true;
                         Parameter roomBounding = wall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
                         try
                         {
                             using (Transaction tran = new Transaction(Doc, "WALL_ATTR_ROOM_BOUNDING set to true"))
                             {
                                 tran.Start();
                                 roomBounding.Set(0);
                                 tran.Commit();
                             }
                         }
                         catch (Exception ex)
                         {
                             TaskDialog.Show("Warning:Room bounding", ex.Message);
                         }
                     }
                 }
             }
         }
     }
     */
