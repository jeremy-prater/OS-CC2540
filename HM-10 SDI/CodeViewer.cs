using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;

namespace HM_10_SDI
{
    public partial class CodeViewer : Form
    {
        public OSCC2540 master;
        public UInt32 hm_10_pc;
        public CodeViewer(OSCC2540 pmaster)
        {
            InitializeComponent();
            master = pmaster;
            hm_10_pc = 0;
        }

        private void CodeViewer_Load(object sender, EventArgs e)
        {

        }
        public void UpdateTrees()
        {
            modulelist.Nodes.Clear();
            foreach (HM10DebugModule dmodule in master.DebugModules)
            {
                TreeNode module = new TreeNode(dmodule.name + " (" + dmodule.Functions.Count.ToString() + ")");
                foreach (HM10DebugFunction func in dmodule.Functions)
                {
                    module.Nodes.Add("0x" + func.address.ToString("X8").PadLeft(8, '0') + " " + func.section + " " + func.name).Tag = func;
                }
                module.Tag = dmodule;
                modulelist.Nodes.Add(module);
            }
        }
        public void UpdatePC(UInt32 PC)
        {
            if (PC == 0)
                return;
            hm_10_pc = PC;
            pc_textbox.Text = PC.ToString("X8").PadLeft(8, '0');
            foreach (HM10DebugModule dmodule in master.DebugModules)
            {
                foreach (HM10DebugFunction func in dmodule.Functions)
                {
                    if ((PC >= func.address) && (PC <= func.MaxAddress))
                    {
                        foreach (TreeNode pnode in modulelist.Nodes)
                        {
                            foreach (TreeNode fnode in pnode.Nodes)
                            {
                                if (fnode.Tag == func)
                                    modulelist.SelectedNode = fnode;
                            }
                        }
                    }
                }
            }
            ViewAddress(PC);
        }
        public void ViewAddress(UInt32 address)
        {
            Int32 selectedindex = 0;
            Int32 itemcounter = 0;
            UInt32 temp= 0;
            foreach (ListViewItem item in code_list.Items)
            {
                if (item.Text.Contains ("0x") == false)
                    continue;
                if (Convert.ToUInt32(item.Text, 16) == address)
                {
                    selectedindex = itemcounter;
                    break;
                }
                itemcounter++;
            }

            if (selectedindex != 0)
            {
                code_list.EnsureVisible(selectedindex);
            }
        }
        private void modulelist_AfterSelect(object sender, TreeViewEventArgs e)
        {
            HM10DebugFunction tfunc = new HM10DebugFunction(null, 0, "test", "test");
            if (e.Node.Tag.GetType() == tfunc.GetType())
            {
                module_title.Text = "Current Module : " + ((HM10DebugFunction)e.Node.Tag).name + " (0x" + ((HM10DebugFunction)e.Node.Tag).address.ToString("X8").PadLeft(8, '0') + " - 0x" + ((HM10DebugFunction)e.Node.Tag).MaxAddress.ToString("X8").PadLeft(8, '0') + ") in " + ((HM10DebugFunction)e.Node.Tag).parentmodule.name;
                code_list.Items.Clear();
                bool hicolor = true;
                HM10SourceFile file = ((HM10DebugFunction)e.Node.Tag).sourcefile;
                if (file != null)
                {
                    foreach (HM10SourceLine line in file.SourceLines)
                    {
                        ListViewItem newitem = new ListViewItem("0x" + line.absoluteaddress.ToString("X8").PadLeft(8, '0'));
                        newitem.SubItems.Add(line.linetext);

                        if (line.ASMLine)
                        {
                            if (hicolor)
                                newitem.BackColor = Color.FromArgb(255, 144, 180, 144);
                            else
                                newitem.BackColor = Color.FromArgb(255, 144, 220, 144);
                        }
                        else
                        {
                            if (hicolor)
                                newitem.BackColor = Color.FromArgb(255, 144, 144, 144);
                            else
                                newitem.BackColor = Color.FromArgb(255, 220, 220, 220);
                        }
                        hicolor = !hicolor;
                        code_list.Items.Add(newitem);
                    }
                    ViewAddress(((HM10DebugFunction)e.Node.Tag).address);
                }
                else
                    code_list.Items.Add("Source Not Availble");
            }
        }

        private void set_view_pc_button_Click(object sender, EventArgs e)
        {
            view_addr_textbox.Text = pc_textbox.Text;
        }

        private void goto_addr_button_Click(object sender, EventArgs e)
        {
            UInt32 addr = Convert.ToUInt32(view_addr_textbox.Text, 16);
            UpdatePC(addr);
            ViewAddress(addr);
        }
    }
}
