using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Linq;

namespace HM_10_SDI
{
    public partial class OSCC2540 : Form
    {
        const ushort DUP_DBGDATA = 0x6260;  // Debug interface data buffer
        const ushort DUP_FCTL = 0x6270;  // Flash controller
        const ushort DUP_FADDRL = 0x6271;  // Flash controller addr
        const ushort DUP_FADDRH = 0x6272;  // Flash controller addr
        const ushort DUP_FWDATA = 0x6273;  // Clash controller data buffer
        const ushort DUP_CLKCONSTA = 0x709E;  // Sys clock status
        const ushort DUP_CLKCONCMD = 0x70C6;  // Sys clock configuration
        const ushort DUP_MEMCTR = 0x70C7;  // Flash bank xdata mapping
        const ushort DUP_DMA1CFGL = 0x70D2;  // Low byte, DMA config ch. 1
        const ushort DUP_DMA1CFGH = 0x70D3;  // Hi byte , DMA config ch. 1
        const ushort DUP_DMA0CFGL = 0x70D4;  // Low byte, DMA config ch. 0
        const ushort DUP_DMA0CFGH = 0x70D5;  // Low byte, DMA config ch. 0
        const ushort DUP_DMAARM = 0x70D6;  // DMA arming register

        public bool connected;
        public bool force_flash;

        public UInt32 DataRateOut;
        public UInt32 DataRateIn;
        public UInt32 DataTotalOut;
        public UInt32 DatatotalIn;

        public byte cc_status;
        public byte cc_config;
        public byte cc_pc_low;
        public byte cc_pc_high;
        public byte cc_acc;
        public byte cc_chipid;
        public byte cc_chipver;
        public byte cc_bm;

        public byte[] flash_0_data;
        public byte[] flash_1_data;
        public byte[] atmega_rom;
        public byte[] atmega_eeprom;

        public IList<HM10DebugModule> DebugModules;
        public IList<HM10SourceFile> SourceFiles;

        CodeViewer cViewer;

        public byte LOBYTE(ushort val)
        {
            return BitConverter.GetBytes(val)[0];
        }
        public byte HIBYTE(ushort val)
        {
            return BitConverter.GetBytes(val)[1];
        }
        public OSCC2540()
        {
            InitializeComponent();
            connected = false;
            force_flash = false;
            flash_0_data = new byte[0x00040000];
            flash_1_data = new byte[0x00040000];
            atmega_rom = new byte[0x00008000];
            atmega_eeprom = new byte[0x00000400];

            clear_buffer(ref flash_0_data);
            clear_buffer(ref flash_1_data);
            clear_buffer(ref atmega_eeprom);
            clear_buffer(ref atmega_rom);

            DebugModules = new List<HM10DebugModule>();
            SourceFiles = new List<HM10SourceFile>();

            cViewer = new CodeViewer(this);

            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Set Rx Gain").ToArray()[0].Tag = new BLE_Command_HCI_Set_Rx_Gain(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Set Tx Power").ToArray()[0].Tag = new BLE_Command_HCI_Set_Tx_Power(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "One Packet Per Event").ToArray()[0].Tag = new BLE_Command_HCI_One_Packet_Per_Event(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Clock Divide On Halt").ToArray()[0].Tag = new BLE_Command_HCI_Clock_Divide_On_Halt(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Declare NV Usage").ToArray()[0].Tag = new BLE_Command_HCI_Clock_Declare_NV_Usage(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Decrypt").ToArray()[0].Tag = new BLE_Command_HCI_Clock_Decrypt(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Set Local Supported Features").ToArray()[0].Tag = new BLE_Command_HCI_Set_Local_Supported(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Set Fast Tx Response Time").ToArray()[0].Tag = new BLE_Command_HCI_Set_Fast_Tx_Response_Time(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Modem Test Tx").ToArray()[0].Tag = new BLE_Command_HCI_Modem_Test_Transmit(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Modem Hop Test Tx").ToArray()[0].Tag = new BLE_Command_HCI_Modem_Test_Transmit_Hop(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Modem Test Rx").ToArray()[0].Tag = new BLE_Command_HCI_Modem_Test_Receive(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "End Modem Test").ToArray()[0].Tag = new BLE_Command_HCI_Modem_Test_End(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Set BDADDR").ToArray()[0].Tag = new BLE_Command_HCI_Set_BD_Addr(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Set SCA").ToArray()[0].Tag = new BLE_Command_HCI_Set_SCA(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Enable PTM").ToArray()[0].Tag = new BLE_Command_HCI_Enable_PTM(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Set Frequency Tuning").ToArray()[0].Tag = new BLE_Command_HCI_Set_Freq_Tune(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Save Frequency Tuning").ToArray()[0].Tag = new BLE_Command_HCI_Save_Freq_Tune(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Set Max DTM Rx Power").ToArray()[0].Tag = new BLE_Command_HCI_Set_Max_DTM_Rx_Power(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Map PM IO Port").ToArray()[0].Tag = new BLE_Command_HCI_Map_PM_IO_Port(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Disconnect Immediate").ToArray()[0].Tag = new BLE_Command_HCI_Disconnect_Immediate(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Packet Error Rate").ToArray()[0].Tag = new BLE_Command_HCI_Packet_Error_Rate(this);

            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Packet Error Rate By Channel").ToArray()[0].Tag = new BLE_Command_HCI_Packet_Error_Rate_By_Channel(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Extend RF Range").ToArray()[0].Tag = new BLE_Command_HCI_Extend_RF_Range(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Advertiser Event Notice").ToArray()[0].Tag = new BLE_Command_HCI_Advertiser_Event_Notice(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Connection Event Notice").ToArray()[0].Tag = new BLE_Command_HCI_Connection_Event_Notice(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Halt During RF").ToArray()[0].Tag = new BLE_Command_HCI_Halt_During_RF(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Set Slave Latency Override").ToArray()[0].Tag = new BLE_Command_HCI_Set_Slave_Latency_Override(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Build Revision").ToArray()[0].Tag = new BLE_Command_HCI_Build_Revision(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Delay Sleep").ToArray()[0].Tag = new BLE_Command_HCI_Delay_Sleep(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Reset System").ToArray()[0].Tag = new BLE_Command_HCI_Reset_System(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Overlapped Processing").ToArray()[0].Tag = new BLE_Command_HCI_Overlapped_Processing_Command(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Number Completed Packets Limit").ToArray()[0].Tag = new BLE_Command_HCI_Number_Completed_Packets_Limit(this);

            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Device Initialization").ToArray()[0].Tag = new BLE_Command_GAP_Device_Init(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Configure Device Address").ToArray()[0].Tag = new BLE_Command_GAP_Configure_Device_Address(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Device Discovery Request").ToArray()[0].Tag = new BLE_Command_GAP_Device_Discovery_Request(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Device Discovery Cancel").ToArray()[0].Tag = new BLE_Command_GAP_Device_Discovery_Cancel(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Make Discoverable").ToArray()[0].Tag = new BLE_Command_GAP_Make_Discoverable(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Update Advertising Data").ToArray()[0].Tag = new BLE_Command_GAP_Update_Advertising_Data(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "End Discoverable").ToArray()[0].Tag = new BLE_Command_GAP_End_Discoverable(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Establish Link Request").ToArray()[0].Tag = new BLE_Command_GAP_Establish_Link_Request(this);
            treeView1.Nodes.FlattenTree().Where(r => r.Text == "Terminate Link Request").ToArray()[0].Tag = new BLE_Command_GAP_Terminate_Link_Request(this);

            ChangeDescriptionHeight(BLE_COMMAND_PROP, 300);
        }
        public void AddLogString(string str)
        {
            logbox.AppendText(str + "\r\n");

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            AddLogString("OS CC2540 Debugger Interface V1.0");
            AddLogString("====================================\r\n");
            AddLogString("Using Baud Rate : " + serialPort1.BaudRate.ToString() + ", " + serialPort1.DataBits.ToString() + " Databits, " + serialPort1.Parity.ToString() + " Parity, " + serialPort1.StopBits.ToString() + " Stop bit(s).");
            AddLogString("Enumerating Com Ports");

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                comports.Items.Add(port);
                AddLogString(port);
            }
            if (comports.Items.Count > 0)
            {
                connectbutton.Enabled = true;
                comports.SelectedIndex = 0;
                AddLogString("Ready to Connect.");
            }
            else
            {
                AddLogString("No Com ports found, check your hardware");
            }
        }

        private void connectbutton_Click(object sender, EventArgs e)
        {
            if (!connected)
            {
                Connect();
            }
            else
            {
                Disconnect();
            }
        }
        public void Connect()
        {
            serialPort1.PortName = comports.Text;
            AddLogString("Connecting to " + serialPort1.PortName + "...");
            connectstatus.Text = "Connecting...";
            connectstatus.BackColor = Color.Orange;
            connectstatus.Refresh();
            serialPort1.Open();
            serialPort1.DiscardInBuffer();
            SendPacket(new SerialPacket(0x0001, null));
            SerialPacket packet = GetPacket();
            if (packet != null)
            {
                connected = true;
                connectbutton.Text = "Disconnect";
                connectstatus.Text = "Connected";
                connectstatus.BackColor = Color.Green;
                //connectimage.Visible = true;
                hm_10_reset_debug_button.Enabled = true;
                AddLogString("Connected to client"); // Get data from client and display
            }
            else
            {
                connectbutton.Text = "Connect";
                connectstatus.Text = "Disconnected";
                connectstatus.BackColor = Color.Red;
                serialPort1.Close();
                comchecktimer.Enabled = false;
                connected = false;
                AddLogString("Client did not respond. Disconnected."); // Get data from client and display

            }
        }
        public void Disconnect()
        {
            //connectimage.Visible = false;
            connectbutton.Text = "Connect";
            connectstatus.Text = "Disconnected";
            hm_10_reset_debug_button.Enabled = false;
            connectstatus.BackColor = Color.Red;
            serialPort1.Close();
            comchecktimer.Enabled = false;
            connected = false;
            AddLogString("Disconnected"); // Get data from client and display

        }
        public void UpdateCCInfo()
        {
            SetLabelStat(cc_status & 0x01, stat_0);
            SetLabelStat(cc_status & 0x02, stat_1);
            SetLabelStat(cc_status & 0x04, stat_2);
            SetLabelStat(cc_status & 0x08, stat_3);
            SetLabelStat(cc_status & 0x10, stat_4);
            SetLabelStat(cc_status & 0x20, stat_5);
            SetLabelStat(cc_status & 0x40, stat_6);
            SetLabelStat(cc_status & 0x80, stat_7);

            SetLabelStat(cc_config & 0x02, config_1);
            SetLabelStat(cc_config & 0x04, config_2);
            SetLabelStat(cc_config & 0x08, config_3);
            SetLabelStat(cc_config & 0x20, config_5);

            cViewer.UpdatePC(Convert.ToUInt32((cc_pc_high.ToString("X2") + cc_pc_low.ToString("X2")), 16));
            cpu_pc.Text = "0x" + cc_pc_high.ToString("X2") + cc_pc_low.ToString("X2");
            cpu_pc.Text = "0x" + cc_pc_high.ToString("X2") + cc_pc_low.ToString("X2");
            cpu_acc.Text = "0x" + cc_acc.ToString("X2");
            cpu_bm.Text = "0x" + cc_bm.ToString("X2");

        }
        public void SetLabelStat(int value, Label target)
        {
            if (value != 0)
                target.BackColor = Color.Green;
            else
                target.BackColor = Color.Red;
        }
        public void SendHM10Command(List<object> data)
        {
            UInt16 length = 0;
            foreach (object d in data)
            {
                if (d is byte)
                    length += 1;
                if (d is UInt16)
                    length += 2;
                if (d is UInt32)
                    length += 4;
                if (d is UInt64)
                    length += 8;
                if (d is byte[])
                {
                    byte[] r = (byte[])d;
                    length += (ushort)r.Length;
                }
            }
            data.Insert(4, (byte)(length - 4));
            byte[] outdata = new byte[length + 1];
            UInt16 datapos = 0;
            foreach (object d in data)
            {
                byte[] tempdata = new byte[1];
                if (d is byte)
                    tempdata[0] = (byte)d;
                if (d is UInt16)
                    tempdata = BitConverter.GetBytes((UInt16)d);
                if (d is UInt32)
                    tempdata = BitConverter.GetBytes((UInt32)d);
                if (d is UInt64)
                    tempdata = BitConverter.GetBytes((UInt64)d);
                if (d is byte[])
                    tempdata = (byte[])d;
                Array.Copy(tempdata, 0, outdata, datapos, tempdata.Length);
                datapos += (UInt16)tempdata.Length;
            }
            SerialPacket pkt = new SerialPacket(0x3010, outdata);
            SendPacket(pkt);
            string mstr = "Sending BLE Command : ";
            foreach (byte bd in pkt.payload)
            {
                mstr += String.Format("0x{0:X2} ", bd);
            }
            AddLogString(mstr);
        }
        public bool SendPacket(SerialPacket packet)
        {
            if (serialPort1.IsOpen)
            {
                int length = (int)BitConverter.ToInt16(packet.payload, 0);
                DataRateOut += (uint)length;
                DataTotalOut += (uint)length;
                serialPort1.Write(packet.payload, 0, length);

                packetresponsetimer.Tag = packet;
                packetresponsetimer.Start();

                /*while (!packet.ResponeRecv)
                {
                    Application.DoEvents();
                    if (serialPort1.BytesToRead > 0)
                    {
                        serialPort1.ReadByte();
                        packet.ResponeRecv = true;
                        packetresponsetimer.Stop();
                    }
                    if (packet.ResponseTimeout)
                        return false;
                }*/

                /*len = 1;
                byte[] data = new byte[len];
                serialPort1.Read(data, 0, len);
                byte result = 0;
                if (len == 1)
                    result = data[0];*/
                //if (result == 0xAA)
                //{
                /*if (packet.payload[2] == 0x02)
                    delayMs(250);
                if (packet.payload[2] == 0x03) // Debug command, wait for response
                {
                    SerialPacket rpacket = GetPacket();
                    if (rpacket.payload[2] == 0x04) // Reply section
                    {
                        byte cmdtmp = (byte)(packet.payload[0x03] & 0xF8);

                        if ((cmdtmp == 0x20) || (cmdtmp == 0x18)) // RD_CONFIG | WR_CONFIG
                        {
                            cc_config = rpacket.payload[0x03];
                            UpdateCCInfo();
                        }
                        if ((cmdtmp == 0x10) || (cmdtmp == 0x30) || (cmdtmp == 0x38) || (cmdtmp == 0x40) || (cmdtmp == 0x48) || (cmdtmp == 0x80)) // RD_STATUS | CHIP_ERASE | SET_HW_BRKPNT | HALT | RESUME |BURST
                        {
                            if ((cmdtmp != 0x40) & ((cc_status & 0x20) == 0)) // If cmd is halt and CPU is halted, cc_status is undefined.
                            {
                                cc_status = rpacket.payload[0x03];
                                UpdateCCInfo();
                            }
                        }
                        if (cmdtmp == 0x58) // STEP_INST
                        {
                            cpu_acc.Text = "0x" + rpacket.payload[0x03].ToString("X2");
                            cpu_data_acc = rpacket.payload[0x03];
                            update_hm_10_status();
                        }
                        if (cmdtmp == 0x50) // DEBUG_INST
                        {
                            cpu_acc.Text = "0x" + rpacket.payload[0x03].ToString("X2");
                            cpu_data_acc = rpacket.payload[0x03];
                        }
                        if (cmdtmp == 0x40) // HALT
                        {
                            update_hm_10_status();
                        }

                        if (cmdtmp == 0x28) // GET_PC
                        {
                            cpu_pc.Text = "0x" + rpacket.payload[0x03].ToString("X2") + rpacket.payload[0x04].ToString("X2");
                        }
                        if (cmdtmp == 0x60) // GET_BM
                        {
                            cpu_bm.Text = "0x" + rpacket.payload[0x03].ToString("X2");
                        }
                        if (cmdtmp == 0x68) // GET_CHIP_ID
                        {
                            AddLogString("CC2540 : CHIPID=0x" + rpacket.payload[0x03].ToString("X2") + ", CHVER=0x" + rpacket.payload[0x04].ToString("X2"));
                        }
                    }
                }*/
                return true;
                //}
            }
            AddLogString("Not Connected to target.");
            Disconnect();
            return false;
        }
        public SerialPacket GetPacket()
        {
            SerialPacket packet = null;
            if (serialPort1.IsOpen)
            {
                while (serialPort1.BytesToRead < 2) Application.DoEvents();
                byte[] data = new byte[2];
                byte[] res = new byte[1];
                data[0] = (byte)serialPort1.ReadByte();
                data[1] = (byte)serialPort1.ReadByte();

                ushort datalen = BitConverter.ToUInt16(data, 0);
                while (serialPort1.BytesToRead < datalen - 2) ;

                data = new byte[datalen];
                data[0] = BitConverter.GetBytes(datalen)[0];
                data[1] = BitConverter.GetBytes(datalen)[1];
                serialPort1.Read(data, 2, datalen - 2);

                packet = new SerialPacket(data);
                DataRateIn += (uint)datalen;
                DatatotalIn += (uint)datalen;
            }
            return packet;
        }
        public void update_hm_10_status()
        {
            SendPacket(new SerialPacket(0x3FFF, null));
            SerialPacket packet = GetPacket();

            cc_status = packet.payload[0x04];
            cc_config = packet.payload[0x05];
            cc_pc_low = packet.payload[0x07];
            cc_pc_high = packet.payload[0x06];
            cc_acc = packet.payload[0x08];
            cc_chipid = packet.payload[0x09];
            cc_chipver = packet.payload[0x0A];
            cc_bm = packet.payload[0x0B];
            UpdateCCInfo();
        }
        private void packetresponsetimer_Tick(object sender, EventArgs e)
        {
            SerialPacket packet = (SerialPacket)packetresponsetimer.Tag;
            if (packet != null)
                packet.ResponseTimeout = true;
            packetresponsetimer.Stop();
        }
        public static DateTime delayMs(int MilliSecondsToPauseFor)
        {
            System.DateTime ThisMoment = System.DateTime.Now;
            System.TimeSpan duration = new System.TimeSpan(0, 0, 0, 0, MilliSecondsToPauseFor);
            System.DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                ThisMoment = System.DateTime.Now;
            }

            return System.DateTime.Now;
        }
        private void resetdebug_Click(object sender, EventArgs e)
        {
            AddLogString("Resetting CC 2540/2541 to Debug...");
            SendPacket(new SerialPacket(0x3F01, null)); // Reset to debug
            SendPacket(new SerialPacket(0x3FA0, null)); // Enable 32 MHz XOSC
            SendPacket(new SerialPacket(0x3F68, null)); // GET CHIPID
            SendPacket(new SerialPacket(0x3F20, null)); // RD_CONFIG
            SendPacket(new SerialPacket(0x3F28, null)); // GET_PC
            SendPacket(new SerialPacket(0x3F60, null)); // GET_BM
            SendPacket(new SerialPacket(0x3F30, null)); // RD_STATUS
            delayMs(200);
            update_hm_10_status();
            AddLogString("  ... CHIPID  : 0x" + cc_chipid.ToString("X2"));
            AddLogString("  ... CHIPVER : 0x" + cc_chipver.ToString("X2"));
            if (cc_chipid == 0x41)
                AddLogString("  ... CC 2541 Detected!");
            if (cc_chipid == 0x8D)
                AddLogString("  ... CC 2540 Detected!");
            Application.DoEvents();
            hm_10_debug_timer.Enabled = true;
        }
        private void cpu_resume_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x3F48, null)); // RESUME
            update_hm_10_status();
        }
        private void cpu_step_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x3F58, null)); // Step Instr
            SendPacket(new SerialPacket(0x3F28, null)); // Get PC
            update_hm_10_status();
        }
        private void cpu_halt_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x3F40, null)); // Halt
            SendPacket(new SerialPacket(0x3F28, null)); // Get PC
            update_hm_10_status();
        }
        private void read_flash_0_button_Click(object sender, EventArgs e)
        {
            read_spi_flash(ref flash_0_data, 0);
        }
        private void read_flash_1_button_Click(object sender, EventArgs e)
        {
            read_spi_flash(ref flash_1_data, 1);
        }
        private void write_flash_0_button_Click(object sender, EventArgs e)
        {
            write_spi_flash(flash_0_data, 0);
        }
        private void write_flash_1_button_Click(object sender, EventArgs e)
        {
            write_spi_flash(flash_1_data, 1);
        }
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
        public byte[] GetATMegaStatus()
        {
            IEnumerable<Color> rgGrad = GetGradients(Color.Red, Color.Green, 128);
            IEnumerable<Color> gbGrad = GetGradients(Color.Green, Color.Blue, 128);
            Color bgColor;

            //SendPacket(new SerialPacket((ushort)0x3EFD, null)); // Read Buffer

            SendPacket(new SerialPacket((ushort)0x0002, null)); // Read Buffer
            SerialPacket dpacket = GetPacket();
            byte[] data = new byte[7];
            data[0] = dpacket.payload[0x04]; // AT
            data[1] = dpacket.payload[0x05]; // Flash 0
            data[2] = dpacket.payload[0x06]; // Flash 1
            data[3] = dpacket.payload[0x07]; // HM-10 Status
            data[4] = dpacket.payload[0x08]; // HM-10 Messages
            data[5] = dpacket.payload[0x09]; // HM-10 Message Length Low
            data[6] = dpacket.payload[0x0A]; // HM-10 Message Length High


            SR1.Text = "0x" + data[0].ToString("X2");
            SR2.Text = "0x" + data[1].ToString("X2");
            SR3.Text = "0x" + data[2].ToString("X2");
            SR4.Text = "0x" + data[3].ToString("X2");
            SR5.Text = "0x" + data[4].ToString("X2");
            SR6.Text = "0x" + data[6].ToString("X2") + data[5].ToString("X2");

            if (data[0] < 128)
                bgColor = rgGrad.ElementAt(data[0]);
            else
                bgColor = gbGrad.ElementAt((data[0] - 128));
            SR1.BackColor = bgColor;

            if (data[1] < 128)
                bgColor = rgGrad.ElementAt(data[1]);
            else
                bgColor = gbGrad.ElementAt((data[1] - 128));
            SR2.BackColor = bgColor;

            if (data[2] < 128)
                bgColor = rgGrad.ElementAt(data[2]);
            else
                bgColor = gbGrad.ElementAt((data[2] - 128));
            SR3.BackColor = bgColor;

            if (data[3] < 128)
                bgColor = rgGrad.ElementAt(data[3]);
            else
                bgColor = gbGrad.ElementAt((data[3] - 128));
            SR4.BackColor = bgColor;

            if (data[4] < 128)
                bgColor = rgGrad.ElementAt(data[4]);
            else
                bgColor = gbGrad.ElementAt((data[4] - 128));
            SR5.BackColor = bgColor;


            SR1.Refresh();
            SR2.Refresh();
            SR3.Refresh();
            SR4.Refresh();
            SR5.Refresh();
            SR6.Refresh();

            return data;
        }
        public void WaitForFlashStatus(byte SR, int flashid)
        {
            if (flashid == 0)
                WaitForFLASH0Status(SR);
            if (flashid == 1)
                WaitForFLASH1Status(SR);
            if (flashid == 2)
                WaitForHM10Status(SR);
        }
        public void WaitForATMegaStatus(byte SR4)
        {
            while (GetATMegaStatus()[0] != SR4) ;
        }
        public void WaitForFLASH0Status(byte SR2)
        {
            while (GetATMegaStatus()[1] != SR2) ;
        }
        public void WaitForFLASH1Status(byte SR3)
        {
            while (GetATMegaStatus()[2] != SR3) ;
        }
        public void WaitForHM10Status(byte SR4)
        {
            while (GetATMegaStatus()[3] != SR4) ;
        }
        public void read_spi_flash(ref byte[] data, int flashid)
        {
            AddLogString("Reading Flash " + flashid.ToString() + ".");
            SerialPacket dpacket = null;
            UInt16 offset = 0;
            if (flashid == 0)
                offset = 0x1000;
            if (flashid == 1)
                offset = 0x2000;

            const short flash_send_size = 0x100; // 256-byte page
            flash_progress.Maximum = 0x00040000;
            flash_progress.Value = 0;
            for (int t = 0; t < 0x00040000; t += flash_send_size)
            {
                SendPacket(new SerialPacket((ushort)(offset + 0x0003), new byte[] { BitConverter.GetBytes(t)[2], BitConverter.GetBytes(t)[1], BitConverter.GetBytes(t)[0], flash_send_size & 0xFF, flash_send_size >> 8 })); // Read Data
                WaitForFlashStatus(1 << 7, flashid); // Wait for Buffer to be ready
                SendPacket(new SerialPacket((ushort)(offset + 0x0FFF), new byte[] { flash_send_size & 0xFF, flash_send_size >> 8 })); // Read Buffer
                dpacket = GetPacket();
                for (int s = 0; s < flash_send_size; s++)
                {
                    if (flashid == 0)
                        flash_0_data[t + s] = dpacket.payload[4 + s];
                    if (flashid == 1)
                        flash_1_data[t + s] = dpacket.payload[4 + s];
                }

                flash_progress.Value = t + flash_send_size;
                flash_progress_label.Text = "0x" + (t + flash_send_size).ToString("X8") + " / 0x00040000 (" + ((float)((t + flash_send_size) * 100) / 0x00040000).ToString("F2") + "%)";
                flash_progress_label.Refresh();
                flash_progress.Refresh();
            }
            AddLogString("Flash Read Complete.");
        }
        public void write_spi_flash(byte[] data, int flashid)
        {
            AddLogString("Writing Flash " + flashid.ToString() + ".");
            UInt16 offset = 0;
            if (flashid == 0)
                offset = 0x1000;
            if (flashid == 1)
                offset = 0x2000;

            const int flash_send_size = 0x100; // 256-byte page
            flash_progress.Maximum = 0x00040000;
            flash_progress.Value = 0;
            byte[] outdata = new byte[flash_send_size + 3];
            bool pageempty;
            for (int t = 0; t < 0x00040000; t += flash_send_size)
            {
                pageempty = true;
                for (ushort taddress = 0; taddress < flash_send_size; taddress++)
                {
                    if (flashid == 0)
                    {
                        if ((flash_0_data[(taddress + t)] != 0xFF))
                            pageempty = false;
                    }
                    else
                    {
                        if ((flash_1_data[(taddress + t)] != 0xFF))
                            pageempty = false;
                    }

                }
                if (pageempty == false)
                {
                    outdata[0] = BitConverter.GetBytes(t)[2];
                    outdata[1] = BitConverter.GetBytes(t)[1];
                    outdata[2] = BitConverter.GetBytes(t)[0];

                    for (int r = 0; r < flash_send_size; r++)
                    {
                        if (flashid == 0)
                            outdata[3 + r] = flash_0_data[t + r];
                        if (flashid == 1)
                            outdata[3 + r] = flash_1_data[t + r];
                    }

                    SendPacket(new SerialPacket((ushort)(offset + 0x0008), outdata)); // Write Data
                    WaitForFlashStatus(0, flashid);
                }
                flash_progress.Value = t + flash_send_size;
                flash_progress_label.Text = "0x" + (t + flash_send_size).ToString("X8") + " / 0x00040000 (" + ((float)((t + flash_send_size) * 100) / 0x00040000).ToString("F2") + "%)";
                flash_progress_label.Refresh();
                flash_progress.Refresh();

            }
            AddLogString("Flash Write Complete.");
        }
        public void verify_spi_flash(byte[] data, int flashid)
        {
            bool flash_ok = true;

            AddLogString("Verifying Flash " + flashid.ToString() + " to stored buffer.");

            SerialPacket dpacket = null;
            UInt16 offset = 0;
            if (flashid == 0)
                offset = 0x1000;
            if (flashid == 1)
                offset = 0x2000;

            const int flash_send_size = 0x40; // 64-byte page
            flash_progress.Maximum = 0x00040000;
            flash_progress.Value = 0;
            for (int t = 0; t < 0x00040000; t += flash_send_size)
            {
                SendPacket(new SerialPacket((ushort)(offset + 0x0003), new byte[] { BitConverter.GetBytes(t)[2], BitConverter.GetBytes(t)[1], BitConverter.GetBytes(t)[0], flash_send_size & 0xFF, flash_send_size >> 8 })); // Read Data
                WaitForFlashStatus(1 << 7, flashid); // Wait for Buffer to be ready
                SendPacket(new SerialPacket((ushort)(offset + 0x0FFF), new byte[] { flash_send_size & 0xFF, flash_send_size >> 8 })); // Read Buffer
                dpacket = GetPacket();
                for (int s = 0; s < flash_send_size; s++)
                {
                    if (flashid == 0)
                        if (flash_0_data[t + s] != dpacket.payload[4 + s])
                        {
                            flash_ok = false;
                            AddLogString("Expected 0x" + flash_0_data[t + s].ToString("X2") + ", Recieved 0x" + dpacket.payload[0x04 + s].ToString("X2") + " @ 0x" + (t + s).ToString("X8"));
                            return;
                        }
                    if (flashid == 1)
                        if (flash_1_data[t + s] != dpacket.payload[4 + s])
                        {
                            flash_ok = false;
                            AddLogString("Expected 0x" + flash_1_data[t + s].ToString("X2") + ", Recieved 0x" + dpacket.payload[0x04 + s].ToString("X2") + " @ 0x" + (t + s).ToString("X8"));
                            return;
                        }
                }

                flash_progress.Value = t + flash_send_size;
                flash_progress_label.Text = "0x" + (t + flash_send_size).ToString("X8") + " / 0x00040000 (" + ((float)((t + flash_send_size) * 100) / 0x00040000).ToString("F2") + "%)";
                flash_progress_label.Refresh();
                flash_progress.Refresh();
            }
            if (flash_ok)
                AddLogString("Flash Verified OK");
            else
                AddLogString("Flash Verification Error");
        }
        private void verify_flash_0_button_Click(object sender, EventArgs e)
        {
            verify_spi_flash(flash_0_data, 0);
        }
        private void verify_flash_1_button_Click(object sender, EventArgs e)
        {
            verify_spi_flash(flash_1_data, 1);
        }
        private void hm_10_erase_click(object sender, EventArgs e)
        {
            if (force_flash == false)
            {
                if (MessageBox.Show("This will erase the entire CC 2540/2541 ROM. Are you sure?", "Confirm Chip Erase", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    SendPacket(new SerialPacket(0x3F01, null)); // Reset to debug
                    delayMs(200);
                    SendPacket(new SerialPacket(0x3F10, null)); // Chip Erase
                    update_hm_10_status();
                    while ((cc_status & 0x80) == 0x80)
                    {
                        SendPacket(new SerialPacket(0x3F30, null)); // Get Status
                        update_hm_10_status();
                    }
                    AddLogString("CC 2540/2541 Erased!");
                }
            }
            else
            {
                SendPacket(new SerialPacket(0x3F01, null)); // Reset to debug
                delayMs(200);
                SendPacket(new SerialPacket(0x3F10, null)); // Chip Erase
                update_hm_10_status();
                while ((cc_status & 0x80) == 0x80)
                {
                    SendPacket(new SerialPacket(0x3F30, null)); // Get Status
                    update_hm_10_status();
                }
                AddLogString("CC 2540/2541 Erased!");
            }
        }
        private void flash_0_write_hex_Click(object sender, EventArgs e)
        {
            read_hex_file(ref flash_0_data);
        }
        public void read_hex_file(ref byte[] data)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                byte[] dline = new byte[64];
                for (int t = 0; t < data.Length; t++)
                    data[t] = 0xFF;
                AddLogString("Opening HEX Firmware " + openFileDialog2.FileName);
                StreamReader infile = new StreamReader(new FileStream(openFileDialog2.FileName, FileMode.Open));
                IList<string> lines = new List<string>();
                while (!infile.EndOfStream)
                {
                    lines.Add(infile.ReadLine());
                }
                UInt32 extaddr = 0;
                UInt32 startaddr = 0;
                foreach (string line in lines)
                {
                    int pos = 0;
                    for (pos = 0; pos < 4; )
                    {
                        dline[pos] = Convert.ToByte(line.Substring((pos++ * 2) + 1, 2), 16);
                    }
                    for (byte t = 0; t < dline[0]; t++)
                    {
                        dline[pos] = Convert.ToByte(line.Substring((pos++ * 2) + 1, 2), 16);
                    }
                    dline[pos] = Convert.ToByte(line.Substring((pos++ * 2) + 1, 2), 16);
                    byte checksum = 0;
                    for (int t = 0; t < pos - 1; t++)
                    {
                        checksum += dline[t];
                    }
                    checksum ^= 0xFF;
                    checksum++;
                    if (checksum != dline[pos - 1])
                    {
                        infile.Close();
                        AddLogString("Parsing Error in HEX file");
                        return;
                    }
                    UInt32 addr = (UInt32)extaddr + (UInt32)BitConverter.ToUInt16(EndianConvert(dline, 1), 0);
                    switch (dline[3])
                    {

                        case 0x00:
                            for (byte t = 0; t < dline[0]; t++)
                            {
                                if (addr + t >= data.Length)
                                {
                                    infile.Close();
                                    AddLogString("HEX Address exceeded flash size.");
                                    return;
                                }
                                data[addr + t] = dline[4 + t];
                            }
                            break;
                        case 0x01:
                            AddLogString("End of HEX file processed.");
                            break;
                        case 0x02:
                            AddLogString("Extended Segment Address Record");
                            break;
                        case 0x04:
                            if (dline[0] == 0x02)
                            {
                                extaddr = (UInt32)BitConverter.ToUInt16(EndianConvert(dline, 4), 0) * 0x10000;
                            }
                            else
                            {
                                AddLogString("Incorrect Length for Extended Linear Addressing");
                            }
                            break;
                        case 0x05:
                            startaddr = BitConverter.ToUInt32(EndianConvert32(dline, 4), 0);
                            AddLogString("Start Linear Address Record : 0x" + startaddr.ToString("X8"));
                            break;
                    }
                }
                infile.Close();
            }
        }
        private void flash_0_save_button_Click(object sender, EventArgs e)
        {
            save_file(1);
        }
        private void flash_0_open_button_Click(object sender, EventArgs e)
        {
            open_file(1);
        }
        private void flash_1_open_button_Click(object sender, EventArgs e)
        {
            open_file(2);
        }
        private void flash_1_save_button_Click(object sender, EventArgs e)
        {
            save_file(2);
        }
        public byte[] EndianConvert(byte[] src, int offset)
        {
            byte[] ret = new byte[2];
            ret[0] = src[offset + 1];
            ret[1] = src[offset + 0];
            return ret;
        }
        public byte[] EndianConvert32(byte[] src, int offset)
        {
            byte[] ret = new byte[4];
            ret[0] = src[offset + 3];
            ret[1] = src[offset + 2];
            ret[2] = src[offset + 1];
            ret[3] = src[offset + 0];
            return ret;
        }
        private void atmega_reset_low_button_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x4000, null)); // Reset Low
        }
        private void atmega_reset_high_button_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x4001, null)); // Reset High
        }
        private void atmega_reset_debug_button_Click(object sender, EventArgs e)
        {
            byte fuse, fusehigh, extfuse, lck, cal;
            AddLogString("Entering ATMega Debug Mode...");
            SendPacket(new SerialPacket(0x0900, null)); // Enter Program ATMega mode
            SendPacket(new SerialPacket(0x0924, null)); // Read Signature Bytes
            SerialPacket packet = GetPacket();
            AddLogString("ATMega Signature Bytes : 0x" + packet.payload[0x04].ToString("X2") + " 0x" + packet.payload[0x05].ToString("X2") + " 0x" + packet.payload[0x06].ToString("X2"));

            SendPacket(new SerialPacket(0x0923, null)); // Read Lock Bits
            packet = GetPacket();
            lck = packet.payload[0x04];
            SendPacket(new SerialPacket(0x0925, null)); // Read Fuse Bits
            packet = GetPacket();
            fuse = packet.payload[0x04];
            SendPacket(new SerialPacket(0x0926, null)); // Read Fuse High Bits
            packet = GetPacket();
            fusehigh = packet.payload[0x04];
            SendPacket(new SerialPacket(0x0927, null)); // Read Ext Fuse Bits
            packet = GetPacket();
            extfuse = packet.payload[0x04];
            SendPacket(new SerialPacket(0x0927, null)); // Read Cal Byte
            packet = GetPacket();
            cal = packet.payload[0x04];

            AddLogString("ATMega Fuse/Lock Configuration :");
            AddLogString(" ...        Lock Bits : " + Convert.ToString(lck, 2).PadLeft(8, '0'));
            AddLogString(" ...        Fuse Bits : " + Convert.ToString(fuse, 2).PadLeft(8, '0'));
            AddLogString(" ...   Fuse High Bits : " + Convert.ToString(fusehigh, 2).PadLeft(8, '0'));
            AddLogString(" ...    Ext Fuse Bits : " + Convert.ToString(extfuse, 2).PadLeft(8, '0'));
            AddLogString(" ... Calibration Byte : " + Convert.ToString(cal, 2).PadLeft(8, '0'));

            atmega_read_lock_button_Click(sender, e);
        }
        private void atmega_reset_button_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x4000, null)); // Reset Low
            delayMs(250);
            SendPacket(new SerialPacket(0x4001, null)); // Reset High
            delayMs(250);
            AddLogString("ATMega Reset");
        }
        public void WaitForATMegaRdyBsy()
        {
            bool ready = false;
            SerialPacket packet;
            while (!ready)
            {
                delayMs(50);
                SendPacket(new SerialPacket(0x0902, null));
                packet = GetPacket();
                if ((packet.payload[0x04] & 0x01) == 0)
                    ready = true;
            }
        }
        private void atmega_erase_button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will erase the entire ATMega ROM. Are you sure?", "Confirm Chip Erase", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                AddLogString("Erasing ATMega 328...");
                SendPacket(new SerialPacket(0x0901, null));
                WaitForATMegaRdyBsy();
                AddLogString("ATMega 328 Erase Complete.");
                atmega_read_lock_button_Click(sender, e);
            }
        }
        private void read_atmega_button_Click(object sender, EventArgs e)
        {
            AddLogString("Reading ATMega ROM");
            flash_progress.Value = 0;
            flash_progress.Maximum = 0x4000;
            for (ushort address = 0; address < 0x4000; address++)
            {

                SendPacket(new SerialPacket(0x920, new byte[] { (byte)(address >> 8), (byte)(address & 0xFF) }));
                SerialPacket packet = GetPacket();
                atmega_rom[(address * 2) + 1] = packet.payload[0x04];

                SendPacket(new SerialPacket(0x921, new byte[] { (byte)(address >> 8), (byte)(address & 0xFF) }));
                packet = GetPacket();
                atmega_rom[(address * 2) + 0] = packet.payload[0x04];
                flash_progress.Value = address + 1;
                flash_progress.Refresh();
                flash_progress_label.Text = "0x" + (address + 1).ToString("X8") + " / 0x00004000 (" + ((float)((address + 1) * 100) / 0x4000).ToString("F2") + "%)";
                flash_progress_label.Refresh();
            }
            AddLogString("Read Complete.");
        }
        private void write_atmega_button_Click(object sender, EventArgs e)
        {
            AddLogString("Writing ATMega ROM");
            bool pageempty = true;
            if (atmega_erase_check.Checked == true)
            {
                atmega_erase_button_Click(sender, e);
            }
            flash_progress.Value = 0;
            flash_progress.Maximum = 0x4000;
            for (ushort address = 0; address < 0x4000; address += 0x00000040)
            {
                pageempty = true;
                for (ushort taddress = 0; taddress < 0x00000040; taddress++)
                {
                    if (
                        (atmega_rom[((taddress + address) * 2) + 0] != 0xFF) ||
                        (atmega_rom[((taddress + address) * 2) + 1] != 0xFF)
                        )
                        pageempty = false;
                }
                if (!pageempty)
                {
                    for (ushort taddress = 0; taddress < 0x00000040; taddress++)
                    {
                        SendPacket(new SerialPacket(0x0912, new byte[] { (byte)(taddress), atmega_rom[((taddress + address) * 2) + 0] })); // Low byte first
                        SendPacket(new SerialPacket(0x0911, new byte[] { (byte)(taddress), atmega_rom[((taddress + address) * 2) + 1] })); // High byte second
                        flash_progress.Value = address + 0x40;
                        flash_progress.Refresh();
                        flash_progress_label.Text = "0x" + (taddress + address + 1).ToString("X8") + " / 0x00004000 (" + ((float)((taddress + address + 1) * 100) / 0x4000).ToString("F2") + "%)";
                        flash_progress_label.Refresh();
                    }
                    SendPacket(new SerialPacket(0x0930, new byte[] { (byte)(address >> 8), (byte)(address & 0xFF) }));
                    WaitForATMegaRdyBsy();
                }
                else
                {
                    flash_progress.Value = address + 0x40;
                    flash_progress.Refresh();
                    flash_progress_label.Text = "0x" + (0x40 + address).ToString("X8") + " / 0x00004000 (" + ((float)((0x40 + address) * 100) / 0x4000).ToString("F2") + "%)";
                    flash_progress_label.Refresh();
                }
            }
            AddLogString("Writing Complete.");
        }
        private void read_atmega_eeprom_button_Click(object sender, EventArgs e)
        {
            AddLogString("Reading ATMega EEPROM.");
            flash_progress.Value = 0;
            flash_progress.Maximum = 0x400;
            for (ushort address = 0; address < 0x400; address++)
            {

                SendPacket(new SerialPacket(0x922, new byte[] { (byte)(address >> 8), (byte)(address & 0xFF) }));
                SerialPacket packet = GetPacket();
                atmega_eeprom[address] = packet.payload[0x04];
                flash_progress.Value = address + 1;
                flash_progress.Refresh();
                flash_progress_label.Text = "0x" + (address + 1).ToString("X8") + " / 0x00000400 (" + ((float)((address + 1) * 100) / 0x400).ToString("F2") + "%)";
                flash_progress_label.Refresh();
            }
            AddLogString("Read Complete.");
        }
        private void view_eeprom_button_Click(object sender, EventArgs e)
        {
            Viewer viewer = new Viewer(atmega_eeprom);
            viewer.ShowDialog(this);
        }
        private void view_atmega_button_Click(object sender, EventArgs e)
        {
            Viewer viewer = new Viewer(atmega_rom);
            viewer.ShowDialog(this);
        }
        private void atmega_write_hex_button_Click(object sender, EventArgs e)
        {
            read_hex_file(ref atmega_rom);
        }
        public void open_file(int index)
        {
            int length = 0;
            string text = "";
            if (index == 1)
            {
                text = "Opening FLASH 0 Image ";
                length = 0x00040000;
            }
            if (index == 2)
            {
                text = "Opening FLASH 1 Image ";
                length = 0x00040000;
            }
            if (index == 3)
            {
                text = "Opening ATMega 328P Firmware ";
                length = 0x00004000;
            }
            if (index == 4)
            {
                text = "Opening ATMega 328 EEPROM ";
                length = 0x00000400;
            }
            openFileDialog1.FilterIndex = index;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                AddLogString(text + openFileDialog1.FileName);
                BinaryReader infile = new BinaryReader(new FileStream(openFileDialog1.FileName, FileMode.Open));
                if (index == 1) infile.Read(flash_0_data, 0, length);
                if (index == 2) infile.Read(flash_1_data, 0, length);
                if (index == 3) infile.Read(atmega_rom, 0, length);
                if (index == 4) infile.Read(atmega_eeprom, 0, length);
                infile.Close();
            }
        }
        public void save_file(int index)
        {
            saveFileDialog1.FilterIndex = index;
            string text = "";
            if (index == 1) text = "Saving FLASH 0 Image to ";
            if (index == 2) text = "Saving FLASH 1 Image to ";
            if (index == 3) text = "Saving ATMega 328P Firmware to ";
            if (index == 4) text = "Saving ATMega 328 EEPROM to ";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                AddLogString(text + saveFileDialog1.FileName);
                BinaryWriter outfile = new BinaryWriter(new FileStream(saveFileDialog1.FileName, FileMode.Create));
                if (index == 1) outfile.Write(flash_0_data);
                if (index == 2) outfile.Write(flash_1_data);
                if (index == 3) outfile.Write(atmega_rom);
                if (index == 4) outfile.Write(atmega_eeprom);
                outfile.Close();
            }
        }
        private void open_atmega_button_Click(object sender, EventArgs e)
        {
            open_file(3);
        }
        private void save_atmega_button_Click(object sender, EventArgs e)
        {
            save_file(3);
        }
        private void save_atmega_eeprom_button_Click(object sender, EventArgs e)
        {
            save_file(4);
        }
        private void open_atmega_eeprom_button_Click(object sender, EventArgs e)
        {
            open_file(4);
        }
        private void view_flash_0_button_Click(object sender, EventArgs e)
        {
            Viewer viewer = new Viewer(flash_0_data);
            viewer.ShowDialog(this);
        }
        private void view_flash_1_button_Click(object sender, EventArgs e)
        {
            Viewer viewer = new Viewer(flash_1_data);
            viewer.ShowDialog(this);
        }
        private void atmega_read_lock_button_Click(object sender, EventArgs e)
        {
            AddLogString("Reading ATMega Fuse/Lock Bytes");
            byte fuse, fusehigh, extfuse, lck;
            SendPacket(new SerialPacket(0x0923, null)); // Read Lock Bits
            SerialPacket packet = GetPacket();
            lck = packet.payload[0x04];
            SendPacket(new SerialPacket(0x0925, null)); // Read Fuse Bits
            packet = GetPacket();
            fuse = packet.payload[0x04];
            SendPacket(new SerialPacket(0x0926, null)); // Read Fuse High Bits
            packet = GetPacket();
            fusehigh = packet.payload[0x04];
            SendPacket(new SerialPacket(0x0927, null)); // Read Ext Fuse Bits
            packet = GetPacket();
            extfuse = packet.payload[0x04];

            lock_5_check.Checked = ((lck & (1 << 5)) == 0 ? false : true);
            lock_4_check.Checked = ((lck & (1 << 4)) == 0 ? false : true);
            lock_3_check.Checked = ((lck & (1 << 3)) == 0 ? false : true);
            lock_2_check.Checked = ((lck & (1 << 2)) == 0 ? false : true);
            lock_1_check.Checked = ((lck & (1 << 1)) == 0 ? false : true);
            lock_0_check.Checked = ((lck & (1 << 0)) == 0 ? false : true);

            fusehigh_7_check.Checked = ((fusehigh & (1 << 7)) == 0 ? false : true);
            fusehigh_6_check.Checked = ((fusehigh & (1 << 6)) == 0 ? false : true);
            fusehigh_5_check.Checked = ((fusehigh & (1 << 5)) == 0 ? false : true);
            fusehigh_4_check.Checked = ((fusehigh & (1 << 4)) == 0 ? false : true);
            fusehigh_3_check.Checked = ((fusehigh & (1 << 3)) == 0 ? false : true);
            fusehigh_2_check.Checked = ((fusehigh & (1 << 2)) == 0 ? false : true);
            fusehigh_1_check.Checked = ((fusehigh & (1 << 1)) == 0 ? false : true);
            fusehigh_0_check.Checked = ((fusehigh & (1 << 0)) == 0 ? false : true);

            fuselow_7_check.Checked = ((fuse & (1 << 7)) == 0 ? false : true);
            fuselow_6_check.Checked = ((fuse & (1 << 6)) == 0 ? false : true);
            fuselow_5_check.Checked = ((fuse & (1 << 5)) == 0 ? false : true);
            fuselow_4_check.Checked = ((fuse & (1 << 4)) == 0 ? false : true);
            fuselow_3_check.Checked = ((fuse & (1 << 3)) == 0 ? false : true);
            fuselow_2_check.Checked = ((fuse & (1 << 2)) == 0 ? false : true);
            fuselow_1_check.Checked = ((fuse & (1 << 1)) == 0 ? false : true);
            fuselow_0_check.Checked = ((fuse & (1 << 0)) == 0 ? false : true);

            extfuse_2_check.Checked = ((extfuse & (1 << 2)) == 0 ? false : true);
            extfuse_1_check.Checked = ((extfuse & (1 << 1)) == 0 ? false : true);
            extfuse_0_check.Checked = ((extfuse & (1 << 0)) == 0 ? false : true);
        }
        private void atmega_write_lock_button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Writing incorrect fuse values can permanently disable the ATMega328! Are you sure you want to write these values?", "Write Fuse/Lock Values?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
            {
                AddLogString("Writing ATMega Fuse/Lock Bytes.");
                byte fuse, fusehigh, extfuse, lck;
                fuse = fusehigh = extfuse = lck = 0;
                lck |= (byte)(lock_5_check.Checked == true ? 1 << 5 : 0);
                lck |= (byte)(lock_4_check.Checked == true ? 1 << 4 : 0);
                lck |= (byte)(lock_3_check.Checked == true ? 1 << 3 : 0);
                lck |= (byte)(lock_2_check.Checked == true ? 1 << 2 : 0);
                lck |= (byte)(lock_1_check.Checked == true ? 1 << 1 : 0);
                lck |= (byte)(lock_0_check.Checked == true ? 1 << 0 : 0);

                fusehigh |= (byte)(fusehigh_7_check.Checked == true ? 1 << 7 : 0);
                fusehigh |= (byte)(fusehigh_6_check.Checked == true ? 1 << 6 : 0);
                fusehigh |= (byte)(fusehigh_5_check.Checked == true ? 1 << 5 : 0);
                fusehigh |= (byte)(fusehigh_4_check.Checked == true ? 1 << 4 : 0);
                fusehigh |= (byte)(fusehigh_3_check.Checked == true ? 1 << 3 : 0);
                fusehigh |= (byte)(fusehigh_2_check.Checked == true ? 1 << 2 : 0);
                fusehigh |= (byte)(fusehigh_1_check.Checked == true ? 1 << 1 : 0);
                fusehigh |= (byte)(fusehigh_0_check.Checked == true ? 1 << 0 : 0);

                fuse |= (byte)(fuselow_7_check.Checked == true ? 1 << 7 : 0);
                fuse |= (byte)(fuselow_6_check.Checked == true ? 1 << 6 : 0);
                fuse |= (byte)(fuselow_5_check.Checked == true ? 1 << 5 : 0);
                fuse |= (byte)(fuselow_4_check.Checked == true ? 1 << 4 : 0);
                fuse |= (byte)(fuselow_3_check.Checked == true ? 1 << 3 : 0);
                fuse |= (byte)(fuselow_2_check.Checked == true ? 1 << 2 : 0);
                fuse |= (byte)(fuselow_1_check.Checked == true ? 1 << 1 : 0);
                fuse |= (byte)(fuselow_0_check.Checked == true ? 1 << 0 : 0);

                extfuse |= (byte)(extfuse_2_check.Checked == true ? 1 << 2 : 0);
                extfuse |= (byte)(extfuse_1_check.Checked == true ? 1 << 1 : 0);
                extfuse |= (byte)(extfuse_0_check.Checked == true ? 1 << 0 : 0);

                SendPacket(new SerialPacket(0x0933, new byte[] { lck })); // Write Lock Bits
                WaitForATMegaRdyBsy();
                SendPacket(new SerialPacket(0x0934, new byte[] { fuse })); // Write Fuse Bits
                WaitForATMegaRdyBsy();
                SendPacket(new SerialPacket(0x0935, new byte[] { fusehigh })); // Write Fuse High Bits
                WaitForATMegaRdyBsy();
                SendPacket(new SerialPacket(0x0936, new byte[] { extfuse })); // Write Extended Fuse Bits
                WaitForATMegaRdyBsy();

                atmega_read_lock_button_Click(sender, e);
            }
        }
        private void verify_atmega_button_Click(object sender, EventArgs e)
        {
            flash_progress.Value = 0;
            flash_progress.Maximum = 0x4000;
            bool valid = true;
            AddLogString("Verifying ATMega ROM");
            for (ushort address = 0; address < 0x4000; address++)
            {

                SendPacket(new SerialPacket(0x920, new byte[] { (byte)(address >> 8), (byte)(address & 0xFF) }));
                SerialPacket packet = GetPacket();
                if (atmega_rom[(address * 2) + 1] != packet.payload[0x04])
                {
                    AddLogString("High Byte - Expected 0x" + atmega_rom[(address * 2) + 0].ToString("X2") + ", Recieved 0x" + packet.payload[0x04].ToString("X2") + " @ 0x" + address.ToString("X8"));
                    valid = false;
                }

                SendPacket(new SerialPacket(0x921, new byte[] { (byte)(address >> 8), (byte)(address & 0xFF) }));
                packet = GetPacket();
                if (atmega_rom[(address * 2) + 0] != packet.payload[0x04])
                {
                    AddLogString("Low Byte - Expected 0x" + atmega_rom[(address * 2) + 0].ToString("X2") + ", Recieved 0x" + packet.payload[0x04].ToString("X2") + " @ 0x" + address.ToString("X8"));
                    valid = false;
                }
                flash_progress.Value = address + 1;
                flash_progress.Refresh();
                flash_progress_label.Text = "0x" + (address + 1).ToString("X8") + " / 0x00004000 (" + ((float)((address + 1) * 100) / 0x4000).ToString("F2") + "%)";
                flash_progress_label.Refresh();
            }
            if (valid)
                AddLogString("ATMega ROM Verified");
            else
                AddLogString("ATMega ROM Verification Failed");
        }
        private void atmega_eeprom_fill_test_data_button_Click(object sender, EventArgs e)
        {
            byte data = 0x00;
            for (int t = 0; t < atmega_eeprom.Length; t++)
                atmega_eeprom[t] = data++;
        }
        private void atmega_rom_fill_test_data_button_Click(object sender, EventArgs e)
        {
            byte data = 0x00;
            for (int t = 0; t < atmega_rom.Length; t++)
                atmega_rom[t] = data++;
        }
        private void write_atmega_eeprom_button_Click(object sender, EventArgs e)
        {
            flash_progress.Value = 0;
            flash_progress.Maximum = 0x400;
            for (ushort address = 0; address < 0x400; address += 0x04)
            {
                for (ushort taddress = 0; taddress < 0x04; taddress++)
                {
                    SendPacket(new SerialPacket(0x913, new byte[] { (byte)(taddress), atmega_eeprom[taddress + address] }));
                }
                SendPacket(new SerialPacket(0x932, new byte[] { (byte)(address >> 8), (byte)(address & 0xFC) }));
                WaitForATMegaRdyBsy();
                flash_progress.Value = address + 0x04;
                flash_progress.Refresh();
                flash_progress_label.Text = "0x" + (address + 0x04).ToString("X8") + " / 0x00000400 (" + ((float)((address + 0x04) * 100) / 0x400).ToString("F2") + "%)";
                flash_progress_label.Refresh();
            }
        }
        private void verify_atmega_eeprom_button_Click(object sender, EventArgs e)
        {
            flash_progress.Value = 0;
            flash_progress.Maximum = 0x400;
            bool valid = true;
            AddLogString("Verifying ATMega EEPROM");
            for (ushort address = 0; address < 0x400; address++)
            {

                SendPacket(new SerialPacket(0x922, new byte[] { (byte)(address >> 8), (byte)(address & 0xFF) }));
                SerialPacket packet = GetPacket();
                if (atmega_eeprom[address] != packet.payload[0x04])
                {
                    AddLogString("Low Byte - Expected 0x" + atmega_rom[(address * 2) + 0].ToString("X2") + ", Recieved 0x" + packet.payload[0x04].ToString("X2") + " @ 0x" + address.ToString("X8"));
                    valid = false;
                }
                flash_progress.Value = address + 1;
                flash_progress.Refresh();
                flash_progress_label.Text = "0x" + (address + 1).ToString("X8") + " / 0x00000400 (" + ((float)((address + 1) * 100) / 0x400).ToString("F2") + "%)";
                flash_progress_label.Refresh();
            }
            if (valid)
                AddLogString("ATMega EEPROM Verified");
            else
                AddLogString("ATMega EEPROM Verification Failed");
        }
        private void atmega_send_spi_Click(object sender, EventArgs e)
        {
            string[] slist = atmega_manual_spi_text.Text.Split(new char[] { ' ' });
            string status = "";
            byte[] pout = new byte[slist.Length];
            for (decimal d = 0; d < atmega_send_spi_times.Value; d++)
            {
                for (int t = 0; t < pout.Length; t++)
                {
                    pout[t] = Convert.ToByte(slist[t], 16);
                    status += "0x" + pout[t].ToString("X2") + " ";
                }
                SendPacket(new SerialPacket(0x4002, pout));
                AddLogString((d + 1).ToString() + "/" + atmega_send_spi_times.Value.ToString() + " Sending " + status);
                status = "";
            }
        }
        private void flash_0_fill_test_data_button_Click(object sender, EventArgs e)
        {
            byte data = 0x00;
            byte offset = 0x00;
            for (int t = 0; t < flash_0_data.Length; t++)
            {
                flash_0_data[t] = (byte)((data++) - offset);
                if (data == 0x00)
                    offset++;
            }

        }
        private void flash_1_fill_test_data_Click(object sender, EventArgs e)
        {
            byte data = 0x00;
            byte offset = 0x00;
            for (int t = 0; t < flash_0_data.Length; t++)
            {
                flash_1_data[t] = (byte)((data++) - offset);
                if (data == 0x00)
                    offset++;
            }
        }
        private void hm_10_reset_button_Click(object sender, EventArgs e)
        {
            hm_10_debug_timer.Enabled = false;
            SendPacket(new SerialPacket(0x3F00, null));
        }
        private void read_hm_10_button_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x1006, null));
            SendPacket(new SerialPacket(0x10C7, null));

            bool erasing = true;
            while (erasing)
            {
                SendPacket(new SerialPacket(0x1006, null));
                SendPacket(new SerialPacket(0x1005, null));
                WaitForFLASH0Status(1 << 7);
                SendPacket(new SerialPacket(0x1FFF, new byte[] { 1, 0 }));
                SerialPacket packet = GetPacket();
                if (packet.payload[4] != 0x03)
                    erasing = false;
            }

            AddLogString("FLASH 0 Erased.");
            SendPacket(new SerialPacket(0x3F03, null));
            flash_progress.Value = 0;
            flash_progress.Maximum = 0x00040000;
            UInt32 address;
            while ((GetATMegaStatus()[3] & (1U << 6)) == (1U << 6))
            {
                update_hm_10_status();
                SendPacket(new SerialPacket(0x3FFE, null));
                SerialPacket packet = GetPacket();
                address = BitConverter.ToUInt32(packet.payload, 4);
                flash_progress.Value = (int)address;
                flash_progress_label.Text = "0x" + address.ToString("X8") + " / 0x00040000 (" + ((float)((address) * 100) / 0x00040000).ToString("F2") + "%)";
                flash_progress_label.Refresh();
                flash_progress.Refresh();
            }
            address = 0x40000;
            flash_progress.Value = (int)address;
            flash_progress_label.Text = "0x" + address.ToString("X8") + " / 0x00040000 (" + ((float)((address) * 100) / 0x00040000).ToString("F2") + "%)";
            flash_progress_label.Refresh();
            flash_progress.Refresh();
        }
        private void write_hm_10_button_Click(object sender, EventArgs e)
        {
            if (hm_10_erase_check.Checked)
            {
                hm_10_erase_click(sender, e);
                update_hm_10_status();
                SendPacket(new SerialPacket(0x3F18, new byte[] { 0x22 })); // Enable DMA
                SendPacket(new SerialPacket(0x3F20, null)); // RD_CONFIG
                update_hm_10_status();

            }

            flash_progress.Value = 0;
            flash_progress.Maximum = 0x00040000;
            SendPacket(new SerialPacket(0x3F02, null));
            UInt32 address;
            while ((GetATMegaStatus()[3] & (1U << 6)) == (1U << 6))
            {
                delayMs(250);
                update_hm_10_status();
                SendPacket(new SerialPacket(0x3FFE, null));
                SerialPacket packet = GetPacket();
                address = BitConverter.ToUInt32(packet.payload, 4);
                flash_progress.Value = (int)address;
                flash_progress_label.Text = "0x" + address.ToString("X8") + " / 0x00040000 (" + ((float)((address) * 100) / 0x00040000).ToString("F2") + "%)";
                flash_progress_label.Refresh();
                flash_progress.Refresh();
            }
            address = 0x40000;
            flash_progress.Value = (int)address;
            flash_progress_label.Text = "0x" + address.ToString("X8") + " / 0x00040000 (" + ((float)((address) * 100) / 0x00040000).ToString("F2") + "%)";
            flash_progress_label.Refresh();
            flash_progress.Refresh();
        }
        private void verify_hm_10_button_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x3F04, null));
        }
        private void read_id_flash_0_button_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x109E, null));
            WaitForFLASH0Status(1 << 7);
            SendPacket(new SerialPacket(0x1FFF, new byte[] { 20, 0 }));
            SerialPacket packet = GetPacket();
            string str = "Flash 0 UID : ";
            for (int t = 0; t < 20; t++)
                str += packet.payload[t + 4].ToString("X2") + " ";

            AddLogString(str);

            SendPacket(new SerialPacket(0x1006, null));
            SendPacket(new SerialPacket(0x1005, null));
            WaitForFLASH0Status(1 << 7);
            SendPacket(new SerialPacket(0x1FFF, new byte[] { 1, 0 }));
            packet = GetPacket();
            str = "Flash 0 Status : 0x" + packet.payload[4].ToString("X2");
            AddLogString(str);
        }
        private void erase_flash_0_button_Click(object sender, EventArgs e)
        {
            if (force_flash == false)
            {
                if (MessageBox.Show("This will erase the entire FLASH 0. Are you sure?", "Confirm Chip Erase", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    SendPacket(new SerialPacket(0x1006, null));
                    SendPacket(new SerialPacket(0x10C7, null));
                    AddLogString("FLASH 0 Erased.");
                }
            }
            else
            {
                SendPacket(new SerialPacket(0x1006, null));
                SendPacket(new SerialPacket(0x10C7, null));
                AddLogString("FLASH 0 Erased.");
            }
        }
        private void hm_10_enable_dma_button_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x3F18, new byte[] { 0x22 })); // Enable DMA
            SendPacket(new SerialPacket(0x3F20, null)); // RD_CONFIG
            update_hm_10_status();
        }

        private void atmega_send_spi_random_Click(object sender, EventArgs e)
        {
            string status = "";
            byte[] pout;
            UInt16 len = 4;
            UInt32 pos = 0;
            Random rando = new Random();
            flash_progress.Maximum = (int)(atmega_send_spi_loops.Value * atmega_send_spi_times.Value);
            flash_progress.Value = 0;
            for (decimal r = 0; r < atmega_send_spi_loops.Value; r++)
            {
                for (decimal d = 0; d < atmega_send_spi_times.Value; d++)
                {
                    flash_progress.Value++;
                    flash_progress_label.Text = "0x" + flash_progress.Value.ToString("X8") + " / 0x" + flash_progress.Maximum.ToString("X8") + " (" + (100 * ((float)flash_progress.Value / flash_progress.Maximum)).ToString("F2") + "%)";
                    flash_progress_label.Refresh();
                    flash_progress.Refresh();

                    len = (ushort)rando.Next(4, 0x108);
                    pout = new byte[len];
                    pout[0x00] = (byte)(len & 0xFF);
                    pout[0x01] = (byte)(len >> 8);
                    pout[0x02] = 0x02;
                    pout[0x03] = 0x40;
                    for (int f = 4; f < len; f++)
                    {
                        pout[f] = (byte)rando.Next();
                    }

                    for (int t = 0; t < pout.Length; t++)
                    {
                        status += "0x" + pout[t].ToString("X2") + " ";
                    }

                    SendPacket(new SerialPacket(0x4002, pout));
                    pos += len;
                    AddLogString(
                        (r + 1).ToString().PadLeft(atmega_send_spi_loops.Value.ToString().Length, '0') + "/" + atmega_send_spi_loops.Value.ToString() + " " +
                        (d + 1).ToString().PadLeft(atmega_send_spi_times.Value.ToString().Length, '0') + "/" + atmega_send_spi_times.Value.ToString()
                        + " " + pos.ToString().PadLeft(4, '0') + " Sending " + status);

                    WaitForFLASH0Status(1 << 7); // Wait for Buffer to be ready

                    SendPacket(new SerialPacket(0x1FFF, new byte[] { (byte)(len & 0xFF), (byte)(len >> 8) })); // Read Buffer
                    pos += (ushort)(len + 6);
                    SerialPacket packet = GetPacket();

                    bool valid = true;
                    for (int td = 0; td < pout.Length; td++)
                    {
                        if (packet.payload[td + 4] != pout[td])
                            valid = false;
                    }
                    status = "";
                    for (int t = 0; t < pout.Length; t++)
                    {
                        status += "0x" + packet.payload[t + 4].ToString("X2") + " ";
                    }
                    if (!valid)
                    {
                        AddLogString(
                            (r + 1).ToString().PadLeft(atmega_send_spi_loops.Value.ToString().Length, '0') + "/" + atmega_send_spi_loops.Value.ToString() + " " +
                            (d + 1).ToString().PadLeft(atmega_send_spi_times.Value.ToString().Length, '0') + "/" + atmega_send_spi_times.Value.ToString()
                            + " " + pos.ToString().PadLeft(4, '0') + " Recv    " + status);

                        AddLogString("Failed");
                        return;
                    }
                    status = "";
                    //len++;
                    //if (len == 0x108)
                    //len = 4;
                }
            }
        }

        private void hm_10_send_cpu_command_Click(object sender, EventArgs e)
        {
            string[] cmds = hm_10_cpu_command_box.Text.Split(' ');
            if ((cmds.Length > 0) && (cmds.Length < 4))
            {
                byte[] data = new byte[cmds.Length];
                int id = 0;
                foreach (string cmd in cmds)
                {
                    data[id++] = Convert.ToByte(cmd, 16);
                }
                SendPacket(new SerialPacket(0x3F50, data));
                update_hm_10_status();
            }
        }
        public void clear_buffer(ref byte[] data)
        {
            for (int t = 0; t < data.Length; t++)
                data[t] = 0xFF;
        }
        private void clear_flash_0_button_Click(object sender, EventArgs e)
        {
            clear_buffer(ref flash_0_data);
        }

        private void hm_10_test_echo_button_Click(object sender, EventArgs e)
        {
            string[] slist = hm_10_test_echo_text.Text.Split(new char[] { ' ' });
            string status = "";
            byte[] pout = new byte[slist.Length];
            for (int t = 0; t < slist.Length; t++)
            {
                pout[t] = Convert.ToByte(slist[t], 16);
            }
            SerialPacket packet = new SerialPacket(pout);
            SendPacket(packet);
            foreach (byte data in packet.payload)
                status += "0x" + data.ToString("X2") + " ";
            AddLogString("Sending " + status);
        }

        private void hextocc2540_button_Click(object sender, EventArgs e)
        {
            force_flash = true;
            atmega_reset_button_Click(sender, e);
            resetdebug_Click(sender, e);
            hm_10_debug_timer.Enabled = false;
            flash_0_write_hex_Click(sender, e);
            erase_flash_0_button_Click(sender, e);
            write_flash_0_button_Click(sender, e);
            write_hm_10_button_Click(sender, e);
            force_flash = false;
            hm_10_debug_timer.Enabled = true;
        }
        public void SetHM10BkptPt(UInt32 address, bool enable, byte bkptnum)
        {
            byte[] pout = new byte[3];
            pout[0x00] = (byte)(((byte)bkptnum << 4) | ((byte)(enable == true ? 1 : 0) << 3) | ((byte)(address >> 16) & 0x7));
            pout[0x01] = (byte)((byte)(address >> 8) & 0xFF);
            pout[0x02] = (byte)((byte)(address >> 0) & 0xFF);
            SendPacket(new SerialPacket(0x3F38, pout));
        }

        public bool hm_10_bkpt_enable_1 = false;
        public bool hm_10_bkpt_enable_2 = false;
        public bool hm_10_bkpt_enable_3 = false;
        public bool hm_10_bkpt_enable_4 = false;

        private void hm_10_bkp_1_button_Click(object sender, EventArgs e)
        {
            hm_10_bkpt_enable_1 = !hm_10_bkpt_enable_1;
            SetHM10BkptPt((UInt32)hm_10_bpk_1.Value, hm_10_bkpt_enable_1, 0);
            if (hm_10_bkpt_enable_1)
                hm_10_bkp_1_button.Text = "Disable";
            else
                hm_10_bkp_1_button.Text = "Enable";
        }

        private void hm_10_bkp_2_button_Click(object sender, EventArgs e)
        {
            hm_10_bkpt_enable_2 = !hm_10_bkpt_enable_2;
            SetHM10BkptPt((UInt32)hm_10_bpk_2.Value, hm_10_bkpt_enable_2, 1);
            if (hm_10_bkpt_enable_2)
                hm_10_bkp_2_button.Text = "Disable";
            else
                hm_10_bkp_2_button.Text = "Enable";
        }

        private void hm_10_bkp_3_button_Click(object sender, EventArgs e)
        {
            hm_10_bkpt_enable_3 = !hm_10_bkpt_enable_3;
            SetHM10BkptPt((UInt32)hm_10_bpk_3.Value, hm_10_bkpt_enable_3, 2);
            if (hm_10_bkpt_enable_3)
                hm_10_bkp_3_button.Text = "Disable";
            else
                hm_10_bkp_3_button.Text = "Enable";
        }

        private void hm_10_bkp_4_button_Click(object sender, EventArgs e)
        {
            hm_10_bkpt_enable_4 = !hm_10_bkpt_enable_4;
            SetHM10BkptPt((UInt32)hm_10_bpk_4.Value, hm_10_bkpt_enable_4, 3);
            if (hm_10_bkpt_enable_4)
                hm_10_bkp_4_button.Text = "Disable";
            else
                hm_10_bkp_4_button.Text = "Enable";
        }

        private void hm_10_debug_timer_Tick(object sender, EventArgs e)
        {
            if (force_flash == false)
            {
                SendPacket(new SerialPacket(0x3F30, null)); // RD_STATUS
                SendPacket(new SerialPacket(0x3F20, null)); // RD_CONFIG
                SendPacket(new SerialPacket(0x3F28, null)); // GET_PC
                SendPacket(new SerialPacket(0x3F60, null)); // GET_BM

                update_hm_10_status();
            }
        }
        private HM10DebugFunction FindFunctionAddress(string name)
        {
            foreach (HM10DebugModule module in DebugModules)
            {
                HM10DebugFunction func = module.FindFunction(name);
                if (func != null)
                    return func;
            }
            return null;
        }
        private void load_hm_10_map_Click(object sender, EventArgs e)
        {
            if (openFileDialog3.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DebugModules.Clear();
                SourceFiles.Clear();

                AddLogString("Loading Linker Map : " + Path.GetFileName(openFileDialog3.FileName));
                string parentdir = Path.GetDirectoryName(openFileDialog3.FileName);
                TextReader reader = new StreamReader(openFileDialog3.FileName);
                bool findingstart = true;
                while (findingstart)
                {
                    string line = reader.ReadLine();
                    if (line.Contains("Entry"))
                    {
                        reader.ReadLine();
                        findingstart = false;
                    }
                }
                bool processsing = true;
                HM10DebugModule newmodule = null;

                UInt32 nummod = 0;
                UInt32 numfunc = 0;

                while (processsing)
                {
                    string line = reader.ReadLine();
                    string[] splits = line.Split(new char[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    if (splits.Length == 0)
                    {
                        processsing = false;
                        break;
                    }
                    if (splits.Length == 3)
                    {
                        numfunc++;
                        if (!splits[1].Contains("BIT"))
                            newmodule.Functions.Add(new HM10DebugFunction(newmodule, Convert.ToUInt32(splits[2], 16), splits[0].TrimEnd(new char[] { ':' }), splits[1]));
                        else
                            newmodule.Functions.Add(new HM10DebugFunction(newmodule, Convert.ToUInt32(splits[2].Split(new char[] { '.' })[0], 16), splits[0].TrimEnd(new char[] { ':' }), splits[1]));
                    }
                    else
                    {
                        if (newmodule != null)
                            DebugModules.Add(newmodule);
                        splits = line.Split(new char[] { '(', ')' });
                        string tname = splits[0];
                        string fname = "No File";
                        if (splits.Length == 3)
                            fname = splits[1].TrimStart(new char[] { ' ' }).TrimEnd(new char[] { ' ' });
                        newmodule = new HM10DebugModule(tname, fname);
                        nummod++;
                    }
                }
                AddLogString("Added " + numfunc.ToString() + " functions in " + nummod.ToString() + " modules.");
                AddLogString("Searching for code list files in " + parentdir + "...");
                IEnumerable<string> files = Directory.EnumerateFiles(parentdir, "*.lst", SearchOption.AllDirectories);
                numfunc = 0;
                foreach (string file in files)
                {
                    HM10SourceFile newfile = new HM10SourceFile(file);
                    AddLogString("Parsing " + Path.GetFileName(file));
                    bool searching = true;
                    bool processing = true;
                    TextReader freader = new StreamReader(file);
                    string sourcefile = null;
                    while (searching)
                    {
                        string line = freader.ReadLine();
                        DirectoryInfo dir = Directory.GetParent(file);
                        sourcefile = dir.Parent.Parent.Parent.Parent.Parent.Parent.FullName;
                        if (line.Contains(sourcefile))
                        {
                            searching = false;
                            newfile.sourcefile = line;
                        }
                    }
                    while (processing)
                    {
                        string line = freader.ReadLine();
                        if (line.Length > 0x00)
                        {
                            if (line.Contains("Maximum stack usage in bytes:"))
                                processing = false;
                            else
                                newfile.AddLine(line);
                        }
                    }
                    SourceFiles.Add(newfile);
                }
                AddLogString("Processing Address Entries");
                flash_progress.Maximum = SourceFiles.Count;
                flash_progress.Value = 0;
                foreach (HM10SourceFile file in SourceFiles)
                {
                    flash_progress.Value++;
                    UInt32 baseaddress = 0;
                    foreach (HM10SourceLine line in file.SourceLines)
                    {
                        if (line.ASMLine)
                        {
                            string[] splits = line.linetext.Split(new char[] { ' ', ':', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                            HM10DebugFunction func = null;
                            if (line.linetext.EndsWith(":"))
                            {
                                func = FindFunctionAddress(splits[0]);
                                if (func != null)
                                {
                                    func.sourcefile = file;
                                    baseaddress = func.address;
                                }
                            }
                            line.absoluteaddress = line.relativeaddress + baseaddress;
                            if (func != null)
                                if (func.MaxAddress < line.absoluteaddress)
                                    func.MaxAddress = line.absoluteaddress;
                        }
                    }
                }
                foreach (HM10DebugModule dmod in DebugModules)
                {
                    foreach (HM10DebugFunction dfunc in dmod.Functions)
                    {
                        if (dfunc.MaxAddress == 0)
                            dfunc.MaxAddress = dfunc.address;
                    }
                }
                cViewer.UpdateTrees();
                cViewer.Show();
                AddLogString("Debugging Info Loaded!");
            }
        }

        private void read_id_flash_1_button_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x209E, null));
            WaitForFLASH1Status(1 << 7);
            SendPacket(new SerialPacket(0x2FFF, new byte[] { 20, 0 }));
            SerialPacket packet = GetPacket();
            string str = "Flash 1 UID : ";
            for (int t = 0; t < 20; t++)
                str += packet.payload[t + 4].ToString("X2") + " ";

            AddLogString(str);

            SendPacket(new SerialPacket(0x2006, null));
            SendPacket(new SerialPacket(0x2005, null));
            WaitForFLASH1Status(1 << 7);
            SendPacket(new SerialPacket(0x2FFF, new byte[] { 1, 0 }));
            packet = GetPacket();
            str = "Flash 1 Status : 0x" + packet.payload[4].ToString("X2");
            AddLogString(str);
        }

        private void hm_10_reset_low_button_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x3F06, null));
        }
        private void hm_10_reset_high_button_Click(object sender, EventArgs e)
        {
            SendPacket(new SerialPacket(0x3F07, null));
        }

        public void hm_10_get_packet(ushort opcode)
        {
            string[] slist = hm_10_gen_data_textbox.Text.Split(new char[] { ' ' });
            byte[] pout = null;
            string status = "";
            if (slist[0x00] != "")
            {

                pout = new byte[slist.Length];

                for (int t = 0; t < slist.Length; t++)
                {
                    pout[t] = Convert.ToByte(slist[t], 16);
                }
            }
            SerialPacket packet = new SerialPacket(opcode, pout);
            foreach (byte data in packet.payload)
                status += data.ToString("X2") + " ";
            status = status.TrimEnd(' ');
            hm_10_test_echo_text.Text = status;
        }

        private void hm_10_gen_packet_button_Click(object sender, EventArgs e)
        {
            hm_10_get_packet(0x3000);
        }


        private void hm_10_gen_packet_gap_button_Click(object sender, EventArgs e)
        {
            hm_10_get_packet(0x3010);
        }

        private void hm_10_gen_packet_l2cap_button_Click(object sender, EventArgs e)
        {
            hm_10_get_packet(0x3011);
        }

        private void hm_10_gen_packet_gatt_button_Click(object sender, EventArgs e)
        {
            hm_10_get_packet(0x3012);
        }

        private void hm_10_gen_packet_gatt_serv_button_Click(object sender, EventArgs e)
        {
            hm_10_get_packet(0x3013);
        }

        private void hm10_test_spi_button_Click(object sender, EventArgs e)
        {
            bool timer = hm10_message_timer.Enabled;
            hm10_message_timer.Enabled = false;
            flash_progress.Maximum = (int)(hm10_test_spi_number.Value * hm10_test_spi_count.Value);
            flash_progress.Value = 0;
            IList<byte[]> sourcedata = new List<byte[]>();
            Random rand = new Random();
            AddLogString("\r\nSending " + hm10_test_spi_count.Value.ToString() + " loops of " + hm10_test_spi_number.Value.ToString() + " each.\r\n");
            flash_progress_label.Text = "0x" + flash_progress.Value.ToString("X8") + " / 0x" + flash_progress.Maximum.ToString("X8") + " (" + (100 * ((float)flash_progress.Value / flash_progress.Maximum)).ToString("F2") + "%)";
            flash_progress_label.Refresh();
            for (int q = 0; q < hm10_test_spi_count.Value; q++)
            {
                sourcedata.Clear();
                for (int t = 0; t < hm10_test_spi_number.Value; t++)
                {
                    string status = "";
                    byte[] pout = new byte[rand.Next(0x05, 0x30)];
                    rand.NextBytes(pout);
                    pout[0x00] = (byte)(pout.Length & 0xFF);
                    pout[0x01] = (byte)((pout.Length >> 8) & 0xFF);
                    pout[0x02] = 0x00;
                    pout[0x03] = 0x30;
                    SerialPacket packet = new SerialPacket(pout);
                    SendPacket(packet);
                    delayMs(25);
                    foreach (byte data in packet.payload)
                        status += "0x" + data.ToString("X2") + " ";
                    AddLogString("Sending " + status);
                    status = "";
                    sourcedata.Add(packet.payload);
                    delayMs(5);
                }

                byte nummessages = 0;
                while (nummessages != hm10_test_spi_number.Value)
                {
                    SendPacket(new SerialPacket(0x3EFD, null));
                    nummessages = GetATMegaStatus()[4];
                }
                if (nummessages > 0)
                    AddLogString(nummessages.ToString() + " HM-10 Messages Available.");
                int tsize = 0;
                for (int ts = 0; ts < hm10_test_spi_number.Value; ts++)
                {
                    tsize += sourcedata[ts].Length;
                }
                AddLogString("Data Size : " + tsize.ToString() + " Bytes.");
                for (int t = 0; t < hm10_test_spi_number.Value; t++)
                {
                    delayMs(5);
                    string status = "";
                    SerialPacket packet = new SerialPacket(0x3EFE, null);
                    SendPacket(packet);
                    WaitForHM10Status(1 << 7);
                    byte[] dlen = GetATMegaStatus();
                    UInt16 len = BitConverter.ToUInt16(dlen, 5);


                    //packet = new SerialPacket(0x3EFE, null);
                    //SendPacket(packet);
                    //packet = GetPacket();
                    ushort messagesize = (ushort)(dlen[5] + (dlen[6] << 8));
                    //WaitForHM10Status(1 << 7);
                    packet = new SerialPacket(0x3EFF, BitConverter.GetBytes(messagesize));
                    SendPacket(packet);
                    packet = GetPacket();
                    foreach (byte data in packet.payload)
                        status += "0x" + data.ToString("X2") + " ";
                    AddLogString("Recv    " + status);
                    status = "";
                    flash_progress.Value++;
                    flash_progress_label.Text = "0x" + flash_progress.Value.ToString("X8") + " / 0x" + flash_progress.Maximum.ToString("X8") + " (" + (100 * ((float)flash_progress.Value / flash_progress.Maximum)).ToString("F2") + "%)";
                    flash_progress_label.Refresh();
                    for (int r = 0; r < sourcedata[t].Length; r++)
                    {
                        if (r == 2) continue;
                        if (r == 3) continue;
                        if (sourcedata[t][r] != packet.payload[r + 4])
                        {
                            AddLogString("Failed @ " + r.ToString() + " : 0x" + sourcedata[t][r].ToString("X2") + " != 0x" + packet.payload[r + 4].ToString("X2"));
                            AddLogString("SPI Test Failed!");
                            return;
                        }
                    }
                    GetATMegaStatus();
                    SendPacket(new SerialPacket(0x3EFD, null));
                    Application.DoEvents();
                }
                GetATMegaStatus();
            }
            hm10_message_timer.Enabled = timer;
        }

        private void hm10_message_timer_Tick(object sender, EventArgs e)
        {
            SerialPacket packet = new SerialPacket(0x3EFD, null);
            SendPacket(packet);
            byte nummessages = GetATMegaStatus()[4];
            if (nummessages > 0)
            {

                AddLogString(nummessages.ToString() + " HM-10 Messages Available.");
                for (int t = 0; t < nummessages; t++)
                {
                    delayMs(5);
                    string status = "";
                    packet = new SerialPacket(0x3EFE, null);
                    SendPacket(packet);
                    WaitForHM10Status(1 << 7);
                    byte[] dlen = GetATMegaStatus();
                    UInt16 len = BitConverter.ToUInt16(dlen, 5);


                    //packet = new SerialPacket(0x3EFE, null);
                    //SendPacket(packet);
                    //packet = GetPacket();
                    ushort messagesize = (ushort)(dlen[5] + (dlen[6] << 8));
                    //WaitForHM10Status(1 << 7);
                    packet = new SerialPacket(0x3EFF, BitConverter.GetBytes(messagesize));
                    SendPacket(packet);
                    packet = GetPacket();
                    foreach (byte data in packet.payload)
                        status += "0x" + data.ToString("X2") + " ";
                    AddLogString("Recv    " + status);
                    status = "";
                    flash_progress.Value++;
                    flash_progress_label.Text = "0x" + flash_progress.Value.ToString("X8") + " / 0x" + flash_progress.Maximum.ToString("X8") + " (" + (100 * ((float)flash_progress.Value / flash_progress.Maximum)).ToString("F2") + "%)";
                    flash_progress_label.Refresh();
                    GetATMegaStatus();
                    SendPacket(new SerialPacket(0x3EFD, null));
                    Application.DoEvents();
                }
            }
            GetATMegaStatus();
        }

        private void hm10_message_timer_button_Click(object sender, EventArgs e)
        {
            hm10_message_timer.Enabled = true;
        }

        private void hm10_message_timer_disable_button_Click(object sender, EventArgs e)
        {
            hm10_message_timer.Enabled = false;
        }

        private void datarate_timer_Tick(object sender, EventArgs e)
        {
            if (DataRateIn > 900)
                datarate_in_label.Text = (DataRateIn / 1024.0f).ToString("F3") + " KB/Sec In";
            else
                datarate_in_label.Text = DataRateIn.ToString("F3") + " Bytes/Sec In";
            DataRateIn = 0;
            if (DataRateOut > 900)
                datarate_out_label.Text = (DataRateOut / 1024.0f).ToString("F3") + " KB/Sec Out";
            else
                datarate_out_label.Text = DataRateOut.ToString("F3") + " Bytes/Sec Out";
            DataRateOut = 0;

            if (DataTotalOut <= 900)
                data_out_total_label.Text = DataTotalOut.ToString("F3") + " Bytes Out";
            if (DataTotalOut > 900)
                data_out_total_label.Text = (DataTotalOut / 1024.0f).ToString("F3") + " KB Out";
            if (DataTotalOut > 943718)
                data_out_total_label.Text = (DataTotalOut / 1048576.0f).ToString("F3") + " MB Out";

            if (DatatotalIn <= 900)
                data_in_total_label.Text = DatatotalIn.ToString("F3") + " Bytes In";
            if (DatatotalIn > 900)
                data_in_total_label.Text = (DatatotalIn / 1024.0f).ToString("F3") + " KB In";
            if (DatatotalIn > 943718)
                data_in_total_label.Text = (DatatotalIn / 1048576.0f).ToString("F3") + " MB In";
        }

        private void erase_flash_1_button_Click(object sender, EventArgs e)
        {
            if (force_flash == false)
            {
                if (MessageBox.Show("This will erase the entire FLASH 1. Are you sure?", "Confirm Chip Erase", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    SendPacket(new SerialPacket(0x2006, null));
                    SendPacket(new SerialPacket(0x20C7, null));
                    AddLogString("FLASH 1 Erased.");
                }
            }
            else
            {
                SendPacket(new SerialPacket(0x2006, null));
                SendPacket(new SerialPacket(0x20C7, null));
                AddLogString("FLASH 1 Erased.");
            }
        }

        private void BLE_COMMAND_SEND_Click(object sender, EventArgs e)
        {
            hm10_message_timer.Enabled = true;
            ((BLE_Command)BLE_COMMAND_PROP.SelectedObject).SendCommand();
        }

        private void BLE_COMMAND_PROP_Click(object sender, EventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            BLE_COMMAND_PROP.SelectedObject = e.Node.Tag;
        }

        private static void ChangeDescriptionHeight(PropertyGrid grid, int height)
        {
            if (grid == null) throw new ArgumentNullException("grid");

            foreach (Control control in grid.Controls)
                if (control.GetType().Name == "DocComment")
                {
                    System.Reflection.FieldInfo fieldInfo = control.GetType().BaseType.GetField("userSized",
                      System.Reflection.BindingFlags.Instance |
                      System.Reflection.BindingFlags.NonPublic);
                    fieldInfo.SetValue(control, true);
                    control.Height = height;
                    return;
                }
        }

    }
    public abstract class BLE_Command
    {
        public byte hdr_event;
        public byte hdr_status;
        private byte pOGF;
        private byte pCSG;
        private byte pCMD;
        private ushort pOPCODE;
        public OSCC2540 parent;

        [CategoryAttribute("OpCode"), ReadOnlyAttribute(true), TypeConverter(typeof(UInt8HexTypeConverter)), Description("OGF (6-bytes). Must be 63")]
        public byte OGF
        {
            get { return pOGF; }
        }
        [CategoryAttribute("OpCode"), ReadOnlyAttribute(true), TypeConverter(typeof(UInt8HexTypeConverter)), Description("Command Sub Group (3-bytes).")]
        public byte CSG
        {
            get { return pCSG; }
        }
        [CategoryAttribute("OpCode"), ReadOnlyAttribute(true), TypeConverter(typeof(UInt8HexTypeConverter)), Description("Command. (7-bytes).")]
        public byte CMD
        {
            get { return pCMD; }
        }
        [CategoryAttribute("OpCode"), ReadOnlyAttribute(true), TypeConverter(typeof(UInt16HexTypeConverter)), Description("OPCode (16 bytes).")]
        public ushort OPCode
        {
            get { return pOPCODE; }
        }
        public abstract void SendCommand();
        public BLE_Command(ushort icmd, OSCC2540 iparent)
        {
            hdr_event = 0x93;
            hdr_status = 0x00;
            parent = iparent;
            pOPCODE = icmd;
            pCMD = (byte)(icmd & 0x007F);
            pCSG = (byte)((icmd >> 7) & 0x0007);
            pOGF = (byte)((icmd >> 10) & 0x003F);
        }
    }

    /////////////////////////////////////////////////////////////////
    //  HCI Commands
    /////////////////////////////////////////////////////////////////

    public class BLE_Command_HCI_Set_Rx_Gain : BLE_Command
    {
        private byte pGain;

        [CategoryAttribute("Rx Gain"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to set the RF receiver gain. The default system value for this feature is standard receiver gain.\r\n\r\n0x00 - HCI_EXT_RX_GAIN_STD\r\n0x01 - HCI_EXT_RX_GAIN_HIGH")]
        public byte Gain
        {
            get { return pGain; }
            set { pGain = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pGain);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Set_Rx_Gain(OSCC2540 parent)
            : base(0xFC00, parent)
        {

        }
    }
    public class BLE_Command_HCI_Set_Tx_Power : BLE_Command
    {
        private byte pPower;

        [CategoryAttribute("Tx Power"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to set the RF transmitter output power. The default system value for this feature is 0 dBm. Note that a setting of 4dBm is only allowed for the CC2540.\r\n\r\n0x00 - HCI_EXT_TX_POWER_MINUS_23_DBM\r\n0x01 - HCI_EXT_TX_POWER_MINUS_6_DBM\r\n0x02 - HCI_EXT_TX_POWER_0_DBM\r\n0x03 - HCI_EXT_TX_POWER_4_DBM (CC2540 only)")]
        public byte Power
        {
            get { return pPower; }
            set { pPower = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pPower);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Set_Tx_Power(OSCC2540 parent)
            : base(0xFC01, parent)
        {

        }
    }
    public class BLE_Command_HCI_One_Packet_Per_Event : BLE_Command
    {
        private byte pPacket;

        [CategoryAttribute("Packet"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to configure the Link Layer to only allow one packet per connection event. The default system value for this feature is disabled.\r\nThis command can be used to tradeoff throughput and power consumption during a connection.\r\nWhen enabled, power can be conserved during a connection by limiting the number of packets per connection event to one, at the expense of more limited throughput.\r\nWhen disabled, the number of packets transferred during a connection event is not limited, at the expense of higher power consumption.\r\n\r\n0x00 - HCI_EXT_DISABLE_ONE_PKT_PER_EVT\r\n0x01 - HCI_EXT_ENABLE_ONE_PKT_PER_EVT")]
        public byte Power
        {
            get { return pPacket; }
            set { pPacket = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pPacket);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_One_Packet_Per_Event(OSCC2540 parent)
            : base(0xFC02, parent)
        {

        }
    }
    public class BLE_Command_HCI_Clock_Divide_On_Halt : BLE_Command
    {
        private byte pPacket;

        [CategoryAttribute("Clock"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to configure the Link Layer to divide the system clock when the MCU is halted during a radio operation. The default system value for this feature is disabled.\r\nNote: This command is only valid when the MCU is halted during RF operation (please see HCI_EXT_HaltDuringRfCmd).\r\n\r\n0x00 - HCI_EXT_DISABLE_CLK_DIVIDE_ON_HALT\r\n0x01 - HCI_EXT_ENABLE_CLK_DIVIDE_ON_HALT")]
        public byte ClockDivide
        {
            get { return pPacket; }
            set { pPacket = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pPacket);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Clock_Divide_On_Halt(OSCC2540 parent)
            : base(0xFC03, parent)
        {

        }
    }
    public class BLE_Command_HCI_Clock_Declare_NV_Usage : BLE_Command
    {
        private byte pNV;

        [CategoryAttribute("NV Usage"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to inform the Controller whether the Host is using NV memory during BLE operations. The default system value for this feature is NV In Use.\r\nWhen the NV is not in use during BLE operations, the Controller is able to bypass internal checks that reduce overhead processing, thereby reducing average power consumption.\r\nNote: This command is only allowed when the BLE Controller is idle.\r\nNote: Using NV when declaring it is not in use may result in a hung BLE Connection.\r\n\r\n0x00 - HCI_EXT_NV_NOT_IN_USE\r\n0x01 - HCI_EXT_NV_IN_USE")]
        public byte NVUsage
        {
            get { return pNV; }
            set { pNV = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pNV);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Clock_Declare_NV_Usage(OSCC2540 parent)
            : base(0xFC04, parent)
        {

        }
    }
    public class BLE_Command_HCI_Clock_Decrypt : BLE_Command
    {
        private byte[] pkey = new byte[16];
        private byte[] pText = new byte[16];

        [CategoryAttribute("Encryption"), ReadOnlyAttribute(false), TypeConverter(typeof(SecurityKey16ByteHexTypeConverter)),
            Description("128 bit key for the decryption of the data given in the command. The most significant octet of the data corresponds to key[0] using the notation specified in FIPS 197.")]
        public byte[] Key
        {
            get { return pkey; }
            set { pkey = value; }
        }
        [CategoryAttribute("Encryption"), ReadOnlyAttribute(false), TypeConverter(typeof(SecurityKey16ByteHexTypeConverter)),
            Description("128 bit encrypted data to be decrypted. The most significant octet of the key corresponds to key[0] using the notation specified in FIPS 197.")]
        public byte[] Text
        {
            get { return pText; }
            set { pText = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pkey);
            data.Add(pText);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Clock_Decrypt(OSCC2540 parent)
            : base(0xFC05, parent)
        {

        }
    }
    public class BLE_Command_HCI_Set_Local_Supported : BLE_Command
    {
        private UInt64 pFeatures;

        [CategoryAttribute("Features"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt64HexTypeConverter)),
            Description("This command is used to set the Controller’s Local Supported Features. For a complete list of supported LE features, please see [1], Part B, Section 4.6.\r\nNote: This command can be issued either before or after one or more connections are formed.\r\nHowever, the local features set in this manner are only effective if performed before a Feature Exchange Procedure has been initiated by the Master.\r\nOnce this control procedure has been completed for a particular connection, only the exchanged feature set for that connection will be used.\r\n\r\n0x0000000000000001 - Encryption Feature\r\n0xFFFFFFFFFFFFFFFE - Reserved for future use")]
        public UInt64 Features
        {
            get { return pFeatures; }
            set { pFeatures = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pFeatures);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Set_Local_Supported(OSCC2540 parent)
            : base(0xFC06, parent)
        {

        }
    }
    public class BLE_Command_HCI_Set_Fast_Tx_Response_Time : BLE_Command
    {
        private byte pTxTime;

        [CategoryAttribute("Tx Time"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to configure the Link Layer fast transmit response time feature. The default system value for this feature is enabled.\r\nNote: This command is only valid for a Slave controller.\r\n\r\n0x00 - HCI_EXT_DISABLE_FAST_TX_RESP_TIME\r\n0x01 - HCI_EXT_ENABLE_FAST_TX_RESP_TIME")]
        public byte Fast_Tx_Time
        {
            get { return pTxTime; }
            set { pTxTime = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pTxTime);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Set_Fast_Tx_Response_Time(OSCC2540 parent)
            : base(0xFC07, parent)
        {

        }
    }
    public class BLE_Command_HCI_Modem_Test_Transmit : BLE_Command
    {
        private byte pMode;
        private byte pTxFreq;

        [CategoryAttribute("Modem Test"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("0x00 - HCI_EXT_TX_MODULATED_CARRIER\r\n0x01 - HCI_EXT_TX_UNMODULATED_CARRIER")]
        public byte Mode
        {
            get { return pMode; }
            set { pMode = value; }
        }
        [CategoryAttribute("Modem Test"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("0..39 - RF channel of transmit frequency.")]
        public byte Tx_Frequency
        {
            get { return pTxFreq; }
            set { pTxFreq = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pMode);
            data.Add(pTxFreq);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Modem_Test_Transmit(OSCC2540 parent)
            : base(0xFC08, parent)
        {

        }
    }
    public class BLE_Command_HCI_Modem_Test_Transmit_Hop : BLE_Command
    {
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Modem_Test_Transmit_Hop(OSCC2540 parent)
            : base(0xFC09, parent)
        {

        }
    }
    public class BLE_Command_HCI_Modem_Test_Receive : BLE_Command
    {
        private byte pTxFreq;
        [CategoryAttribute("Modem Test"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("0..39 - RF channel of receive frequency.")]
        public byte Tx_Frequency
        {
            get { return pTxFreq; }
            set { pTxFreq = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pTxFreq);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Modem_Test_Receive(OSCC2540 parent)
            : base(0xFC0A, parent)
        {

        }
    }
    public class BLE_Command_HCI_Modem_Test_End : BLE_Command
    {
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Modem_Test_End(OSCC2540 parent)
            : base(0xFC0B, parent)
        {

        }
    }
    public class BLE_Command_HCI_Set_BD_Addr : BLE_Command
    {
        private byte[] pAddress = new byte[6];

        [CategoryAttribute("Address"), ReadOnlyAttribute(false), TypeConverter(typeof(BTAddrConverter)),
            Description("This command is used to set this device’s BLE address (BDADDR). This address will override the device’s address determined when the device is reset (i.e. a hardware reset, not an HCI Controller Reset).\r\nTo restore the device’s initialized address, issue this command with an invalid address.\r\nNote: This command is only allowed when the Controller is in the Standby state.\r\n\r\n0x000000000000..0xFFFFFFFFFFFE - Valid BLE device address.\r\n0xFFFFFFFFFFFF - Invalid BLE device address. Used to restore the device address to that which was determined at initialization.")]
        public byte[] Address
        {
            get { return pAddress; }
            set { pAddress = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pAddress);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Set_BD_Addr(OSCC2540 parent)
            : base(0xFC0C, parent)
        {

        }
    }
    public class BLE_Command_HCI_Set_SCA : BLE_Command
    {
        private UInt16 pSCA;

        [CategoryAttribute("SCA"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt16HexTypeConverter)),
            Description("This command is used to set this device’s Sleep Clock Accuracy (SCA) value, in parts per million (PPM), from 0 to 500. For a Master device, the value is converted to one of eight ordinal values representing a SCA range (per [1], Volume 6, Part B, Section 2.3.3.1, Table 2.2),\r\nwhich will be used when a connection is created. For a Slave device, the value is directly used. The system default value for a Master and Slave device is 50ppm and 40ppm, respectively.\r\nNote: This command is only allowed when the device is not in a connection.\r\nNote: The device’s SCA value remains unaffected by an HCI Reset.\r\n0..0x1F4 - Valid SCA value.\r\n0x01F5..0xFFFF - Invalid SCA value.")]
        public UInt16 SCA
        {
            get { return pSCA; }
            set { pSCA = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pSCA);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Set_SCA(OSCC2540 parent)
            : base(0xFC0D, parent)
        {

        }
    }
    public class BLE_Command_HCI_Enable_PTM : BLE_Command
    {
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Enable_PTM(OSCC2540 parent)
            : base(0xFC0E, parent)
        {

        }
    }
    public class BLE_Command_HCI_Set_Freq_Tune : BLE_Command
    {
        private byte pFreqTune;

        [CategoryAttribute("Frequency Tuning"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This PTM-only command is used to set this device’s Frequency Tuning either up one step or down one step. When the current setting is already at its max value, then stepping up will have no effect.\r\nWhen the current setting is already at its min value, then stepping down will have no effect.\r\nThis setting will only remain in effect until the device is reset unless HCI_EXT_SaveFreqTuneCmd is used to save it in non-volatile memory.\r\n\r\n0 - HCI_PTM_SET_FREQ_TUNE_DOWN\r\n1 - HCI_PTM_SET_FREQ_TUNE_UP")]
        public byte Frequency_Tuning
        {
            get { return pFreqTune; }
            set { pFreqTune = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pFreqTune);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Set_Freq_Tune(OSCC2540 parent)
            : base(0xFC0F, parent)
        {

        }
    }
    public class BLE_Command_HCI_Save_Freq_Tune : BLE_Command
    {
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Save_Freq_Tune(OSCC2540 parent)
            : base(0xFC10, parent)
        {

        }
    }
    public class BLE_Command_HCI_Set_Max_DTM_Rx_Power : BLE_Command
    {
        private byte pRxPwr;

        [CategoryAttribute("DTM Rx Power"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to override the RF transmitter output power used by the Direct Test Mode (DTM).\r\nNormally, the maximum transmitter output power setting used by DTM is the maximum transmitter output power setting for the device (i.e. 4 dBm for the CC2540; 0 dBm for the CC2541).\r\nThis command will change the value used by DTM.\r\nNote: When DTM is ended by a call to HCI_LE_TestEndCmd, or a HCI_Reset is used, the transmitter output power setting is restored to the default value of 0 dBm.\r\n\r\n0x00 - HCI_EXT_TX_POWER_MINUS_23_DBM\r\n0x01 - HCI_EXT_TX_POWER_MINUS_6_DBM\r\n0x02 - HCI_EXT_TX_POWER_0_DBM\r\n0x03 - HCI_EXT_TX_POWER_4_DBM (CC2540 only)")]
        public byte DTM_Rx_Power
        {
            get { return pRxPwr; }
            set { pRxPwr = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pRxPwr);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Set_Max_DTM_Rx_Power(OSCC2540 parent)
            : base(0xFC11, parent)
        {

        }
    }
    public class BLE_Command_HCI_Map_PM_IO_Port : BLE_Command
    {
        private byte pioPort;
        private byte pioPin;

        [CategoryAttribute("IO Port"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to configure and map a CC254x I/O Port as a General-Purpose I/O (GPIO) output signal that reflects the Power Management (PM) state of the CC254x device.\r\nThe GPIO output will be High on Wake, and Low upon entering Sleep. This feature can be disabled by specifying HCI_EXT_PM_IO_PORT_NONE for the ioPort (ioPin is then ignored).\r\nThe system default value upon hardware reset is disabled.\r\n\r\n0x00 - HCI_EXT_PM_IO_PORT_P0\r\n0x01 - HCI_EXT_PM_IO_PORT_P1\r\n0x02 - HCI_EXT_PM_IO_PORT_P2\r\n0xFF - HCI_EXT_PM_IO_PORT_NONE")]
        public byte ioPort
        {
            get { return pioPort; }
            set { pioPort = value; }
        }
        [CategoryAttribute("IO Port"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to configure and map a CC254x I/O Port as a General-Purpose I/O (GPIO) output signal that reflects the Power Management (PM) state of the CC254x device.\r\nThe GPIO output will be High on Wake, and Low upon entering Sleep. This feature can be disabled by specifying HCI_EXT_PM_IO_PORT_NONE for the ioPort (ioPin is then ignored).\r\nThe system default value upon hardware reset is disabled.\r\n\r\n0x00 - HCI_EXT_PM_IO_PORT_PIN0\r\n0x01 - HCI_EXT_PM_IO_PORT_PIN1\r\n0x02 - HCI_EXT_PM_IO_PORT_PIN2\r\n0x03 - HCI_EXT_PM_IO_PORT_PIN3\r\n0x04 - HCI_EXT_PM_IO_PORT_PIN4\r\n0x05 - HCI_EXT_PM_IO_PORT_PIN5\r\n0x06 - HCI_EXT_PM_IO_PORT_PIN6\r\n0x07 - HCI_EXT_PM_IO_PORT_PIN7")]
        public byte ioPin
        {
            get { return pioPin; }
            set { pioPin = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pioPort);
            data.Add(pioPin);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Map_PM_IO_Port(OSCC2540 parent)
            : base(0xFC12, parent)
        {

        }
    }
    public class BLE_Command_HCI_Disconnect_Immediate : BLE_Command
    {
        private UInt16 pConn;

        [CategoryAttribute("Connection"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt16HexTypeConverter)),
            Description("This command is used to disconnect a connection immediately. This command can be useful for when a connection needs to be ended without the latency associated with the normal BLE Controller Terminate control procedure.\r\nNote that the Host issuing the command will still receive the HCI Disconnection Complete event with a Reason status of 0x16 (i.e. Connection Terminated by Local Host), followed by an HCI Vendor Specific Event.\r\n\r\n0x0000 .. 0x0EFF - Connection Handle to be used to identify a connection.\r\nNote: 0x0F00 – 0x0FFF are reserved for future use.")]
        public UInt16 Connection_Handle
        {
            get { return pConn; }
            set { pConn = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pConn);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Disconnect_Immediate(OSCC2540 parent)
            : base(0xFC13, parent)
        {

        }
    }
    public class BLE_Command_HCI_Packet_Error_Rate : BLE_Command
    {
        private UInt16 pConn;
        private byte pCmd;

        [CategoryAttribute("Connection"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt16HexTypeConverter)),
            Description("This command is used to Reset or Read the Packet Error Rate counters for a connection. When Reset, the counters are cleared; when Read, the total number of packets received, the number of packets received with a CRC error, the number of events, and the number of missed events are returned.\r\nThe system default value upon hardware reset is Reset.\r\nNote: The counters are only 16 bits. At the shortest connection interval, this provides a little over 8 minutes of data.\r\n\r\n0x0000 .. 0x0EFF - Connection Handle to be used to identify a connection.\r\nNote: 0x0F00 – 0x0FFF are reserved for future use.")]
        public UInt16 Connection_Handle
        {
            get { return pConn; }
            set { pConn = value; }
        }
        [CategoryAttribute("Connection"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("0x00 - HCI_EXT_PER_RESET\r\n0x01 - HCI_EXT_PER_READ")]
        public byte Command
        {
            get { return pCmd; }
            set { pCmd = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pConn);
            data.Add(pCmd);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Packet_Error_Rate(OSCC2540 parent)
            : base(0xFC14, parent)
        {

        }
    }
    public class BLE_Command_HCI_Packet_Error_Rate_By_Channel : BLE_Command
    {
        private UInt16 pConn;
        private byte pChan;

        [CategoryAttribute("Connection"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt16HexTypeConverter)),
            Description("This command is used to start or end Packet Error Rate by Channel counter accumulation for a connection, and can be used by an application to make Coexistence assessments.\r\nBased on the results, an application can perform an Update Channel Classification command to limit channel interference from other wireless standards.\r\nIf *perByChan is NULL, counter accumulation will be discontinued.\r\nIf *perByChan is not NULL, then it is assumed that there is sufficient memory for the PER data.\r\nNote: This command is only allowed as a direct function call, and is only intended to be used by an embedded application.Note: It is the user’s responsibility to ensure there is sufficient memory allocated! The user is also responsible for maintaining the counters, clearing them if required before starting accumulation.\r\nNote: As indicated, the counters are 16 bits. At the shortest connection interval, this provides a bit over 8 minutes of data.\r\nNote: This command can be used in combination with HCI_EXT_PacketErrorRateCmd.\r\n\r\n0x0000 .. 0x0EFF - Connection Handle to be used to identify a connection.\r\nNote: 0x0F00 – 0x0FFF are reserved for future use.")]
        public UInt16 Connection_Handle
        {
            get { return pConn; }
            set { pConn = value; }
        }
        [CategoryAttribute("Connection"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("NULL - End counter accumulation.\r\nNon-NULL - Start counter accumulation by channel.")]
        public byte Channel
        {
            get { return pChan; }
            set { pChan = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pConn);
            data.Add(pChan);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Packet_Error_Rate_By_Channel(OSCC2540 parent)
            : base(0xFC15, parent)
        {

        }
    }
    public class BLE_Command_HCI_Extend_RF_Range : BLE_Command
    {
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Extend_RF_Range(OSCC2540 parent)
            : base(0xFC16, parent)
        {

        }
    }
    public class BLE_Command_HCI_Advertiser_Event_Notice : BLE_Command
    {
        private UInt16 ptastEvent;
        private byte ptaskdID;

        [CategoryAttribute("Events"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt16HexTypeConverter)),
            Description("This command is used to configure the CC254x to set a user task’s event when an Advertisement event completes. Only a single task event value is allowed (i.e. must be a power of two).\r\nA non-zero taskEvent value is taken to be \"enable\", while a zero valued taskEvent is taken to be \"disable\". The default value is \"disable\".\r\n\r\nNote: This command is only allowed as a direct function call, and is only intended to be used by an embedded application. No vendor specific Command Complete event will be generated.\r\n\r\n0x00..0xFF - OSAL task ID.")]
        public UInt16 Task_Event
        {
            get { return ptastEvent; }
            set { ptastEvent = value; }
        }
        [CategoryAttribute("Events"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("2^0 .. 2^14 - OSAL task event.\r\n\r\nNote that 2^15 is a reserved system task event.")]
        public byte Task_ID
        {
            get { return ptaskdID; }
            set { ptaskdID = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(ptaskdID);
            data.Add(ptastEvent);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Advertiser_Event_Notice(OSCC2540 parent)
            : base(0xFC17, parent)
        {

        }
    }
    public class BLE_Command_HCI_Connection_Event_Notice : BLE_Command
    {
        private UInt16 ptastEvent;
        private byte ptaskdID;

        [CategoryAttribute("Events"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt16HexTypeConverter)),
            Description("This command is used to configure the CC254x to set a user task’s event when a Connection event completes. Only a single task event value is allowed (i.e. must be a power of two).\r\nA non-zero taskEvent value is taken to be \"enable\", while a zero valued taskEvent is taken to be \"disable\". The default value is \"disable\".\r\n\r\nNote: Only a Slave connection is supported.\r\nNote: This command is only allowed as a direct function call, and is only intended to be used by an embedded application. No vendor specific Command Complete event will be generated.\r\n\r\n0x00..0xFF - OSAL task ID.")]
        public UInt16 Task_Event
        {
            get { return ptastEvent; }
            set { ptastEvent = value; }
        }
        [CategoryAttribute("Events"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("2^0 .. 2^14 - OSAL task event.\r\n\r\nNote that 2^15 is a reserved system task event.")]
        public byte Task_ID
        {
            get { return ptaskdID; }
            set { ptaskdID = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(ptaskdID);
            data.Add(ptastEvent);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Connection_Event_Notice(OSCC2540 parent)
            : base(0xFC18, parent)
        {

        }
    }
    public class BLE_Command_HCI_Halt_During_RF : BLE_Command
    {
        private byte pMode;

        [CategoryAttribute("Radio"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to enable or disable the halting of the MCU while the radio is operating. When the MCU is not halted, the peak current is higher, but the system is more responsive.\r\nWhen the MCU is halted, the peak current consumption is reduced, but the system is less responsive. The default value is Enable.\r\n\r\nNote: This command will be disallowed if there are any active BLE connections.\r\nNote: The HCI_EXT_ClkDivOnHaltCmd will be disallowed if the halt during RF is not enabled.\r\n\r\n0x00 - HCI_EXT_HALT_DURING_RF_DISABLE\r\n0x01 - HCI_EXT_HALT_DURING_RF_ENABLE")]
        public byte Mode
        {
            get { return pMode; }
            set { pMode = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pMode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Halt_During_RF(OSCC2540 parent)
            : base(0xFC19, parent)
        {

        }
    }
    public class BLE_Command_HCI_Set_Slave_Latency_Override : BLE_Command
    {
        private byte pMode;

        [CategoryAttribute("Radio"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to enable or disable the Slave Latency Override, allowing the user to ensure that Slave Latency is not applied even though it is active.\r\nThe default value is Disable.\r\n\r\nNote: This command will be disallowed for no connection, or the connection is not in the Slave role.0x00 - HCI_EXT_DISABLE_SL_OVERRIDE\r\n0x01 - HCI_EXT_ENABLE_SL_OVERRIDE")]
        public byte Mode
        {
            get { return pMode; }
            set { pMode = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pMode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Set_Slave_Latency_Override(OSCC2540 parent)
            : base(0xFC1A, parent)
        {

        }
    }
    public class BLE_Command_HCI_Build_Revision : BLE_Command
    {
        private byte pMode;
        private UInt16 PUserRevNum;

        [CategoryAttribute("Build Version"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("This command is used to a) allow the embedded user code to set their own 16 bit revision number, and b) to read the build revision number of the BLE stack library software.\r\nThe default value of the user revision number is zero.\r\nWhen the user updates a BLE project by adding their own code, they may use this API to set their own revision number. When called with mode set to HCI_EXT_SET_APP_REVISION, the stack will save this value. No event will be returned from this API when used this way as it is intended to be called from within the target itself. Note however that this does not preclude this command from being received via the HCI. However, no event will be returned.\r\nWhen this API is used from the HCI, then the second parameter is ignored, and a vendor specific event is returned with the user’s revision number and the build revision number of the BLE stack.\r\n0x00 - HCI_EXT_SET_APP_REVISION\r\n0x01 - HCI_EXT_READ_BUILD_REVISION")]
        public byte Mode
        {
            get { return pMode; }
            set { pMode = value; }
        }
        [CategoryAttribute("Build Version"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt16HexTypeConverter)),
            Description("This command is used to a) allow the embedded user code to set their own 16 bit revision number, and b) to read the build revision number of the BLE stack library software.\r\nThe default value of the user revision number is zero.\r\nWhen the user updates a BLE project by adding their own code, they may use this API to set their own revision number. When called with mode set to HCI_EXT_SET_APP_REVISION, the stack will save this value. No event will be returned from this API when used this way as it is intended to be called from within the target itself. Note however that this does not preclude this command from being received via the HCI. However, no event will be returned.\r\nWhen this API is used from the HCI, then the second parameter is ignored, and a vendor specific event is returned with the user’s revision number and the build revision number of the BLE stack.\r\n0xXXXX - Any 16 bit value the application wishes to use as their revision number.")]
        public UInt16 UserRevNum
        {
            get { return PUserRevNum; }
            set { PUserRevNum = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pMode);
            data.Add(PUserRevNum);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Build_Revision(OSCC2540 parent)
            : base(0xFC1B, parent)
        {

        }
    }
    public class BLE_Command_HCI_Delay_Sleep : BLE_Command
    {
        private UInt16 pDelay;

        [CategoryAttribute("Sleep"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt16HexTypeConverter)),
             Description("This command is used to set the delay before sleep occurs after Reset or upon waking from PM3 (i.e. deep sleep) to allow the external 32kHz crystal to stabilize.\r\nIf this command is never used, the default delay is 400ms.\r\nIf the customer’s hardware requires a different delay or does not require this delay at all, it can be changed by calling this command during their OSAL task initialization. A non-zero delay value will change the delay after Reset and all subsequent (unless changed again) wakes from PM3; a zero delay value will eliminate the delay after Reset and all subsequent (unless changed again) wakes from PM3.\r\nIf this command is used any time after system initialization, then the new delay value will be applied the next time the delay is used.\r\nNote: This delay only applies to Reset and Deep Sleep (i.e. PM3). If a periodic timer is used, or a BLE operation is active, then only PM2 is used, and this delay will only occur after Reset.\r\nNote: There is no distinction made between a hard and soft reset. The delay (if non-zero) will be applied the same way in either case.\r\n\r\n0x0000..0x03E8 - In milliseconds.")]
        public UInt16 Delay
        {
            get { return pDelay; }
            set { pDelay = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pDelay);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Delay_Sleep(OSCC2540 parent)
            : base(0xFC1C, parent)
        {

        }
    }
    public class BLE_Command_HCI_Reset_System : BLE_Command
    {
        private byte pMode;

        [CategoryAttribute("System"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
             Description("This command is used to issue a hard or soft system reset. A hard reset is caused by a watchdog timer timeout, while a soft reset is caused by resetting the PC to zero.\r\n\r\n0x00 - HCI_EXT_RESET_SYSTEM_HARD\r\n0x01 - HCI_EXT_RESET_SYSTEM_SOFT")]
        public byte Mode
        {
            get { return pMode; }
            set { pMode = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pMode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Reset_System(OSCC2540 parent)
            : base(0xFC1D, parent)
        {

        }
    }
    public class BLE_Command_HCI_Overlapped_Processing_Command : BLE_Command
    {
        private byte pMode;

        [CategoryAttribute("System"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
             Description("This command is used to enable or disable overlapped processing. The default is disabled.\r\n\r\n0x00 - HCI_EXT_DISABLE_OVERLAPPED_PROCESSING\r\n0x01 - HCI_EXT_ENABLE_OVERLAPPED_PROCESSING")]
        public byte Mode
        {
            get { return pMode; }
            set { pMode = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pMode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Overlapped_Processing_Command(OSCC2540 parent)
            : base(0xFC1E, parent)
        {

        }
    }
    public class BLE_Command_HCI_Number_Completed_Packets_Limit : BLE_Command
    {
        private byte pLimit;
        private byte pflushOnEvent;

        [CategoryAttribute("System"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
             Description("This command is used to set the limit on the minimum number of complete packets before a Number of Completed Packets event is returned by the Controller.\r\nIf the limit is not reached by the end of a connection event, then the Number of Completed Packets event will be returned (if non-zero) based on the flushOnEvt flag.\r\nThe limit can be set from one to the maximum number of HCI buffers (please see the LE Read Buffer Size command in the Bluetooth Core specification).\r\nThe default limit is one; the default flushOnEvt flag is FALSE.\r\n\r\n0x01..<max buffers> - Where <max buffers> is returned by HCI_LE_ReadBufSizeCmd.")]
        public byte Limit
        {
            get { return pLimit; }
            set { pLimit = value; }
        }
        [CategoryAttribute("System"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
             Description("This command is used to set the limit on the minimum number of complete packets before a Number of Completed Packets event is returned by the Controller.\r\nIf the limit is not reached by the end of a connection event, then the Number of Completed Packets event will be returned (if non-zero) based on the flushOnEvt flag.\r\nThe limit can be set from one to the maximum number of HCI buffers (please see the LE Read Buffer Size command in the Bluetooth Core specification).\r\nThe default limit is one; the default flushOnEvt flag is FALSE.\r\n\r\n0x00 - Only return a Number of Completed Packets event when the number of completed packets is greater than or equal to the limit.\r\n0x01 - Return a Number of Complete Packets event if the number of completed packets is less than the limit.")]
        public byte Flush_On_Event
        {
            get { return pflushOnEvent; }
            set { pflushOnEvent = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pLimit);
            data.Add(pflushOnEvent);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_HCI_Number_Completed_Packets_Limit(OSCC2540 parent)
            : base(0xFC1F, parent)
        {

        }
    }

    /////////////////////////////////////////////////////////////////
    //  GAP Commands
    /////////////////////////////////////////////////////////////////

    public class BLE_Command_GAP_Device_Init : BLE_Command
    {
        private byte pProfileRole;
        private byte pMaxScanResponses;
        private byte[] pIRK = new byte[16];
        private byte[] pCSRK = new byte[16];
        private UInt32 psignCounter;

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
             Description("This command is used to setup the device in a GAP Role and should only be called once per reboot. To enable multiple combinations setup multiple GAP Roles (profileRole parameter).\r\n\r\nMultiple Role settings examples:\r\n\r\n- GAP_PROFILE_PERIPHERAL and GAP_PROFILE_BROADCASTER – allows a connection and advertising (non-connectable) at the same time.\r\n- GAP_PROFILE_PERIPHERAL and GAP_PROFILE_OBSERVER – allows a connection (with master) and scanning at the same time.\r\n- GAP_PROFILE_PERIPHERAL, GAP_PROFILE_OBSERVER and GAP_PROFILE_BROADCASTER – allows a connection (with master) and scanning or advertising at the same time.\r\n- GAP_PROFILE_CENTRAL and GAP_PROFILE_BROADCASTER – allows connections and advertising (non-connectable) at the same time.\r\n\r\n0x01 - GAP_PROFILE_BROADCASTER\r\n0x02 - GAP_PROFILE_OBSERVER\r\n0x04 - GAP_PROFILE_PERIPHERAL\r\n0x08 - GAP_PROFILE_CENTRAL")]
        public byte ProfileRole
        {
            get { return pProfileRole; }
            set { pProfileRole = value; }
        }
        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
             Description("Central or Observer only: The device will allocate buffer space for received advertisement packets. The default is 3. The larger the number, the more RAM that is needed and maintained.")]
        public byte maxScanResponses
        {
            get { return pMaxScanResponses; }
            set { pMaxScanResponses = value; }
        }
        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(SecurityKey16ByteHexTypeConverter)),
             Description("16 byte Identity Resolving Key (IRK). If this value is all 0’s, the GAP will randomly generate all 16 bytes. This key is used to generate Resolvable Private Addresses.")]
        public byte[] IRK
        {
            get { return pIRK; }
            set { pIRK = value; }
        }

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(SecurityKey16ByteHexTypeConverter)),
            Description("16 byte Connection Signature Resolving Key (CSRK). If this value is all 0’s, the GAP will randomly generate all 16 bytes. This key is used to generate data Signatures.")]
        public byte[] CSRK
        {
            get { return pCSRK; }
            set { pCSRK = value; }
        }
        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt32HexTypeConverter)),
             Description("0x00000000 – 0xFFFFFFFF - 32 bit Signature Counter. Initial signature counter.")]
        public UInt32 signCounter
        {
            get { return psignCounter; }
            set { psignCounter = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pProfileRole);
            data.Add(pMaxScanResponses);
            data.Add(pIRK);
            data.Add(pCSRK);
            data.Add(psignCounter);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_GAP_Device_Init(OSCC2540 parent)
            : base(0xFE00, parent)
        {

        }
    }
    public class BLE_Command_GAP_Configure_Device_Address : BLE_Command
    {
        private byte pAddrType;
        private byte[] pAddress = new byte[6];

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(BTAddrConverter)),
            Description("Send this command to set the device’s address type. Intended address. Only used with ADDRTYPE_STATIC or ADDRTYPE_PRIVATE_NONRESOLVE.\r\n\r\nXX:XX:XX:XX:XX:XX")]
        public byte[] Address
        {
            get { return pAddress; }
            set { pAddress = value; }
        }

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
             Description("Send this command to set the device’s address type. If ADDRTYPE_PRIVATE_RESOLVE is selected, the address will change periodically.\r\n\r\n0 - ADDRTYPE_PUBLIC\r\n1 - ADDRTYPE_STATIC\r\n2 - ADDRTYPE_PRIVATE_NONRESOLVE\r\n3 - ADDRTYPE_PRIVATE_RESOLVE")]
        public byte AddressType
        {
            get { return pAddrType; }
            set { pAddrType = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pAddrType);
            data.Add(pAddress);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_GAP_Configure_Device_Address(OSCC2540 parent)
            : base(0xFE03, parent)
        {

        }
    }
    public class BLE_Command_GAP_Device_Discovery_Request : BLE_Command
    {
        private byte pMode;
        private byte pActive;
        private byte pwhileList;

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("Send this command to start a scan for advertisement packets. This command is valid for a central or a peripheral device.\r\n\r\n0 - Non-Discoverable Scan\r\n1 - General Mode Scan\r\n2 - Limited Mode Scan\r\n3 - Scan for all devices")]
        public byte Mode
        {
            get { return pMode; }
            set { pMode = value; }
        }

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
             Description("Send this command to start a scan for advertisement packets. This command is valid for a central or a peripheral device.\r\n\r\n0 - Turn off active scanning (SCAN_REQ)\r\n1 - Turn on active scanning (SCAN_REQ)")]
        public byte ActiveScan
        {
            get { return pActive; }
            set { pActive = value; }
        }
        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
             Description("Send this command to start a scan for advertisement packets. This command is valid for a central or a peripheral device.\r\n\r\n0 - Don’t use the white list during a scan\r\n1 - Use the white list during a scan")]
        public byte WhiteList
        {
            get { return pwhileList; }
            set { pwhileList = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pMode);
            data.Add(pActive);
            data.Add(pwhileList);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_GAP_Device_Discovery_Request(OSCC2540 parent)
            : base(0xFE04, parent)
        {

        }
    }
    public class BLE_Command_GAP_Device_Discovery_Cancel : BLE_Command
    {
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_GAP_Device_Discovery_Cancel(OSCC2540 parent)
            : base(0xFE05, parent)
        {

        }
    }
    public class BLE_Command_GAP_Make_Discoverable : BLE_Command
    {
        private byte pEventType;
        private byte pinitiatorAddrType;
        private byte[] pinitiatorAddr = new byte[6];
        private byte pChannelMap;
        private byte pfilterPolicy;

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("Send this command to start the device advertising.\r\n\r\n0 - Connectable undirected advertisement\r\n1 - Connectable directed advertisement\r\n2 - Discoverable undirected advertisement\r\n3 - Non-connectable undirected advertisement")]
        public byte EventType
        {
            get { return pEventType; }
            set { pEventType = value; }
        }
        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("Send this command to start the device advertising.\r\n\r\n0 - ADDRTYPE_PUBLIC\r\n1 - ADDRTYPE_STATIC\r\n2 - ADDRTYPE_PRIVATE_NONRESOLVE\r\n3 - ADDRTYPE_PRIVATE_RESOLVE")]
        public byte InitiatorAddressType
        {
            get { return pinitiatorAddrType; }
            set { pinitiatorAddrType = value; }
        }
        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(BTAddrConverter)),
            Description("Send this command to start the device advertising.\r\n\r\nIntended address. Only used for directed advertisments")]
        public byte[] InitiatorAddress
        {
            get { return pinitiatorAddr; }
            set { pinitiatorAddr = value; }
        }
        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("Send this command to start the device advertising.\r\n\r\n0 - Channel 37\r\n1 - Channel 38\r\n2 - Channel 39\r\n3 – 7 - reserved")]
        public byte ChannelMap
        {
            get { return pChannelMap; }
            set { pChannelMap = value; }
        }
        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("Send this command to start the device advertising.\r\n\r\n0 - Allow scan requests from any, allow connect request from any.\r\n1 - Allow scan requests from white list only, allow connect request from any.\r\n2 - Allow scan requests from any, allow connect request from white list only.\r\n3 - Allow scan requests from white list only, allow connect requests from white list only.\r\n4 – 0xFF - reserved")]
        public byte FilterPolicy
        {
            get { return pfilterPolicy; }
            set { pfilterPolicy = value; }
        }
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pEventType);
            data.Add(pinitiatorAddrType);
            data.Add(pinitiatorAddr);
            data.Add(pChannelMap);
            data.Add(pfilterPolicy);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_GAP_Make_Discoverable(OSCC2540 parent)
            : base(0xFE06, parent)
        {

        }
    }
    public class BLE_Command_GAP_Update_Advertising_Data : BLE_Command
    {
        private byte padType;
        private List<byte> padvertData = new List<byte>();

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("Send this command to set the raw advertising or scan response data.\r\n\r\n0 - SCAN_RSP data\r\n1 - Advertisement data")]
        public byte AdvertisementType
        {
            get { return padType; }
            set { padType = value; }
        }

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false),
            Description("Send this command to set the raw advertising or scan response data.\r\n\r\nLength of the advertData field (in octets)")]
        public byte DataLength
        {
            get
            {
                return (byte)(padvertData.Count);
            }
        }

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false),
            Description("Send this command to set the raw advertising or scan response data.\r\n\r\nRaw advertising data")]
        public List<byte> AdvertisingData
        {
            get { return padvertData; }
            set { padvertData = value; }
        }

        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(padType);
            data.Add(DataLength);
            data.Add(padvertData.ToArray());
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_GAP_Update_Advertising_Data(OSCC2540 parent)
            : base(0xFE07, parent)
        {

        }
    }
    public class BLE_Command_GAP_End_Discoverable : BLE_Command
    {
        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_GAP_End_Discoverable(OSCC2540 parent)
            : base(0xFE08, parent)
        {

        }
    }
    public class BLE_Command_GAP_Establish_Link_Request : BLE_Command
    {
        private byte phighDutyCycle;
        private byte pwhiteList;
        private byte paddrTypePeer;
        private byte[] ppeerAddr = new byte[6];

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(BTAddrConverter)),
            Description("Send this command to initiate a connection with a peripheral device. Only central devices can issue this command.\r\n\r\nXX:XX:XX:XX:XX:XX - Peripheral address to connect with.")]
        public byte[] PeerAddress
        {
            get { return ppeerAddr; }
            set { ppeerAddr = value; }
        }

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("Send this command to initiate a connection with a peripheral device. Only central devices can issue this command.\r\nA central device may use high duty cycle scan parameters in order to achieve low latency connection time with a peripheral device using directed link establishment.\r\n\r\n0 - disabled\r\n1 - enabled")]
        public byte HighDutyCycle
        {
            get { return phighDutyCycle; }
            set { phighDutyCycle = value; }
        }

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("Send this command to initiate a connection with a peripheral device. Only central devices can issue this command.\r\n\r\n0 - Don’t use the white list\r\n1 - Only connect to a device in the white list")]
        public byte WhiteList
        {
            get { return pwhiteList; }
            set { pwhiteList = value; }
        }
        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt8HexTypeConverter)),
            Description("Send this command to initiate a connection with a peripheral device. Only central devices can issue this command.\r\n\r\n0 - ADDRTYPE_PUBLIC\r\n1 - ADDRTYPE_STATIC\r\n2 - ADDRTYPE_PRIVATE_NONRESOLVE\r\n3 - ADDRTYPE_PRIVATE_RESOLVE")]
        public byte AddressTypePeer
        {
            get { return paddrTypePeer; }
            set { paddrTypePeer = value; }
        }

        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(phighDutyCycle);
            data.Add(pwhiteList);
            data.Add(paddrTypePeer);
            data.Add(ppeerAddr);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_GAP_Establish_Link_Request(OSCC2540 parent)
            : base(0xFE09, parent)
        {

        }
    }
    public class BLE_Command_GAP_Terminate_Link_Request : BLE_Command
    {
        private UInt16 pconnHandle;

        [CategoryAttribute("GAP"), ReadOnlyAttribute(false), TypeConverter(typeof(UInt16HexTypeConverter)),
            Description("Send this command to terminate a connection link, a connection request or all connected links.\r\n\r\n0 – 0xFFFD - Existing connection handle to terminate\r\n0xFFFE - Terminate the “Establish Link Request”\r\n0xFFFF - Terminate all links")]
        public UInt16 ConnectionHandle
        {
            get { return pconnHandle; }
            set { pconnHandle = value; }
        }

        public override void SendCommand()
        {
            List<object> data = new List<object>();
            data.Add(hdr_event);
            data.Add(hdr_status);
            data.Add((byte)0xAA);
            data.Add(OPCode);
            data.Add(pconnHandle);
            this.parent.SendHM10Command(data);
        }
        public BLE_Command_GAP_Terminate_Link_Request(OSCC2540 parent)
            : base(0xFE0A, parent)
        {

        }
    }




    public class HM10SourceLine
    {
        public UInt32 relativeaddress;
        public UInt32 absoluteaddress;
        public bool ASMLine;
        public string linetext;
    }
    public class HM10SourceFile
    {
        public bool OnlyHexInString(string test)
        {
            // For C-style hex notation (0xFF) you can use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }
        public string filename;
        public string sourcefile;
        public IList<HM10SourceLine> SourceLines;
        public HM10SourceFile(string pfile)
        {
            filename = pfile;
            SourceLines = new List<HM10SourceLine>();
        }
        public void AddLine(string line)
        {
            if (line.Length > 0)
            {
                HM10SourceLine newline = new HM10SourceLine();
                newline.linetext = line;

                string[] splits = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                splits.ToString();
                if (splits[0] == "\\") // It's an ASM line
                    newline.ASMLine = true;
                else
                    newline.ASMLine = false;

                if (newline.ASMLine)
                {
                    if (OnlyHexInString(splits[1]))
                        newline.relativeaddress = Convert.ToUInt32(splits[1], 16);
                }
                else
                    newline.relativeaddress = 0x0000;

                SourceLines.Add(newline);
            }
        }
    }
    public class HM10DebugFunction
    {
        public UInt32 address;
        public UInt32 MaxAddress;
        public string name;
        public string section;
        public HM10SourceFile sourcefile;
        public HM10DebugModule parentmodule;
        public HM10DebugFunction(HM10DebugModule pmod, UInt32 paddr, string pname, string psect)
        {
            parentmodule = pmod;
            address = paddr;
            name = pname;
            section = psect;
        }
    }
    public class HM10DebugModule
    {
        public string name;
        public string filename;
        public IList<HM10DebugFunction> Functions;
        public HM10DebugModule(string pname, string pfilename)
        {
            name = pname;
            filename = pfilename;
            Functions = new List<HM10DebugFunction>();
        }
        public HM10DebugFunction FindFunction(string name)
        {
            foreach (HM10DebugFunction func in Functions)
            {
                if (func.name.CompareTo(name) == 0)
                    return func;
            }
            return null;
        }
    }
    public class SerialPacket
    {
        public byte[] payload;
        public bool ResponeRecv;
        public bool ResponseTimeout;
        public SerialPacket(byte[] data)
        {
            payload = data;
        }
        public SerialPacket(ushort pcmd, byte[] ppayload)
        {
            ushort length;

            length = 4;
            if (ppayload != null)
                length += Convert.ToUInt16(ppayload.Length);

            payload = new byte[length];
            payload[0] = BitConverter.GetBytes(length)[0]; // Little Endian
            payload[1] = BitConverter.GetBytes(length)[1];
            payload[2] = BitConverter.GetBytes(pcmd)[0];
            payload[3] = BitConverter.GetBytes(pcmd)[1];
            UInt16 ctr = 0;
            if (ppayload != null)
            {
                foreach (byte db in ppayload)
                    payload[0x04 + ctr++] = db;
            }
            ResponeRecv = false;
            ResponseTimeout = false;
        }
    }
}
public static class SOExtension
{
    public static IEnumerable<TreeNode> FlattenTree(this TreeView tv)
    {
        return FlattenTree(tv.Nodes);
    }

    public static IEnumerable<TreeNode> FlattenTree(this TreeNodeCollection coll)
    {
        return coll.Cast<TreeNode>()
                    .Concat(coll.Cast<TreeNode>()
                                .SelectMany(x => FlattenTree(x.Nodes)));
    }
}
public class UInt64BTAddr : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertFrom(context, sourceType);
        }
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertTo(context, destinationType);
        }
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value.GetType() == typeof(UInt64))
        {
            UInt64 data = (UInt64)value;
            string str = "";
            str += string.Format("{0:X2}:", ((data >> 40) & 0xFF));
            str += string.Format("{0:X2}:", ((data >> 32) & 0xFF));
            str += string.Format("{0:X2}:", ((data >> 24) & 0xFF));
            str += string.Format("{0:X2}:", ((data >> 16) & 0xFF));
            str += string.Format("{0:X2}:", ((data >> 8) & 0xFF));
            str += string.Format("{0:X2}", ((data >> 0) & 0xFF));
            return string.Format(str);
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value.GetType() == typeof(string))
        {
            string input = (string)value;

            string[] data = input.Split(':');
            string dout = "";
            foreach (string str in data)
            {
                dout += str;
            }

            return UInt64.Parse(dout, System.Globalization.NumberStyles.HexNumber, culture);
        }
        else
        {
            return base.ConvertFrom(context, culture, value);
        }
    }
} // UInt32HexTypeConverter used by grid to display hex.
public class UInt64HexTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertFrom(context, sourceType);
        }
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertTo(context, destinationType);
        }
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value.GetType() == typeof(UInt64))
        {
            return string.Format("0x{0:X16}", value);
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value.GetType() == typeof(string))
        {
            string input = (string)value;

            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                input = input.Substring(2);
            }

            return UInt64.Parse(input, System.Globalization.NumberStyles.HexNumber, culture);
        }
        else
        {
            return base.ConvertFrom(context, culture, value);
        }
    }
} // UInt64HexTypeConverter used by grid to display hex.
public class UInt32HexTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertFrom(context, sourceType);
        }
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertTo(context, destinationType);
        }
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value.GetType() == typeof(UInt32))
        {
            return string.Format("0x{0:X8}", value);
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value.GetType() == typeof(string))
        {
            string input = (string)value;

            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                input = input.Substring(2);
            }

            return UInt32.Parse(input, System.Globalization.NumberStyles.HexNumber, culture);
        }
        else
        {
            return base.ConvertFrom(context, culture, value);
        }
    }
} // UInt32HexTypeConverter used by grid to display hex.
public class UInt16HexTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertFrom(context, sourceType);
        }
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertTo(context, destinationType);
        }
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value.GetType() == typeof(UInt16))
        {
            return string.Format("0x{0:X4}", value);
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value.GetType() == typeof(string))
        {
            string input = (string)value;

            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                input = input.Substring(2);
            }

            return UInt16.Parse(input, System.Globalization.NumberStyles.HexNumber, culture);
        }
        else
        {
            return base.ConvertFrom(context, culture, value);
        }
    }
} // UInt16HexTypeConverter used by grid to display hex.
public class UInt8HexTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertFrom(context, sourceType);
        }
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertTo(context, destinationType);
        }
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value.GetType() == typeof(byte))
        {
            return string.Format("0x{0:X2}", value);
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value.GetType() == typeof(string))
        {
            string input = (string)value;

            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                input = input.Substring(2);
            }

            return byte.Parse(input, System.Globalization.NumberStyles.HexNumber, culture);
        }
        else
        {
            return base.ConvertFrom(context, culture, value);
        }
    }
} // UInt8HexTypeConverter used by grid to display hex.
public class SecurityKey16ByteHexTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertFrom(context, sourceType);
        }
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(byte[]))
        {
            return true;
        }
        else
        {
            return base.CanConvertTo(context, destinationType);
        }
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value.GetType() == typeof(byte[]))
        {
            string str = "";
            byte[] data = (byte[])value;
            for (int t = 0; t < data.Length; t++)
            {
                str += string.Format("{0:X2}", data[t]);
                if (t < data.Length - 1)
                    str += ":";
            }
            return str;
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value.GetType() == typeof(string))
        {
            string input = (string)value;

            string[] tokens = input.Split(':');
            byte[] data = new byte[tokens.Length];
            for (int t = 0; t < tokens.Length; t++)
            {
                data[t] = byte.Parse(tokens[t], System.Globalization.NumberStyles.HexNumber, culture);
            }
            return data;
        }
        else
        {
            return base.ConvertFrom(context, culture, value);
        }
    }
} // SecurityKey16ByteHexTypeConverter used by grid to display hex.
public class BTAddrConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }
        else
        {
            return base.CanConvertFrom(context, sourceType);
        }
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(byte[]))
        {
            return true;
        }
        else
        {
            return base.CanConvertTo(context, destinationType);
        }
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string) && value.GetType() == typeof(byte[]))
        {
            string str = "";
            byte[] data = (byte[])value;
            for (int t = 0; t < data.Length; t++)
            {
                str += string.Format("{0:X2}", data[t]);
                if (t < data.Length - 1)
                    str += ":";
            }
            return str;
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value.GetType() == typeof(string))
        {
            string input = (string)value;

            string[] tokens = input.Split(':');
            byte[] data = new byte[tokens.Length];
            for (int t = 0; t < tokens.Length; t++)
            {
                data[t] = byte.Parse(tokens[t], System.Globalization.NumberStyles.HexNumber, culture);
            }
            return data;
        }
        else
        {
            return base.ConvertFrom(context, culture, value);
        }
    }
} // SecurityKey16ByteHexTypeConverter used by grid to display hex.
