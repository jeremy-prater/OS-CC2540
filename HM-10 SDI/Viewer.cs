using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HM_10_SDI
{
    public partial class Viewer : Form
    {
        public static IEnumerable<Color> GetGradients(Color start, Color end, int steps)
        {
            int stepA = ((end.A - start.A) / (steps - 1));
            int stepR = ((end.R - start.R) / (steps - 1));
            int stepG = ((end.G - start.G) / (steps - 1));
            int stepB = ((end.B - start.B) / (steps - 1));

            for (int i = 0; i < steps; i++)
            {
                yield return Color.FromArgb(start.A + (stepA * i),
                                            start.R + (stepR * i),
                                            start.G + (stepG * i),
                                            start.B + (stepB * i));
            }
        }
        public Viewer(byte [] data)
        {
            InitializeComponent();
            IEnumerable<Color> rgGrad = GetGradients (Color.Red, Color.Green, 128);
            IEnumerable<Color> gbGrad = GetGradients(Color.Green, Color.Blue, 128);
            Color bgColor;
            for (int addr = 0; addr < data.Length; addr +=16)
            {
                ListViewItem row = listView1.Items.Add(addr.ToString("X5"));
                row.UseItemStyleForSubItems = false;
                for (int d=0;d<16;d++)
                {
                    ListViewItem.ListViewSubItem item = row.SubItems.Add(data[addr + d].ToString ("X2"));
                    if (data[addr + d] < 128)
                        bgColor = rgGrad.ElementAt(data[addr + d]);
                    else
                        bgColor = gbGrad.ElementAt((data[addr + d] - 128));
                    item.BackColor = bgColor;
                }
                byte[] temp = new byte[1];
                for (int d = 0; d < 16; d++)
                {
                    temp[0] = data[addr + d];
                    row.SubItems.Add(Encoding.UTF8.GetString(temp));
                }
            }
        }

        private void Viewer_Load(object sender, EventArgs e)
        {

        }
    }
}
