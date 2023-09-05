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

namespace MergeSmallRoom
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
        //定义一个错误处理类
        public class MyFailuresPreprocessor : IFailuresPreprocessor
        {
            private string _failureMessage;
            private bool _hasError;
            public string FailureMessage
            {
                get { return _failureMessage; }
                set { _failureMessage = value; }
            }
            public bool HasError
            {
                get { return _hasError; }
                set { _hasError = value; }
            }
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                //获取所有的失败信息
                IList<FailureMessageAccessor> failures = failuresAccessor.GetFailureMessages();
                if (failures.Count == 0)
                    return FailureProcessingResult.Continue;

                foreach (FailureMessageAccessor failure in failures)
                {
                    //如果是错误则尝试解决
                    if (failure.GetSeverity() == FailureSeverity.Error)
                    {
                        _failureMessage = failure.GetDescriptionText(); // get the failure description                
                        _hasError = true;
                        TaskDialog.Show("错误警告", "FailureProcessingResult.ProceedWithRollBack");
                        return FailureProcessingResult.ProceedWithRollBack;
                    }
                    //如果是警告，则禁止弹框
                    if (failure.GetSeverity() == FailureSeverity.Warning)
                    {
                        failuresAccessor.DeleteWarning(failure);
                    }
                }
                return FailureProcessingResult.Continue;
            }
        }
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class MergeSmallRoom : IExternalCommand
    {

        public static Dictionary<Room, string> RoomsWithWrongName = new Dictionary<Room, string>();
        public Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document Doc = revit.Application.ActiveUIDocument.Document;

            //进行模型解组
            FilteredElementCollector collector = new FilteredElementCollector(Doc);
            ICollection<Element> collection = collector.OfClass(typeof(Group)).ToElements();
            foreach (Element el in collection)
            {
                try
                {
                    using (Transaction tran = new Transaction(Doc, "Translate RoomName to English"))
                    {

                        tran.Start();
                        Group g = el as Group;
                        g.UngroupMembers();
                        tran.Commit();
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("ChangeRoomNames Waring", ex.ToString());
                    return Result.Failed;
                }
                
            }

            //进行房间功能判断
            string[] roomnames = { "办公室", "会议室", "走廊", "消防前室", "楼梯间", "卫生间", "空调机房", "不设空调的房间", "其他不可穿管的房间", "空调水管井", "排风井", "排烟井", "新风井", "加压风井", "风井", "强电间", "弱电间", "消防电梯", "电梯" };
            string[] roomEnglishNames = { "office", "meeting", "corridor", "fire_front_room", "staircase", "toilet", "AC_plant", "NoAC", "NoDuct", "KTSJ", "PF", "PY", "XF", "JY", "FJ", "QD", "RD", "XDT", "DT" };
            List<string> correctNames = new List<string>();
            List<string> RoomNames = new List<string>(roomnames);
            List<string> RoomEnglishNames = new List<string>(roomEnglishNames);
            Dictionary<string, string> roomNamesDictionary = new Dictionary<string, string>();
            for (int i = 0; i < RoomNames.Count; i++)
            {
                roomNamesDictionary.Add(RoomNames[i], RoomEnglishNames[i]);
            }
            for (int i = 0; i < roomnames.Length; i++)
            {
                correctNames.Add(roomnames[i]);
            }
            for (int i = 0; i < roomEnglishNames.Length; i++)
            {
                correctNames.Add(roomEnglishNames[i]);
            }

            ICollection<ElementId> roomsOriginal = (new FilteredElementCollector(Doc)).WherePasses(new RoomFilter()).WhereElementIsNotElementType().ToElementIds();
            //判断房间名称是否正确
            foreach (ElementId id in roomsOriginal)
            {
                Room room = Doc.GetElement(id) as Room;
                string RoomNumber = " " + room.Number;
                string OnlyName = room.Name.Replace(RoomNumber, "");
                if (! (RoomNames.Contains(OnlyName) || RoomEnglishNames.Contains(OnlyName)))
                {
                    RoomsWithWrongName.Add(room, room.Name);
                }
            }

            if (RoomsWithWrongName.Count ==0)
            {
                TaskDialog.Show("prompt","房间名称标准");
            }

            //将房间名字修改为英文函数
            void ChangeRoomName(string OnlyName, Room room)
            {
                try
                {
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

            //修改房间名字
            while (RoomsWithWrongName.Count != 0)
            {

                TaskDialog.Show("Prompt", "房间名字不规范，请修改房间名字");
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
                }

                catch (Exception ex)
                {
                    // If there are something wrong, give error information and return failed
                    message = ex.Message;
                    TaskDialog.Show("catch", message);
                    return Autodesk.Revit.UI.Result.Failed;
                }
                RoomsWithWrongName.Clear();
                foreach (ElementId id in roomsOriginal)
                {
                    Room eachRoom = Doc.GetElement(id) as Room;
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
                if (RoomsWithWrongName.Count == 0)
                {
                    TaskDialog.Show("Prompt", "房间名字修改完成");
                }
            }
            
            
            foreach(ElementId id in roomsOriginal)
            {
                Room eachRoom = Doc.GetElement(id) as Room;
                string RoomNumber = " " + eachRoom.Number;
                string OnlyName = eachRoom.Name.Replace(RoomNumber, "");
                //TaskDialog.Show("test1", OnlyName );
                //TaskDialog.Show("test2", RoomNames[0]);
                ChangeRoomName(OnlyName, eachRoom);

            }


            //删除原本就多余的房间
            for (int i = 0; i < roomsOriginal.Count; i++)
            {
                Room room = Doc.GetElement(roomsOriginal.ElementAt(i)) as Room;
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

            ICollection<ElementId> roomsExist = (new FilteredElementCollector(Doc)).WherePasses(new RoomFilter()).WhereElementIsNotElementType().ToElementIds();

            //进行小空间的边界属性更改
            //先判断小空间房间
            int count_bounding = 0;
            int count_smallarea = 0;
            int count_change = 0;
            string MergeRooms = "合并的房间为："; 
            double small_area = 7;
            List<Room> smallrooms = new List<Room>();
            List<Room> bigrooms = new List<Room>();
            string[] roomretain = { "office", "meeting", "corridor", "fire_front_room", "staircase", "toilet", "AC_plant", "NoAC", "NoDuct" };
            List<string> RoomRetain = new List<string>(roomretain);
            for (int i = 0; i < roomsExist.Count; i++)
            {
                Room room = Doc.GetElement(roomsExist.ElementAt(i)) as Room;
                double area = room.Area;    //aera的单位是平方英尺
                double area_sqm = UnitUtils.Convert(area, DisplayUnitType.DUT_SQUARE_FEET, DisplayUnitType.DUT_SQUARE_METERS);
                //bool changeBounding = false;
                string RoomNumber = " " + room.Number;
                string OnlyName = room.Name.Replace(RoomNumber, "");
                if ((0 < area_sqm & area_sqm <= small_area) || !(RoomRetain.Contains(OnlyName)))
                {
                    count_smallarea++;
                    smallrooms.Add(room);
                }
                else
                {
                    bigrooms.Add(room);
                }
            }
            
            List<ElementId> BigRoomWallsId = new List<ElementId>();
            foreach (Room bigroom in bigrooms)
            {
                SpatialElementBoundaryOptions seb = new SpatialElementBoundaryOptions();
                seb.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;
                SpatialElementGeometryCalculator calculator = new SpatialElementGeometryCalculator(Doc, seb);
                try
                {
                    Solid solid = calculator.CalculateSpatialElementGeometry(bigroom).GetGeometry(); //！！！有报错！！！
                    FilteredElementCollector RoomElementCollector = new FilteredElementCollector(Doc).WhereElementIsNotElementType().WherePasses(new ElementIntersectsSolidFilter(solid));
                    //将房间中所有的element中wall过滤出来
                    ElementClassFilter WallFilter = new ElementClassFilter(typeof(Wall));
                    RoomElementCollector = RoomElementCollector.WherePasses(WallFilter);
                    var BigRoomWallList = RoomElementCollector.ToList();
                    foreach (Wall bigwall in BigRoomWallList)
                    {
                        BigRoomWallsId.Add(bigwall.Id);
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Warning:Bigroom solid", ex.Message);
                }
            }
            for (int i = 0; i < smallrooms.Count; i++)
            {
                Room room = smallrooms[i];
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
                    //找出每个房间中是房间边界的墙
                    List<Wall> RoomBWallList = new List<Wall>();
                    foreach (Wall wall in RoomWallList)  //todo 可优化，有重复的wall，可直接跳过
                    {
                        if (wall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING).AsInteger() ==1)
                        {
                            RoomBWallList.Add(wall);
                        }
                    }
                    int RoomBWallAmount = RoomBWallList.Count;
                    List<Wall> BacktotheForm = new List<Wall>();
                    foreach (Wall wall in RoomBWallList)  //todo 可优化，有重复的wall，可直接跳过
                    {
                        /*
                        double length = wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                        double length_meter = UnitUtils.Convert(length, DisplayUnitType.DUT_DECIMAL_FEET, DisplayUnitType.DUT_METERS);
                        */
                        if (!BigRoomWallsId.Contains(wall.Id))
                        {
                            //changeBounding = true;
                            Parameter roomBounding = wall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
                            try
                            {
                                //修改墙的房间边界参数
                                using (Transaction tran = new Transaction(Doc, "WALL_ATTR_ROOM_BOUNDING set to true"))
                                {
                                    tran.Start();
                                    FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                                    MyFailuresPreprocessor failureProcessor = new MyFailuresPreprocessor();
                                    options.SetFailuresPreprocessor(failureProcessor);
                                    tran.SetFailureHandlingOptions(options);
                                    //更改房间边界属性
                                    roomBounding.Set(0);
                                    count_bounding++;
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
                                TaskDialog.Show("Warning:Room bounding", ex.Message);
                                return Autodesk.Revit.UI.Result.Failed;
                            }
                            /*
                            if (changeBounding >= 2)
                            {
                                break;
                            }
                            */
                        }
                        else
                        {
                            BacktotheForm.Add(wall);
                        }
                    }
                    if (BacktotheForm.Count == RoomBWallAmount)//回型结构的房间，todo 回型房间内不能有是房间边界的隔墙
                    {
                        //TaskDialog.Show("test",room.Name+" 被某一房间全部包围");
                        if(BacktotheForm.Count != 0)
                        { 
                            ElementId MinWallId = BacktotheForm[0].Id;
                            double MinLength = BacktotheForm[0].get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                            foreach (Wall Iwall in BacktotheForm)
                            {
                                double length = Iwall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                                if (length < MinLength)
                                {
                                    MinLength = length;
                                    MinWallId = Iwall.Id;
                                }
                            }
                            try
                            {
                                Wall MinWall = Doc.GetElement(MinWallId) as Wall;
                                Parameter roomBounding = MinWall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
                                //修改被回形房间包围的边界参数
                                using (Transaction tran = new Transaction(Doc, "WALL_ATTR_ROOM_BOUNDING set to true"))
                                {
                                    tran.Start();
                                    FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                                    MyFailuresPreprocessor failureProcessor = new MyFailuresPreprocessor();
                                    options.SetFailuresPreprocessor(failureProcessor);
                                    tran.SetFailureHandlingOptions(options);
                                    //更改房间边界属性
                                    roomBounding.Set(0);
                                    count_bounding++;
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
                                TaskDialog.Show("Warning:Room bounding", ex.Message);
                                return Autodesk.Revit.UI.Result.Failed;
                            }
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Warning:Solid Original Small Area", room.Id + "\n" + ex.ToString());
                }
            }


            //删除小房间
            foreach (Room Sroom in smallrooms)
            {
                //进行元素的删除
                using (Transaction tran = new Transaction(Doc, "Delete extra room."))
                {
                    try
                    {
                        tran.Start();
                        FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                        MyFailuresPreprocessor failureProcessor = new MyFailuresPreprocessor();
                        options.SetFailuresPreprocessor(failureProcessor);
                        tran.SetFailureHandlingOptions(options);
                        string SroomName = Sroom.Name;
                        string SroomId = Sroom.Id.ToString();
                        string SroomLevel = Sroom.Level.Name;
                        ICollection<ElementId> DeletedElements = Doc.Delete(Sroom.Id);
                        count_change++;
                        MergeRooms += "\n" + SroomId + "/" + SroomLevel + "/" + SroomName;
                        //FailuresAccessor.DeleteAllWarnings;
                        //tran.Commit();
                        var status = tran.Commit();
                        if (status != TransactionStatus.Committed)
                        {
                            if (failureProcessor.HasError)
                            {
                                TaskDialog.Show("ERROR", failureProcessor.FailureMessage);
                            }
                        }
                        //continue;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Extra Room Delete Warning", ex.Message);
                    }
                }
            }
            int count_OverMerge = 0;
            string OverMerge = "存在过度合并的房间，过度合并的房间信息如下：\n";
            //过度合并的房间删除
            ICollection<ElementId> roomsAfMer = (new FilteredElementCollector(Doc)).WherePasses(new RoomFilter()).WhereElementIsNotElementType().ToElementIds();
            foreach (ElementId roomId in roomsAfMer)
            {
                Room Oroom = Doc.GetElement(roomId) as Room;
                if(Oroom.Area == 0)
                {
                    //进行元素的删除
                    using (Transaction tran = new Transaction(Doc, "Delete extra room."))
                    {
                        try
                        {
                            tran.Start();
                            FailureHandlingOptions options = tran.GetFailureHandlingOptions();
                            MyFailuresPreprocessor failureProcessor = new MyFailuresPreprocessor();
                            options.SetFailuresPreprocessor(failureProcessor);
                            tran.SetFailureHandlingOptions(options);
                            string OroomName = Oroom.Name;
                            string OroomId = Oroom.Id.ToString();
                            string OroomLevel = Oroom.Level.Name;
                            ICollection<ElementId> DeletedElements = Doc.Delete(Oroom.Id);
                            count_change++;
                            count_OverMerge++;
                            MergeRooms += "\n" + OroomId + "/" + OroomLevel + "/" + OroomName;
                            OverMerge += OroomId + "/" + OroomLevel + "/" + OroomName;
                            //FailuresAccessor.DeleteAllWarnings;
                            //tran.Commit();
                            var status = tran.Commit();
                            if (status != TransactionStatus.Committed)
                            {
                                if (failureProcessor.HasError)
                                {
                                    TaskDialog.Show("ERROR", failureProcessor.FailureMessage);
                                }
                            }
                            //continue;
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Extra Room Delete Warning", ex.Message);
                        }
                    }
                }
            }
            if (count_OverMerge == 0)
            {
                TaskDialog.Show("合并信息：\n" + "Succeed Prompt", "小面积房间个数为：" + count_smallarea + "\n修改房间边界的墙个数为：" + count_bounding + "\n合并的房间个数为" + count_change + "\n合并的房间如下，请检查合并后的房间的名称是否正确\n" + MergeRooms);
            }
            else
            {
                TaskDialog.Show("合并信息：\n" + "Succeed Prompt", "小面积房间个数为：" + count_smallarea + "\n修改房间边界的墙个数为：" + count_bounding + "\n合并的房间个数为" + count_change + "\n合并的房间如下，请检查合并后的房间的名称是否正确\n" + MergeRooms + "\n" + OverMerge);
            }
            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}
