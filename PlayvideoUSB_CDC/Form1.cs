using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO.Ports;
namespace PlayvideoUSB_CDC
{
    public partial class Form1 : Form
    {
        int x0, y0, x1, y1,type;
        int width=64, height=32;
        double gamma=2.8;
        int max_in=255;
        int max_out=255;
        int bitpwm = 8;
        Boolean play = false;
        public Form1()
        {
            InitializeComponent();
            MouseHook.MouseAction_move += new EventHandler(mouse_move);
            MouseHook.MouseAction_click += new EventHandler(mouse_click);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] ComList = SerialPort.GetPortNames();
            int[] ComNumberList = new int[ComList.Length];
            for (int i = 0; i < ComList.Length; i++)
            {
                ComNumberList[i] = int.Parse(ComList[i].Substring(3));
            }
            Array.Sort(ComNumberList);
            foreach (int ComNumber in ComNumberList)
            {
                cbxComlist.Items.Add("COM" + ComNumber.ToString());
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            play = !play;
            if (play == true)
            {
                timer1.Enabled = true;
                button2.Text = "Stop";
            }
            else
            {
                timer1.Enabled = false;
                button2.Text = "Play";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Bitmap bmp_data = new Bitmap(width,height);
            try
            {
                Bitmap bmp = new Bitmap(x1 - x0 + 1, y1 - y0 + 1);
                Graphics gr = Graphics.FromImage(bmp);
                gr.CopyFromScreen(x0, y0, 0, 0, bmp.Size);
                bmp_data  = ResizeBitmap(bmp, 192, 96);
                pictureBox1.Image = bmp_data;
            }
            catch
            {

            }
            if (serialPort.IsOpen) send_data(bmp_data);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (cbxComlist.Text == "")
            {
                MessageBox.Show("Vui lòng chọn cổng com", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                button3.Text = "Kết nối";
                cbxComlist.Enabled = true;
            }
            else
            {
                serialPort.PortName = cbxComlist.Text;
                try
                {
                    serialPort.Open();
                    button3.Text = "Ngắt kết nối";
                    cbxComlist.Enabled = false;
                }
                catch
                {
                    MessageBox.Show("Không thể mở cổng " + serialPort.PortName, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            width = Int16.Parse(textBox7.Text);
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            height = Int16.Parse(textBox8.Text);
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            bitpwm = Int16.Parse(textBox6.Text);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            gamma = Convert.ToDouble(textBox3.Text);
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            max_out = Int16.Parse(textBox4.Text);
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            max_in = Int16.Parse(textBox5.Text);
        }

        private void mouse_move(object sender, EventArgs e)
        {
            if (type == 1)
            {
                x0 = System.Windows.Forms.Control.MousePosition.X;
                y0 = System.Windows.Forms.Control.MousePosition.Y;
                textBox1.Text = x0.ToString() + "," + y0.ToString();
            }
            else if (type == 2)
            {
                x1 = System.Windows.Forms.Control.MousePosition.X;
                y1 = System.Windows.Forms.Control.MousePosition.Y;
                textBox2.Text = x1.ToString() + "," + y1.ToString();
            }
        }
        private void mouse_click(object sender, EventArgs e)
        {
            if (type == 1)
                type = 2;             
            else
            {
                type = 0;
                button1.Enabled = true;
                Bitmap bmp = new Bitmap(x1 - x0 + 1, y1 - y0 + 1);
                Graphics gr = Graphics.FromImage(bmp);
                gr.CopyFromScreen(x0, y0, 0, 0, bmp.Size);
                pictureBox1.Image = ResizeBitmap(bmp, 192, 96);
                MouseHook.stop();
            }    
        }
        private void button1_Click(object sender, EventArgs e)
        {
            type = 1;
            timer1.Enabled = false;
            button2.Text = "Play";
            x0 = y0 = x1 = y1 = 0;
            textBox1.Text = "";
            textBox2.Text = "";
            button1.Enabled = false;
            MouseHook.Start();
        }

        //resize imgae
        public Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }
        void send_data(Bitmap bitmap)
        {
            Bitmap bmp = ResizeBitmap(bitmap,width,height);
            byte[] bytestosend = new byte[64];
            for (int i = 0; i < 3; i++) //3 màu RGB 
            {
                for (int j = 0; j < height; j++)        // quét từng hàng ngang 1
                {
                    for (int index = 0; index < width / 64; index++)  //quét từng tấm 1
                    {
                        for (int k = 0; k < 64; k++)                        //1 lần gửi 64 byte
                        {
                            Color curColor = bmp.GetPixel(k + (index * 64), j);

                            if (i == 0) bytestosend[k] = curColor.R;
                            else if (i == 1) bytestosend[k] = curColor.G;
                            else bytestosend[k] = curColor.B;              //lấy ra màu của pixel

                            if (checkBox3.Checked == true) bytestosend[k] = (byte)(Math.Pow((double)bytestosend[k] / (double)max_in, gamma) * max_out + 0.5);        //tính toán gamma
                        }
                        serialPort.Write(bytestosend, 0, 64); //gửi 1 gói tin đi
                    }
                }
            }
        }
    }

    public static class MouseHook
    {
        public static event EventHandler MouseAction_click = delegate { };
        public static event EventHandler MouseAction_move = delegate { };

        public static void Start()
        {
            _hookID = SetHook(_proc);


        }
        public static void stop()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                  GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
          int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                MouseAction_click(null, new EventArgs());
            }
            if (nCode >= 0 && MouseMessages.WM_MOUSEMOVE == (MouseMessages)wParam)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                MouseAction_move(null, new EventArgs());
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
          LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
          IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


    }
}
