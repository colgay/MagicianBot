using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using Win32Library;
using FastBitmapLib;
using Interceptor;
using System.Threading;

namespace MagicianBubbling
{
    public partial class Form1 : Form
    {
        private Input _Input;
        private bool _IsWorking = false;
        private IntPtr _Handle = IntPtr.Zero;
        private int _TitlebarSize = 0;
        private int _BorderSize = 0;
        private bool _RestoreHP = false;
        private bool _RestoreMP = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _Input = new Input();
            _Input.KeyboardFilterMode = KeyboardFilterMode.All;
            _Input.Load();
        }

        private void buttonToggle_Click(object sender, EventArgs e)
        {
            if (_IsWorking)
                StopWorking();
            else
                StartWorking();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            CheckGameWindow();
            //_Input.SendKeys(Interceptor.Keys.A);

            // Take a screenshot with a specific position and size (HP bar)
            Bitmap bmp = CaptureFromGame(218, 588, 104, 1, PixelFormat.Format32bppArgb); // save it as BMP

            using (var hpbar = bmp.FastLock())
            {
                // HP alert color detected
                if (hpbar.GetPixel(0, 0) == Color.FromArgb(255, 255, 0, 0))
                {
                    // Restore HP
                    _RestoreHP = true;
                }
                else
                {
                    Color grey = Color.FromArgb(255, 190, 190, 190);

                    // Get current HP value by looping the HP Bar bmp
                    for (int x = hpbar.Width - 1; x >= 4; x--)
                    {
                        // Check if current pixel isn't GREY color
                        if (hpbar.GetPixel(x, 0) != grey)
                        {
                            int hp = x + 1 - 4; // Calculate the HP value
                            if (hp <= 40) // if HP is lower than
                            {
                                // Need restore
                                _RestoreHP = true;
                            }
                            else if (_RestoreHP && hp >= 80) // Check if HP is enough
                            {
                                // We don't need restore right now
                                _RestoreHP = false;
                            }

                            // Show HP value to the program
                            label2.Text = "HP:" + hp;
                            break;
                        }
                    }
                }

                // Restoring HP
                if (_RestoreHP)
                {
                    _Input.SendKeys(Interceptor.Keys.CommaLeftArrow);
                    Thread.Sleep(250); // Add some delay for current thread (Timer thread)
                }
            }

            // Release the BMP memory
            bmp.Dispose();
            
            // Now take a screenshot for MP bar
            bmp = CaptureFromGame(330, 588, 105, 1, PixelFormat.Format32bppArgb);

            using (var mpbar = bmp.FastLock())
            {
                // MP alert color detected
                if (mpbar.GetPixel(104, 0) == Color.FromArgb(255, 255, 0, 0))
                {
                    // We need to restore MP
                    _RestoreMP = true;
                }
                else
                {
                    Color grey = Color.FromArgb(255, 190, 190, 190);

                    for (int x = mpbar.Width - 5 - 1; x >= 0; x--)
                    {
                        if (mpbar.GetPixel(x, 0) != grey)
                        {
                            int mp = x + 1; // Calculate the MP value
                            if (mp <= 30)
                            {
                                // Need restore
                                _RestoreMP = true;
                            }
                            else if (_RestoreMP && mp >= 75) // Check if MP is enough
                            {
                                // Don't need restore
                                _RestoreMP = false;
                            }

                            // Show MP
                            label3.Text = "MP:" + mp;
                            break;
                        }
                    }

                    // Let's restore MP
                    if (_RestoreMP)
                    {
                        _Input.SendKeys(Interceptor.Keys.PeriodRightArrow);
                        Thread.Sleep(250); // Delay
                    }
                }
            }

            bmp.Dispose(); // Do I really need this?
        }

        private void StartWorking()
        {
            _Handle = Win32.FindWindow("MapleStoryClass", null); // Find game window

            if (CheckGameWindow() > 0) // Check if game window is valid
            {
                timer1.Enabled = true;

                _IsWorking = true;

                Win32.SetForegroundWindow(_Handle); // Set MapleStory as foreground window

                labelStatus.ForeColor = Color.Green; // Green hat
                labelStatus.Text = "運行中...";

                buttonToggle.Text = "停止";

                // Get titlebar and border color of game window
                RECT rect1, rect2;
                Win32.GetClientRect(_Handle, out rect1);
                Win32.GetWindowRect(_Handle, out rect2);

                Rectangle cRect = new Rectangle(rect1.Left, rect1.Top, rect1.Right - rect1.Left, rect1.Bottom - rect1.Top);
                Rectangle wRect = new Rectangle(rect2.Left, rect2.Top, rect2.Right - rect2.Left, rect2.Bottom - rect2.Top);

                _BorderSize = (wRect.Width - cRect.Width) / 2;
                _TitlebarSize = (wRect.Height - cRect.Height) - _BorderSize;
            }
        }

        private void StopWorking()
        {
            _IsWorking = false;

            timer1.Enabled = false;

            labelStatus.ForeColor = Color.Red;
            labelStatus.Text = "已停止";

            buttonToggle.Text = "開始";
        }

        private int CheckGameWindow()
        {
            if (Win32.IsWindow(_Handle)) // Window is valid
            {
                if (Win32.GetForegroundWindow() != _Handle) // Foreground window is not the game
                {
                    labelStatus.ForeColor = Color.DarkOrange;
                    labelStatus.Text = "已暫停 (遊戲視窗不可見)";

                    return 2;
                }

                // Foreground window is the game
                labelStatus.ForeColor = Color.Green;
                labelStatus.Text = "運行中...";
                return 1;
            }

            // Unable to find the game window, stop the program.
            if (_IsWorking)
            {
                StopWorking();
            }

            labelStatus.Text = "已停止 (找不到遊戲視窗)";

            return 0;
        }

        // Take a screenshot in a specific size and position and output as Bitmap
        private Bitmap CaptureFromGame(int x, int y, int width, int height, PixelFormat format)
        {
            RECT rect;
            Win32.GetWindowRect(_Handle, out rect);

            Bitmap bmp = new Bitmap(width, height, format);

            using (Graphics gr = Graphics.FromImage(bmp))
            {
                gr.CopyFromScreen(rect.Left + _BorderSize + x, rect.Top + _TitlebarSize + y, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
            }

            return bmp;
        }
    }
}
