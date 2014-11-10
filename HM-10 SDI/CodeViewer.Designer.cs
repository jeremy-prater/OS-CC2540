namespace HM_10_SDI
{
    partial class CodeViewer
    {
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
            this.modulelist = new System.Windows.Forms.TreeView();
            this.code_list = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pc_textbox = new System.Windows.Forms.TextBox();
            this.view_addr_textbox = new System.Windows.Forms.TextBox();
            this.goto_addr_button = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.set_view_pc_button = new System.Windows.Forms.Button();
            this.module_title = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // modulelist
            // 
            this.modulelist.Location = new System.Drawing.Point(13, 13);
            this.modulelist.Name = "modulelist";
            this.modulelist.Size = new System.Drawing.Size(368, 484);
            this.modulelist.TabIndex = 0;
            this.modulelist.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.modulelist_AfterSelect);
            // 
            // code_list
            // 
            this.code_list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader3});
            this.code_list.Location = new System.Drawing.Point(387, 29);
            this.code_list.Name = "code_list";
            this.code_list.Size = new System.Drawing.Size(879, 468);
            this.code_list.TabIndex = 1;
            this.code_list.UseCompatibleStateImageBehavior = false;
            this.code_list.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Address";
            this.columnHeader1.Width = 80;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Code";
            this.columnHeader3.Width = 773;
            // 
            // pc_textbox
            // 
            this.pc_textbox.Location = new System.Drawing.Point(387, 504);
            this.pc_textbox.Name = "pc_textbox";
            this.pc_textbox.Size = new System.Drawing.Size(122, 20);
            this.pc_textbox.TabIndex = 2;
            // 
            // view_addr_textbox
            // 
            this.view_addr_textbox.Location = new System.Drawing.Point(548, 504);
            this.view_addr_textbox.Name = "view_addr_textbox";
            this.view_addr_textbox.Size = new System.Drawing.Size(122, 20);
            this.view_addr_textbox.TabIndex = 4;
            // 
            // goto_addr_button
            // 
            this.goto_addr_button.Location = new System.Drawing.Point(676, 502);
            this.goto_addr_button.Name = "goto_addr_button";
            this.goto_addr_button.Size = new System.Drawing.Size(75, 23);
            this.goto_addr_button.TabIndex = 5;
            this.goto_addr_button.Text = "Go to...";
            this.goto_addr_button.UseVisualStyleBackColor = true;
            this.goto_addr_button.Click += new System.EventHandler(this.goto_addr_button_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(295, 507);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Program Counter";
            // 
            // set_view_pc_button
            // 
            this.set_view_pc_button.Location = new System.Drawing.Point(515, 502);
            this.set_view_pc_button.Name = "set_view_pc_button";
            this.set_view_pc_button.Size = new System.Drawing.Size(27, 23);
            this.set_view_pc_button.TabIndex = 7;
            this.set_view_pc_button.Text = "->";
            this.set_view_pc_button.UseVisualStyleBackColor = true;
            this.set_view_pc_button.Click += new System.EventHandler(this.set_view_pc_button_Click);
            // 
            // module_title
            // 
            this.module_title.AutoSize = true;
            this.module_title.Location = new System.Drawing.Point(388, 13);
            this.module_title.Name = "module_title";
            this.module_title.Size = new System.Drawing.Size(88, 13);
            this.module_title.TabIndex = 8;
            this.module_title.Text = "Current Module : ";
            // 
            // CodeViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1278, 534);
            this.Controls.Add(this.module_title);
            this.Controls.Add(this.set_view_pc_button);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.goto_addr_button);
            this.Controls.Add(this.view_addr_textbox);
            this.Controls.Add(this.pc_textbox);
            this.Controls.Add(this.code_list);
            this.Controls.Add(this.modulelist);
            this.Name = "CodeViewer";
            this.Text = "CodeViewer";
            this.Load += new System.EventHandler(this.CodeViewer_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.TreeView modulelist;
        private System.Windows.Forms.ListView code_list;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.Button goto_addr_button;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button set_view_pc_button;
        public System.Windows.Forms.TextBox pc_textbox;
        public System.Windows.Forms.TextBox view_addr_textbox;
        private System.Windows.Forms.Label module_title;
    }
}