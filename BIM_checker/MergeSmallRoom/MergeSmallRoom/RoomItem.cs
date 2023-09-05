using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Architecture;

namespace MergeSmallRoom
{

    class RoomItem : ListViewItem
    {
        Room thisroom;
        public RoomItem(Room room) : base(room.Name)
        {
            thisroom = room;
            base.Text = room.Level.Name + ": " + room.Name;

        }
        public Room Room
        {
            get
            {
                return thisroom;
            }
        }
    }
}
