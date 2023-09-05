using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace AddRoom_Space
{
    public class DataManager
    {
       
        ExternalCommandData DocData;

        public DataManager(ExternalCommandData commandData)
        {
            DocData = commandData;
            Document Doc = commandData.Application.ActiveUIDocument.Document;
            List<Level> AllLevels = new List<Level>();
            // RoomManager Add_roomManager;
            Parameter para = commandData.Application.ActiveUIDocument.Document.ActiveView.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.VIEW_PHASE);
            ElementId phaseId = para.AsElementId();
            Phase defaultPhase = commandData.Application.ActiveUIDocument.Document.GetElement(phaseId) as Phase;

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



            }








        








        //roomdata
        Dictionary<int, List<Room>> roomDictionary = new Dictionary<int, List<Room>>();
        
        string[] roomnames = { "办公室", "会议室","走廊", "消防前室", "楼梯间", "卫生间", "空调机房", "不设空调的房间", "其他不可穿管的房间", "空调水管井", "排风井", "排烟井", "新风井", "加压风井", "风井", "强电间", "弱电间", "消防电梯", "电梯" };







        public Array CorrectRoomNames
        {
            get
            {
                return roomnames.ToArray();
            }
        }

       

    }
}
