using System.Collections.Generic;
using Autodesk.Revit.DB.Architecture;

namespace AddRoom_Space
{
    partial class AmendRoomName
    {
        private DataManager windowData;
        private AddSpaces roomData;
        Dictionary<Room, string> WrongRoomNames;
        List<Room> RoomToChange = new List<Room>();
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.WrongNameRoomslistView = new System.Windows.Forms.ListView();
            this.ChangeNameRoomslistView = new System.Windows.Forms.ListView();
            this.AddRoombutton = new System.Windows.Forms.Button();
            this.RemoveRoombutton = new System.Windows.Forms.Button();
            this.OK_button = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.RoomNameCombox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(39, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "原有房间名称：";
            this.label1.Click += new System.EventHandler(this.Label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(499, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(97, 15);
            this.label2.TabIndex = 1;
            this.label2.Text = "修改的房间：";
            this.label2.Click += new System.EventHandler(this.Label2_Click);
            // 
            // WrongNameRoomslistView
            // 
            this.WrongNameRoomslistView.Location = new System.Drawing.Point(42, 82);
            this.WrongNameRoomslistView.Name = "WrongNameRoomslistView";
            this.WrongNameRoomslistView.Size = new System.Drawing.Size(268, 256);
            this.WrongNameRoomslistView.TabIndex = 2;
            this.WrongNameRoomslistView.UseCompatibleStateImageBehavior = false;
            this.WrongNameRoomslistView.View = System.Windows.Forms.View.List;
            this.WrongNameRoomslistView.SelectedIndexChanged += new System.EventHandler(this.WrongNameRoomslistView_SelectedIndexChanged);
            // 
            // ChangeNameRoomslistView
            // 
            this.ChangeNameRoomslistView.Location = new System.Drawing.Point(502, 82);
            this.ChangeNameRoomslistView.Name = "ChangeNameRoomslistView";
            this.ChangeNameRoomslistView.Size = new System.Drawing.Size(268, 213);
            this.ChangeNameRoomslistView.TabIndex = 2;
            this.ChangeNameRoomslistView.UseCompatibleStateImageBehavior = false;
            this.ChangeNameRoomslistView.View = System.Windows.Forms.View.List;
            this.ChangeNameRoomslistView.SelectedIndexChanged += new System.EventHandler(this.ChangeNameRoomslistView_SelectedIndexChanged);
            // 
            // AddRoombutton
            // 
            this.AddRoombutton.Location = new System.Drawing.Point(350, 146);
            this.AddRoombutton.Name = "AddRoombutton";
            this.AddRoombutton.Size = new System.Drawing.Size(114, 36);
            this.AddRoombutton.TabIndex = 3;
            this.AddRoombutton.Text = "添加房间";
            this.AddRoombutton.UseVisualStyleBackColor = true;
            this.AddRoombutton.Click += new System.EventHandler(this.AddRoomButton_Click);
            // 
            // RemoveRoombutton
            // 
            this.RemoveRoombutton.Location = new System.Drawing.Point(350, 211);
            this.RemoveRoombutton.Name = "RemoveRoombutton";
            this.RemoveRoombutton.Size = new System.Drawing.Size(114, 35);
            this.RemoveRoombutton.TabIndex = 3;
            this.RemoveRoombutton.Text = "移除房间";
            this.RemoveRoombutton.UseVisualStyleBackColor = true;
            this.RemoveRoombutton.Click += new System.EventHandler(this.RemoveRoomButton_Click);
            // 
            // OK_button
            // 
            this.OK_button.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OK_button.Location = new System.Drawing.Point(359, 373);
            this.OK_button.Name = "OK_button";
            this.OK_button.Size = new System.Drawing.Size(91, 26);
            this.OK_button.TabIndex = 4;
            this.OK_button.Text = "&OK";
            this.OK_button.UseVisualStyleBackColor = true;
            this.OK_button.Click += new System.EventHandler(this.OK_button_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(499, 318);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 15);
            this.label3.TabIndex = 1;
            this.label3.Text = "修改为：";
            this.label3.Click += new System.EventHandler(this.Label2_Click);
            // 
            // RoomNameCombox
            // 
            this.RoomNameCombox.FormattingEnabled = true;
            this.RoomNameCombox.Location = new System.Drawing.Point(561, 315);
            this.RoomNameCombox.Name = "RoomNameCombox";
            this.RoomNameCombox.Size = new System.Drawing.Size(209, 23);
            this.RoomNameCombox.TabIndex = 5;
            this.RoomNameCombox.SelectedIndexChanged += new System.EventHandler(this.RoomNameCombox_SelectedIndexChanged);
            // 
            // AmendRoomName
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.RoomNameCombox);
            this.Controls.Add(this.OK_button);
            this.Controls.Add(this.RemoveRoombutton);
            this.Controls.Add(this.AddRoombutton);
            this.Controls.Add(this.ChangeNameRoomslistView);
            this.Controls.Add(this.WrongNameRoomslistView);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "AmendRoomName";
            this.Text = "Amend Room Name";
            this.Load += new System.EventHandler(this.AmendRoomName_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListView WrongNameRoomslistView;
        private System.Windows.Forms.ListView ChangeNameRoomslistView;
        private System.Windows.Forms.Button AddRoombutton;
        private System.Windows.Forms.Button RemoveRoombutton;
        private System.Windows.Forms.Button OK_button;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox RoomNameCombox;
    }
}