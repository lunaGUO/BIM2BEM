using System;
using System.Collections.Generic;
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
using Autodesk.Revit.Creation;

namespace AddRoom_Space
{
    class RoomManager
    {
        ExternalCommandData m_commandData;
        Dictionary<int, List<Room >> m_roomDictionary;

        /// <summary>
        /// The constructor of SpaceManager class.
        /// </summary>
        /// <param name="data">The ExternalCommandData</param>
        /// <param name="roomData">The roomData contains all the Room elements in different level.</param>
        public RoomManager(ExternalCommandData data, Dictionary<int, List<Room>> roomData)
        {
            m_commandData = data;
            m_roomDictionary = roomData;
        }

        /// <summary>
        /// Get the Rooms elements in a specified level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns>Return a Room list</returns>
        public List<Room> GetRooms(Level level)
        {
            return m_roomDictionary[level.Id.IntegerValue];
        }

        /// <summary>
        /// Create the room for each closed wall loop or closed room separation in the active view.
        /// </summary>
        /// <param name="level">The level in which the rooms is to exist.</param>
        /// <param name="phase">The phase in which the rooms is to exist.</param>
        public void CreateRooms(Level level, Phase phase)
        {
            try
            {
                ICollection<ElementId> elements = m_commandData.Application.ActiveUIDocument.Document.Create.NewRooms2(level, phase);
                 //m_commandData.Application.ActiveUIDocument.Document.Create.NewZone(level, phase);
                TaskDialog.Show("room","To Create Rooms");
                foreach (ElementId elem in elements)
                {
                    Room room = m_commandData.Application.ActiveUIDocument.Document.GetElement(elem) as Room;
                    if (room != null)
                    {
                        m_roomDictionary[level.Id.IntegerValue].Add(room);
                    }
                }
                if (elements == null || elements.Count == 0)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("Revit", "There is no enclosed loop in " + level.Name);
                }

            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Revit", ex.Message);
            }
        }
    }
}
