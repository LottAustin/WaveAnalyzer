using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WaveAnalyer
{
    public partial class Form1 : Form
    {
        // DLL calls
        [DllImport("winmm.dll")]
        public static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WAVEHDR lpWaveHdr, uint Size);
        [DllImport("winmm.dll")]
        public static extern int waveOutWrite(IntPtr hWaveOut, ref WAVEHDR lpWaveHdr, uint Size);
        [DllImport("winmm.dll", EntryPoint = "waveOutOpen", SetLastError = true)]
        public static extern int waveOutOpen(ref IntPtr t, uint id, ref WAVEFORMAT pwfx, IntPtr dwCallback, int dwInstance, int fdwOpen);
        [DllImport("winmm.dll", EntryPoint = "waveOutReset", SetLastError = true)]
        static extern uint waveOutReset(IntPtr hwi);

        // Data containers
        public WaveReader wave = null;
        public double[] right;
        public double[] left;
        public Complex[] A;
        public Complex[] A2 = null;
        public int range, offset;

        //C# playback variables
        //public SoundPlayer player;

        // win32 variables
        private static WAVEHDR pWaveHdr;
        public static IntPtr handle;
        private RecordDialog.AudioPlayBackDelegate waveOut;
        private GCHandle headerPin;

        // Delegats for UI concurency
        public delegate void graph(Complex[] A, Complex[] A2);
        public delegate void inverse(double[] s);
        public delegate void inverse2(double[] s);
        public delegate void enable(Button button2,
                                    Button button3,
                                    Button button4,
                                    Button PlayBtn,
                                    Button StopBtn,
                                    Button WndBtn,
                                    Button FilterBtn,
                                    ToolStripMenuItem exportToolStripMenuItem,
                                    ToolStripMenuItem copyToolStripMenuItem,
                                    ToolStripMenuItem cutToolStripMenuItem);
        public static graph dft;
        public inverse inv;
        public inverse2 inv2;
        public static enable btnEnble;

        /* 
         * Form1 constructor
         *
         *  Constructor for the main form
         *
         */
        public Form1()
        {
            InitializeComponent();
            dft = new graph(graphDFT);
            inv = new inverse(graphInverse);
            inv2 = new inverse2(graphInverse2);
            btnEnble = new enable(EnableButtons);
            chart1.MouseWheel += new MouseEventHandler(chData_MouseWheel);
            chart2.MouseWheel += new MouseEventHandler(chData2_MouseWheel);
            chart3.MouseWheel += new MouseEventHandler(chData3_MouseWheel);
            chart4.MouseWheel += new MouseEventHandler(chData4_MouseWheel);

            chart1.MouseClick += new MouseEventHandler(chart1_MouseClick);
            chart2.MouseClick += new MouseEventHandler(chart2_MouseClick);
            chart3.MouseClick += new MouseEventHandler(chart3_MouseClick);
            chart4.MouseClick += new MouseEventHandler(chart4_MouseClick);

            tableLayoutPanel1.RowStyles[1].Height = 0;
            tableLayoutPanel2.RowStyles[1].Height = 0;

            // Disable data relient buttons
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            PlayBtn.Enabled = false;
            StopBtn.Enabled = false;
            WndBtn.Enabled = false;
            FilterBtn.Enabled = false;
            exportToolStripMenuItem.Enabled = false;
            cutToolStripMenuItem.Enabled = false;
            copyToolStripMenuItem.Enabled = false;

            this.SetStyle(ControlStyles.DoubleBuffer |
                          ControlStyles.UserPaint |
                          ControlStyles.AllPaintingInWmPaint,
                          true);
            this.UpdateStyles();
        }

        /*
         * EnableButtons
         *
         * Function enables initally disabled buttons 
         * 
         * Parameter: 
         *      Button button2,
         *                        Button button3
         *                        Button button4
         *                        Button PlayBtn
         *                        Button StopBtn
         *                        Button WndBtn
         *                        Button FilterBtn
         *                        ToolStripMenuItem exportToolStripMenuItem
         *                        ToolStripMenuItem copyToolStripMenuItem
         *                        ToolStripMenuItem cutToolStripMenuItem
         *
         * return void
         */
        public void EnableButtons(Button button2,
                                  Button button3,
                                  Button button4,
                                  Button PlayBtn,
                                  Button StopBtn,
                                  Button WndBtn,
                                  Button FilterBtn,
                                  ToolStripMenuItem exportToolStripMenuItem,
                                  ToolStripMenuItem copyToolStripMenuItem,
                                  ToolStripMenuItem cutToolStripMenuItem)
        {
            // Disable data relient buttons
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            PlayBtn.Enabled = true;
            StopBtn.Enabled = true;
            WndBtn.Enabled = true;
            FilterBtn.Enabled = true;
            exportToolStripMenuItem.Enabled = true;
            cutToolStripMenuItem.Enabled = true;
            copyToolStripMenuItem.Enabled = true;
        }

        /*
         * menuStrip1_ItemClicked
         *
         *  Unused
         *
         * return void
         */
        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        /*
         * newToolStripMenuItem_Click
         *
         * Listener for the New button in The Menu strip, calls a new instance of RercordDialog 
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RecordDialog r = new RecordDialog(this);
            r.Show();
        }

        /*
         * Graph DFT function
         *
         * Graphing function for DTF, This does not graph the DC
         *
         * Parameter: 
         *      double[] s samples 
         *
         * return void
         */
        public void graphDFT(Complex[] A, Complex[] A2)
        {
            int n = A.Length;

            chart2.Series["Series1"].Points.Clear();

            if (A2 != null)
            {
                chart4.Series["Series1"].Points.Clear();
            }

            for (int i = 1; i < n; i++)
            {
                chart2.Series["Series1"].Points.Add(A[i].Magnitude);

                if(A2 != null)
                {
                    chart4.Series["Series1"].Points.Add(A2[i].Magnitude);
                }
            }

            chart2.Series["Series1"].ChartType = SeriesChartType.Column;
            chart2.Series["Series1"].Color = Color.Green;

            if(A2 != null)
            {
                chart4.Series["Series1"].ChartType = SeriesChartType.Column;
                chart4.Series["Series1"].Color = Color.Green;
            }
        }

        /*
         * Graph iDFT function
         *
         * Graphing fucntion for Inverse of DTF for Chart 1
         *
         * Parameter: 
         *      double[] s samples 
         *
         * return void
         */
        public void graphInverse(double[] s)
        {
            chart1.Series["Series1"].Points.Clear();
            chart1.Series["Series1"].Points.DataBindY(s);

            chart1.Series["Series1"].ChartType = SeriesChartType.FastLine;
            chart1.Series["Series1"].Color = Color.Green;
        }

        /*
         * Graph iDFT function 2
         *
         * Graphing fucntion for Inverse of DTF for chart3
         * 
         * Parameter: 
         *      double[] s samples 
         *
         * return void
         */
        public void graphInverse2(double[] s)
        {
            if (s != null)
            {
                chart3.Series["Series1"].Points.Clear();
                chart3.Series["Series1"].Points.DataBindY(s);

                chart3.Series["Series1"].ChartType = SeriesChartType.FastLine;
                chart3.Series["Series1"].Color = Color.Green;
            }
        }

        /*
         * button1_Click
         *
         * Listener for the Import button on the main interface 
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void button1_Click(object sender, EventArgs e)
        {
            openFile();
        }

        /*
         * button2_Click
         *
         * Listener for the Export button on the main interface 
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void button2_Click(object sender, EventArgs e)
        {
            saveFile();
        }

        /*
         * chData_MouseWheel
         *
         * Listener for the listener for chart1 when using the mouse wheel
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void chData_MouseWheel(object sender, MouseEventArgs e)
        {
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            try
            {
                    if (e.Delta < 0)
                    {
                        chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                    }

                    if (e.Delta > 0)
                    {
                        double xMin = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                        double xMax = chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum;

                        double posXStart = chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) - (xMax - xMin) / 4;
                        double posXFinish = chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) + (xMax - xMin) / 4;

                        chart1.ChartAreas[0].AxisX.ScaleView.Zoom(posXStart, posXFinish);
                    }
                }
                catch { }   
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = false;
        }

        /*
         * ch3Data_MouseWheel
         *
         * Listener for the listener for chart3 when using the mouse wheel
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void chData3_MouseWheel(object sender, MouseEventArgs e)
        {
            chart3.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            try
            {
                if (e.Delta < 0)
                {
                    chart3.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                }

                if (e.Delta > 0)
                {
                    double xMin = chart3.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                    double xMax = chart3.ChartAreas[0].AxisX.ScaleView.ViewMaximum;

                    double posXStart = chart3.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) - (xMax - xMin) / 4;
                    double posXFinish = chart3.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) + (xMax - xMin) / 4;

                    chart3.ChartAreas[0].AxisX.ScaleView.Zoom(posXStart, posXFinish);
                }
            }
            catch { }
            chart3.ChartAreas[0].AxisX.ScaleView.Zoomable = false;
        }

        /*
         * ch4Data_MouseWheel
         *
         * Listener for the listener for chart4 when using the mouse wheel
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void chData4_MouseWheel(object sender, MouseEventArgs e)
        {
            chart4.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            try
            {
                if (e.Delta < 0)
                {
                    chart4.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                }

                if (e.Delta > 0)
                {
                    double xMin = chart4.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                    double xMax = chart4.ChartAreas[0].AxisX.ScaleView.ViewMaximum;

                    double posXStart = chart4.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) - (xMax - xMin) / 4;
                    double posXFinish = chart4.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) + (xMax - xMin) / 4;

                    chart4.ChartAreas[0].AxisX.ScaleView.Zoom(posXStart, posXFinish);
                }
            }
            catch { }
            chart4.ChartAreas[0].AxisX.ScaleView.Zoomable = false;
        }

        /*
         * chart1_MouseClick
         *
         * Listener for chart1 click method, match chat1 selection with chart3's
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void chart1_MouseClick(object sender, MouseEventArgs e)
        {
            chart3.ChartAreas[0].CursorX.SelectionStart = chart1.ChartAreas[0].CursorX.SelectionStart;
            chart3.ChartAreas[0].CursorX.SelectionEnd = chart1.ChartAreas[0].CursorX.SelectionEnd;

            chart3.UpdateCursor();
        }

        /*
         * chart2_MouseClick
         *
         * Listener for chart2 click method, match chat2 selection with chart4's
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void chart2_MouseClick(object sender, MouseEventArgs e)
        {
            /*chart4.ChartAreas[0].CursorX.SelectionStart = chart2.ChartAreas[0].CursorX.SelectionStart;
            chart4.ChartAreas[0].CursorX.SelectionEnd = chart2.ChartAreas[0].CursorX.SelectionEnd;*/

            int bucket = (int)chart2.ChartAreas[0].CursorX.SelectionStart;
            int start, end;

            if(bucket < left.Length / 2)
            {
                start = bucket;
                end = A.Length - bucket;
            } else if(bucket > left.Length)
            {
                end = bucket;
                start = A.Length / 2 - bucket;
            } else
            {
                start = end = bucket;
            }

            chart4.ChartAreas[0].CursorX.SelectionStart = start;
            chart4.ChartAreas[0].CursorX.SelectionEnd = end;

            chart2.ChartAreas[0].CursorX.SelectionStart = start;
            chart2.ChartAreas[0].CursorX.SelectionEnd = end;

            chart4.UpdateCursor();
        }

        /*
         * chart3_MouseClick
         *
         * Listener for chart3 click method, match chat3 selection with chart1's
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void chart3_MouseClick(object sender, MouseEventArgs e)
        {
            chart1.ChartAreas[0].CursorX.SelectionStart = chart3.ChartAreas[0].CursorX.SelectionStart;
            chart1.ChartAreas[0].CursorX.SelectionEnd = chart3.ChartAreas[0].CursorX.SelectionEnd;

            chart1.UpdateCursor();
        }

        /*
         * chart4_MouseClick
         *
         * Listener for chart4 click method, match chat4 selection with chart2's
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void chart4_MouseClick(object sender, MouseEventArgs e)
        {
            int bucket = (int)chart4.ChartAreas[0].CursorX.SelectionStart;
            int start, end;

            if (bucket < left.Length / 2)
            {
                start = bucket;
                end = A.Length - bucket;
            }
            else if (bucket > left.Length)
            {
                end = bucket;
                start = A.Length / 2 - bucket;
            }
            else
            {
                start = end = bucket;
            }

            chart4.ChartAreas[0].CursorX.SelectionStart = start;
            chart4.ChartAreas[0].CursorX.SelectionEnd = end;

            chart2.ChartAreas[0].CursorX.SelectionStart = start;
            chart2.ChartAreas[0].CursorX.SelectionEnd = end;

            chart2.UpdateCursor();
        }

        /*
         * chData2_MouseWheel
         *
         * Listener for the listener for chart2 when using the mouse wheel
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void chData2_MouseWheel(object sender, MouseEventArgs e)
        {
            chart2.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            try
            {
                    if (e.Delta < 0)
                    {
                        chart2.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                    }

                    if (e.Delta > 0)
                    {
                        double xMin = chart2.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                        double xMax = chart2.ChartAreas[0].AxisX.ScaleView.ViewMaximum;

                        double posXStart = chart2.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) - (xMax - xMin) / 4;
                        double posXFinish = chart2.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) + (xMax - xMin) / 4;

                        chart2.ChartAreas[0].AxisX.ScaleView.Zoom(posXStart, posXFinish);
                    }
                }
                catch { }    
            chart2.ChartAreas[0].AxisX.ScaleView.Zoomable = false;
        }

        /*
         * openFile
         *
         * Method opens a wave file picked by user and graphs it
         *
         * return void
         */
        private void openFile()
        {
            OpenFileDialog openFile = new OpenFileDialog();

            openFile.Filter = "Wav Files|*.wav";
            openFile.Title = "Select a File";

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                left = null;
                right = null;
                wave = null;

                wave = new WaveReader();
                wave.openWav(openFile.FileName, out left, out right);

                this.Invoke(inv, new object[] { left });

                if(right != null)
                {
                    this.Invoke(inv2, new object[] { right });
                }

                if(wave.channels == 2)
                {
                    tableLayoutPanel1.RowStyles[1].Height = 50;
                    tableLayoutPanel2.RowStyles[1].Height = 50;
                } else
                {
                    tableLayoutPanel1.RowStyles[1].Height = 0;
                    tableLayoutPanel2.RowStyles[1].Height = 0;
                }

                if (left.Length < 500) {
                    offset = 0;
                    range = left.Length;
                } else
                {
                    offset = left.Length / 2 - 250;
                    range = 500;
                }
                window();
            }
        }

        /*
         * saveFile
         *
         * Method rebuilds the wave and saves it to the file user specifies (either new or existing)
         *
         * return void
         */
        private void saveFile()
        {
            SaveFileDialog saveFile = new SaveFileDialog();

            saveFile.Filter = "Wav Files|*.wav";
            saveFile.Title = "Save a wav File";
            saveFile.ShowDialog();

            if (saveFile.FileName != "")
            {
                wave.rebuildWav(left, right);
                wave.writeWav(saveFile.FileName);
            }
        }

        /*
         * importToolStripMenuItem_Click
         *
         * Listener for the menus Import button
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFile();
        }

        /*
         * exportToolStripMenuItem_Click
         *
         * Listener for the menus Export button
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFile();
        }

        /*
         * exitToolStripMenuItem_Click
         *
         * Listener for the menus Exit button, quites application when clicked
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /*
         * copyChart1
         *
         * copies user selected data on chart1 (time domain of signal)
         *
         * return void
         */
        private void copyChart1()
        {
            int start = (int)chart1.ChartAreas[0].CursorX.SelectionStart;
            int end = (int)chart1.ChartAreas[0].CursorX.SelectionEnd;
            int range, offset;

            if(start < end)
            {
                range = end - start;
                offset = start;
            } else
            {
                range = start - end;
                offset = end;
            }

            double[] selection = new double[range];
            double[] selection2  = null;
            if(right != null)
            {
                selection2 = new double[range];
            }

            for(int i = 0; i < range; i++)
            {
                selection[i] = left[i + offset];
                if(right != null)
                {
                    selection2[i] = right[i + offset];
                }
            }

            if(right != null)
            {
                double[] tmp = new double[selection.Length + selection2.Length];
                Array.Copy(selection, tmp, selection.Length);
                Array.Copy(selection2, 0, tmp, selection.Length, selection2.Length);
                Clipboard.SetData("Copy", tmp); 
            } else
            {
                Clipboard.SetData("Copy", selection);
            }

            //Clipboard.SetData("Copy2", selection2);
        }

        /*
         * pasteChart1
         *
         * paste user selected data on chart1 (time domain of signal)
         *
         * return void
         */
        private void pasteChart1()
        {
            double[] tmp = (double[])Clipboard.GetData("Copy");
            double[] paste = null;
            double[] paste2 = null;

            int point = (int)chart1.ChartAreas[0].CursorX.SelectionStart;

            if(right != null)
            {
                paste = new double[tmp.Length / 2];
                paste2 = new double[tmp.Length / 2];
                Array.Copy(tmp, paste, tmp.Length / 2);
                Array.Copy(tmp, tmp.Length / 2, paste2, 0, tmp.Length / 2);
            } else
            {
                paste = tmp;
            }

            double[] newWave = new double[paste.Length + left.Length];
            double[] newWave2 = null;

            if(right != null)
            {
                newWave2 = new double[paste2.Length + right.Length];
            }

            Array.Copy(left, 0, newWave, 0, point);
            Array.Copy(paste, 0, newWave, point, paste.Length);
            Array.Copy(left, point, newWave, point + paste.Length, left.Length - point);
            left = newWave;

            if (right != null)
            {
                Array.Copy(right, 0, newWave2, 0, point);
                Array.Copy(paste2, 0, newWave2, point, paste2.Length);
                Array.Copy(right, point, newWave2, point + paste2.Length, right.Length - point);
                right = newWave2;
            }
            
            this.Invoke(inv, new object[] { left });

            if(right != null)
            {
                this.Invoke(inv2, new object[] { right });
            }
        }

        /*
         * cutChart1
         *
         * copies user selected data on chart1 (time domain of signal) then removes it from the chart
         *
         * return void
         */
        private void cutChart1()
        {
            int start = (int)chart1.ChartAreas[0].CursorX.SelectionStart;
            int end = (int)chart1.ChartAreas[0].CursorX.SelectionEnd;
            int range, offset;

            if (start < end)
            {
                range = end - start;
                offset = start;
            }
            else
            {
                range = start - end;
                offset = end;
            }

            double[] selection = new double[range];
            double[] tmp = new double[left.Length - range];
            double[] selection2 = null;
            double[] tmp2 = null;

            if(right != null)
            {
                selection2 = new double[range];
                tmp2 = new double[right.Length - range];
            }


            for (int i = 0; i < range; i++)
            {
                selection[i] = left[i + offset];
                if(right != null)
                {
                    selection2[i] = right[i + offset];
                }
            }

            if(right != null)
            {
                double[] temp = new double[selection.Length + selection2.Length];
                Array.Copy(selection, temp, selection.Length);
                Array.Copy(selection2, 0, temp, selection.Length, selection2.Length);
                Clipboard.SetData("Copy", temp);

            } else
            {
                Clipboard.SetData("Copy", selection);
            }

            if(start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }
            
            Array.Copy(left, 0, tmp, 0, start);
            Array.Copy(left, end, tmp, start, tmp.Length - start);
            left = tmp;

            if (right != null)
            {
                Array.Copy(right, 0, tmp2, 0, start);
                Array.Copy(right, end, tmp2, start, tmp2.Length - start);
                right = tmp2;
            }

            this.Invoke(inv, new object[] { left });

            if (right != null)
            {
                this.Invoke(inv2, new object[] { right });
            }
        }

        /*
         * copyToolStripMenuItem_Click
         *
         * Listener for the menus Copy button
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            copyChart1();
        }

        /*
         * pasteToolStripMenuItem_Click
         *
         * Listener for the menus Paste button
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pasteChart1();
        }

        /*
         * cutToolStripMenuItem_Click
         *
         * Listener for the menus Cut button
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cutChart1();
        }

        /*
         * FilterBtn_Click
         *
         * Listener for the for the Filter button on the main interface, checks which filtering method is
         * checked (via coresponding radio buttons), and applies it.
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void FilterBtn_Click(object sender, EventArgs e)
        {
            int select = (int)chart2.ChartAreas[0].CursorX.SelectionStart;
            Thread filter = null;

            if ((select >= 0) && (select <= A.Length))
            {
                if (HPassRdBtn.Checked)
                {
                    filter = new Thread(() => HighFill(select));
                }
                if (LPassRdBtn.Checked)
                {
                    filter = new Thread(() => LowFill(select));
                }

                if (filter != null)
                {
                    //clear existing data on charts
                    chart1.Series["Series1"].Points.Clear();
                    chart2.Series["Series1"].Points.Clear();
                    chart3.Series["Series1"].Points.Clear();
                    chart4.Series["Series1"].Points.Clear();

                    // disable data relient buttons
                    button2.Enabled = false;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    PlayBtn.Enabled = false;
                    StopBtn.Enabled = false;
                    WndBtn.Enabled = false;
                    FilterBtn.Enabled = false;
                    exportToolStripMenuItem.Enabled = false;
                    cutToolStripMenuItem.Enabled = false;
                    copyToolStripMenuItem.Enabled = false;

                    filter.Start();
                }
            } else
            {
                MessageBox.Show("Error: Please Make a Selection first.");
            }
        }

        /*
         * HighFill
         *
         * Method used to apply a High Pass Filter (sperated for better threading)
         * 
         * Parameter: 
         *      select  bucket selected to build the filter from
         *
         * return void
         */
        private void HighFill(int select)
        {
            Filter.HPass(ref A, ref A2, ref left, ref right, select, this);
            //Thread fourier = new Thread(() => Fourier.DFT(left, ref A, this));
            this.Invoke(inv, new object[] { left });
            this.Invoke(inv2, new object[] { right });
            //fourier.Start();
            window();
        }

        /*
         * LowFill
         *
         * Method used to apply a Low Pass Filter (sperated for better threading)
         * 
         * Parameter: 
         *      select  bucket selected to build the filter from
         *
         * return void
         */
        private void LowFill(int select)
        {
            Filter.LPass(ref A, ref A2, ref left, ref right, select, this);
           // Thread fourier = new Thread(() => Fourier.DFT(left, ref A, this));
            this.Invoke(inv, new object[] { left });
            this.Invoke(inv2, new object[] { right });
            // fourier.Start();
            window();
        }

        /*
         * WndBtn_Click
         *
         * Listener for the Windowing Button on the main interface. builds window based on chart1 selection.
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void WndBtn_Click(object sender, EventArgs e)
        {
            int start = (int)chart1.ChartAreas[0].CursorX.SelectionStart;
            int end = (int)chart1.ChartAreas[0].CursorX.SelectionEnd;

            if ((start >= 0) && (start <= left.Length))
            {
                if ((end >= 0) && (end <= left.Length))
                {
                    if (start < end)
                    {
                        range = end - start;
                        offset = start;
                    }
                    else
                    {
                        range = start - end;
                        offset = end;
                    }

                    // disable data relient buttons
                    button2.Enabled = false;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    PlayBtn.Enabled = false;
                    StopBtn.Enabled = false;
                    WndBtn.Enabled = false;
                    FilterBtn.Enabled = false;
                    exportToolStripMenuItem.Enabled = false;
                    cutToolStripMenuItem.Enabled = false;
                    copyToolStripMenuItem.Enabled = false;

                    window();
                } else
                {
                    MessageBox.Show("Error: Please Make a Selection first.");
                }
            } else
            {
                MessageBox.Show("Error: Please Make a Selection first.");
            }
        }

        /*
         * window
         *
         * Checks which windowing fucntion to use and calls it based on radio buttons.
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        public void window()
        {
            double[] winSample, winSample2 = null;
            Thread dft = null;
            A = new Complex[range];
            winSample = new double[range];
            Array.Copy(left, offset, winSample, 0, range);

            if(right != null)
            {
                A2 = new Complex[range];
                winSample2 = new double[range];
                Array.Copy(right, offset, winSample2, 0, range);
            }

            if (!SqrBtn.Checked)
            {
                if (TriBtn.Checked)
                {
                    Windowing.Triangle(ref winSample, range);

                    //chart2.Series["Series1"].Points.Clear();

                    dft = new Thread(() => Fourier.DFT(winSample, winSample2, ref A, ref A2, this));
                }

                if (HamBtn.Checked)
                {
                    Windowing.Hamming(ref winSample, range);

                    //chart2.Series["Series1"].Points.Clear();

                    dft = new Thread(() => Fourier.DFT(winSample, winSample2, ref A, ref A2, this));
                }
            }
            else
            {
                // chart2.Series["Series1"].Points.Clear();

                dft = new Thread(() => Fourier.DFT(winSample, winSample2, ref A, ref A2, this));
            }
            if (dft != null)
            {
                dft.Start();
            }
        }

        /*
         * button3_Click
         *
         * Listener for the Copy button on the main interface
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void button3_Click(object sender, EventArgs e)
        {
            copyChart1();
        }

        /*
         * button4_Click
         *
         * Listener for the Cut button on the main interface
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void button4_Click(object sender, EventArgs e)
        {
            cutChart1();
        }

        /*
         * button5_Click
         *
         * Listener for the Paste button on the main interface
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void button5_Click(object sender, EventArgs e)
        {
            pasteChart1();
        }

        /*
         * PlayBtn_Click
         *
         * Listener for the Play button on the main interface
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void PlayBtn_Click(object sender, EventArgs e)
        {
            // C# code for play back in the main form
            /*byte[] tmp;

            if(player == null)
            {
                if(wave == null)
                {
                    return;
                }

                wave.rebuildWav(left);
                tmp =  wave.playWav();
                
                if(tmp != null)
                {
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream(tmp))
                    {
                        player = new System.Media.SoundPlayer(ms);
                        player.Play();
                    }
                }
            } else
            {
                wave.rebuildWav(left);
                tmp = wave.playWav();

                if (tmp != null)
                {
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream(tmp))
                    {
                        player = new System.Media.SoundPlayer(ms);
                        player.Play();
                    }
                }
            }*/

            // win32 code for playback in the main form
            if (wave != null)
            {
                wave.rebuildWav(left, right);
                waveOut = this.callbackWaveOut;
                play(waveOut);
            }
        }

        /*
         * play
         *
         * Function uses win32 to play the audio imported/recored by the progam
         * 
         * Parameter: 
         *      RecordDialog.AudioPlayBackDeleagate waveOut
         *
         * return void
         */
        private void play(RecordDialog.AudioPlayBackDelegate waveOut)
        {

            handle = new IntPtr();

            WAVEFORMAT format;
            format.wFormatTag = (ushort)wave.fmtCode;
            format.nChannels = (ushort)wave.channels;
            format.nSamplesPerSec = (ushort)wave.sampleRate;
            format.wBitsPerSample = (ushort)wave.bitDepth;
            format.nBlockAlign = (ushort)wave.fmtBlockAlign;
            format.nAvgBytesPerSec = (ushort)wave.fmtAvgBPS;
            format.cbSize = 0;
            int i = -1;

            i = waveOutOpen(ref handle, 4294967295, ref format, Marshal.GetFunctionPointerForDelegate(waveOut), 0, RecordDialog.CALLBACK_FUNCTION);
            if (i != 0)
            {
                this.Text = "Error: waveOutOpen";
                return;
            }

            i = waveOutPrepareHeader(handle, ref pWaveHdr, Convert.ToUInt32(Marshal.SizeOf(pWaveHdr)));
            if (i != 0)
            {
                return;
            }

            int ii = waveOutWrite(handle, ref pWaveHdr, Convert.ToUInt32(Marshal.SizeOf(pWaveHdr)));
            if (ii != 0)
            {
                return;
            }
        }

        /*
         * callBackWaveOut
         *
         * Function mimicks a win32 callback function, mostaly a dummy function, 
         * rebuilds pWaveHdr when called and correct message is recieved
         * 
         * Parameter: 
         *      IntPtr deviceHandle
         *      uint message
         *      IntPtr instance
         *      WAVEHDR wavehdr
         *      IntPtr reserved2
         *
         * return void
         */
        public void callbackWaveOut(IntPtr deviceHandle, uint message, IntPtr instance, ref WAVEHDR wavehdr, IntPtr reserved2)
        {
            if (message == 0x3BB) // WOM_OPEN
            {

                headerPin = GCHandle.Alloc(wave.byteArray, GCHandleType.Pinned);
                pWaveHdr.lpData = headerPin.AddrOfPinnedObject();
                pWaveHdr.dwBufferLength = (uint)wave.byteArray.Length;
                pWaveHdr.dwBytesRecorded = 0;
                pWaveHdr.dwUser = IntPtr.Zero;
                pWaveHdr.dwFlags = 0x00000004 | 0x00000008;
                pWaveHdr.dwLoops = 1;
                pWaveHdr.lpNext = IntPtr.Zero;
                pWaveHdr.reserved = IntPtr.Zero;

            }

        }

        /*
         * newWindowToolStripMenuItem_Click
         *
         * Listener for the New Window Menu item on the main interface, opens a new instance of the program
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(Application.ExecutablePath);
        }

        /*
         * viewReadMeToolStripMenuItem_Click
         *
         * Listener for the View Read Me Menu item on the main interface, opens the read me or displays an
         * error message
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void veiwReadMeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string readme = Application.StartupPath + "\\Comp3931Project README.txt";

            if(File.Exists(readme))
            {
                Process.Start(readme);
            } else
            {
                MessageBox.Show("Error: File Not Found. Read me moved or missing");
            } 
        }

        /*
         * StopBtn_Click
         *
         * Listener for the Stop button on the main interface, Stops audio play back
         * 
         * Parameter: 
         *      sender
         *      e
         *
         * return void
         */
        private void StopBtn_Click(object sender, EventArgs e)
        {
            // C# code to stop playback
            /*if(player != null)
            {
                player.Stop();
            }*/

            // win32 code for stoping palyback
            waveOutReset(handle);
        }
    }
}
