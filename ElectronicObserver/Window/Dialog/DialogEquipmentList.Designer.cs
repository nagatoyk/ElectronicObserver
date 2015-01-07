﻿namespace ElectronicObserver.Window.Dialog {
	partial class DialogEquipmentList {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing ) {
			if ( disposing && ( components != null ) ) {
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.EquipmentView = new System.Windows.Forms.DataGridView();
			this.EquipmentView_ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.EquipmentView_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.EquipmentView_CountAll = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.EquipmentView_CountRemain = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.EquipmentView_EquippedShip = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this.EquipmentView)).BeginInit();
			this.SuspendLayout();
			// 
			// EquipmentView
			// 
			this.EquipmentView.AllowUserToAddRows = false;
			this.EquipmentView.AllowUserToDeleteRows = false;
			this.EquipmentView.AllowUserToResizeRows = false;
			this.EquipmentView.BackgroundColor = System.Drawing.SystemColors.Control;
			this.EquipmentView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.EquipmentView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.EquipmentView_ID,
            this.EquipmentView_Name,
            this.EquipmentView_CountAll,
            this.EquipmentView_CountRemain,
            this.EquipmentView_EquippedShip});
			this.EquipmentView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.EquipmentView.Location = new System.Drawing.Point(0, 0);
			this.EquipmentView.Name = "EquipmentView";
			this.EquipmentView.ReadOnly = true;
			this.EquipmentView.RowHeadersVisible = false;
			this.EquipmentView.RowTemplate.Height = 21;
			this.EquipmentView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.EquipmentView.Size = new System.Drawing.Size(640, 480);
			this.EquipmentView.TabIndex = 0;
			this.EquipmentView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.EquipmentView_CellFormatting);
			this.EquipmentView.SortCompare += new System.Windows.Forms.DataGridViewSortCompareEventHandler(this.EquipmentView_SortCompare);
			this.EquipmentView.Sorted += new System.EventHandler(this.EquipmentView_Sorted);
			// 
			// EquipmentView_ID
			// 
			this.EquipmentView_ID.HeaderText = "ID";
			this.EquipmentView_ID.Name = "EquipmentView_ID";
			this.EquipmentView_ID.ReadOnly = true;
			this.EquipmentView_ID.Width = 40;
			// 
			// EquipmentView_Name
			// 
			this.EquipmentView_Name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
			this.EquipmentView_Name.HeaderText = "装備名";
			this.EquipmentView_Name.Name = "EquipmentView_Name";
			this.EquipmentView_Name.ReadOnly = true;
			this.EquipmentView_Name.Width = 5;
			// 
			// EquipmentView_CountAll
			// 
			this.EquipmentView_CountAll.HeaderText = "全個数";
			this.EquipmentView_CountAll.Name = "EquipmentView_CountAll";
			this.EquipmentView_CountAll.ReadOnly = true;
			this.EquipmentView_CountAll.Width = 40;
			// 
			// EquipmentView_CountRemain
			// 
			this.EquipmentView_CountRemain.HeaderText = "余個数";
			this.EquipmentView_CountRemain.Name = "EquipmentView_CountRemain";
			this.EquipmentView_CountRemain.ReadOnly = true;
			this.EquipmentView_CountRemain.Width = 40;
			// 
			// EquipmentView_EquippedShip
			// 
			this.EquipmentView_EquippedShip.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.EquipmentView_EquippedShip.HeaderText = "装備艦";
			this.EquipmentView_EquippedShip.Name = "EquipmentView_EquippedShip";
			this.EquipmentView_EquippedShip.ReadOnly = true;
			this.EquipmentView_EquippedShip.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// DialogEquipmentList
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(640, 480);
			this.Controls.Add(this.EquipmentView);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Meiryo UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
			this.Name = "DialogEquipmentList";
			this.Text = "装備一覧";
			this.Load += new System.EventHandler(this.DialogEquipmentList_Load);
			((System.ComponentModel.ISupportInitialize)(this.EquipmentView)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView EquipmentView;
		private System.Windows.Forms.DataGridViewTextBoxColumn EquipmentView_ID;
		private System.Windows.Forms.DataGridViewTextBoxColumn EquipmentView_Name;
		private System.Windows.Forms.DataGridViewTextBoxColumn EquipmentView_CountAll;
		private System.Windows.Forms.DataGridViewTextBoxColumn EquipmentView_CountRemain;
		private System.Windows.Forms.DataGridViewTextBoxColumn EquipmentView_EquippedShip;
	}
}