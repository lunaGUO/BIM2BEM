using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Architecture;

namespace MergeSmallRoom
{
    public partial class AmendRoomName : System.Windows.Forms.Form
    {
        public AmendRoomName()
        {
            InitializeComponent();
        }

        public AmendRoomName(DataManager dataManager)
        {
            windowData = dataManager;
            InitializeComponent();
        }

        public AmendRoomName(MergeSmallRoom AddData)
        {
            roomData = AddData;
            RoomToChange.Clear();
            InitializeComponent();
        }





        private void Label1_Click(object sender, EventArgs e)
        {

        }

        private void Label2_Click(object sender, EventArgs e)
        {

        }






        private void ChangeNameRoomslistView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void AddRoomButton_Click(object sender, EventArgs e)
        {
            List<Room> AddRoom = new List<Room>();
            foreach (RoomItem roomItem in this.WrongNameRoomslistView.SelectedItems)
            {
                AddRoom.Add(roomItem.Room);

            }
            foreach (Room room in AddRoom)
            {
                RoomToChange.Add(room);
            }
            //RoomToChange = AddRoom.OrderByDescending(s => s.Number).ToList();
            UpdateRoomNameList();
        }

        private void RemoveRoomButton_Click(object sender, EventArgs e)
        {
            List<Room> RemoveRoom = new List<Room>();
            foreach (RoomItem roomItem in this.ChangeNameRoomslistView.SelectedItems)
            {
                RemoveRoom.Add(roomItem.Room);
            }
            foreach (Room room in RemoveRoom)
            {
                RoomToChange.Remove(room);
            }

            UpdateRoomNameList();
        }



        private void RoomNameCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Update(comboBox1.SelectedItem as Level);
        }

        private void AmendRoomName_Load(object sender, EventArgs e)
        {

            this.RoomNameCombox.DataSource = windowData.CorrectRoomNames;
            this.RoomNameCombox.DisplayMember = "Name";
            this.RoomNameCombox.DropDownStyle = ComboBoxStyle.DropDownList;
            UpdateRoomNameList();
        }

        private void OK_button_Click(object sender, EventArgs e)
        {
            foreach (Room room in RoomToChange)
            {
                room.Name = RoomNameCombox.SelectedItem.ToString();
            }
            this.Close();
        }

        private void WrongNameRoomslistView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void UpdateRoomNameList()
        {
            this.WrongNameRoomslistView.Items.Clear();
            this.ChangeNameRoomslistView.Items.Clear();

            // AvailableSpacesListView

            //WrongRoomNames = new Dictionary<string, string>();
            //WrongRoomNames = new Dictionary<Room, string>(roomData.RoomsWithWrongName);
            foreach (Room room in MergeSmallRoom.RoomsWithWrongName.Keys)
            {
                if (RoomToChange.Contains(room) == false)
                {
                    this.WrongNameRoomslistView.Items.Add(new RoomItem(room));
                }
            }

            // CurrentSpacesListView
            foreach (Room room in RoomToChange)
            {
                this.ChangeNameRoomslistView.Items.Add(new RoomItem(room));
            }

            this.WrongNameRoomslistView.Update();
            this.ChangeNameRoomslistView.Update();
        }




    }
}
