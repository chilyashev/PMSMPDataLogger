using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PMSMPDataLogger
{
    public partial class MainForm : Form
    {
        private int tempIndex = 0;
        private string[] ports;
        
        private SerialPort serial;

        private Series tempSeries = new Series();
        private Series currentSeries = new Series();
        private Legend me;

        public string selectedPort;
        public MainForm()
        {
            InitializeComponent();
            chart1.Series.Add(tempSeries);
            chart1.Series.Add(currentSeries);

            chart1.Update();
            chart1.Show();

            tempSeries.ChartType = SeriesChartType.Spline;
            tempSeries.Name = @"Температура, °C";
            currentSeries.ChartType = SeriesChartType.Spline;
            currentSeries.Name = "Заряд, A";

            me = new Legend();
            me.Name = "me";
            me.Title = "Легенда";

            chart1.Legends.Add(me);

            currentSeries.Legend = "me";
            tempSeries.Legend = "me";

            ports = SerialPort.GetPortNames();
            serial = new SerialPort();

            if (ports.Length > 0)
            {
                cmbPort.Enabled = true;
                cmbPort.Items.AddRange(ports);
            }
            else
            {
                cmbPort.Enabled = false;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();

            selectedPort = cmbPort.SelectedItem as String;
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_Suffer;
            worker.ProgressChanged += worker_Whine;
            worker.RunWorkerAsync();
        }

        private void worker_Suffer(object sender, DoWorkEventArgs e)
        {
            int bytesRead;
            byte[] handShakeCmd = new byte[5];
            string line = "";
            BackgroundWorker worker = sender as BackgroundWorker;

            // Port setup
            serial.PortName = selectedPort;
            serial.BaudRate = 115200;
            serial.Parity = Parity.None;
            serial.StopBits = StopBits.One;
            serial.DataBits = 8;
            serial.Handshake = Handshake.None;
            serial.DtrEnable = true;
            serial.Open();

            // That's what the controller considers a "successful handshake message"
            handShakeCmd[0] = Convert.ToByte(80);
            handShakeCmd[1] = Convert.ToByte(08);
            handShakeCmd[2] = Convert.ToByte(1);
            handShakeCmd[3] = Convert.ToByte(3);
            handShakeCmd[4] = Convert.ToByte(5);

            while (line != "kthxbye" && serial.IsOpen)
            {
                bytesRead = serial.BytesToRead;
                if (bytesRead < 1)
                {
                    continue;
                }
                line = serial.ReadLine();
                if (line.Contains("o hai"))
                {
                    serial.Write(handShakeCmd, 0, 5);
                    serial.DiscardInBuffer();
                    serial.DiscardOutBuffer();
                    Thread.Sleep(1000);
                    continue;
                }

                worker.ReportProgress(1, line.Split(';'));
                Debug.WriteLine(line);
            }
        }
        private void worker_Whine(object sender, ProgressChangedEventArgs e)
        {
            string[] data = e.UserState as string[];
            DataPoint tempPoint;
            DataPoint currentPoint; 

            // Received some wrong stuff over the port
            if (data.Length < 3)
            {
                return;
            }
            // Sometimes the worker reports its progress after the form is closed and causes an exception.
            if (tempSeries == null || tempSeries.Points == null)
            {
                return;
            }
            // Well...
            if (tempIndex >= 100)
            {
                tempSeries.Points.Clear();
                currentSeries.Points.Clear();
                tempIndex = 0;
            }

            tempPoint = new DataPoint((double)tempIndex, Double.Parse(data[2]));
            currentPoint = new DataPoint((double)tempIndex, Double.Parse(data[1]));

            tempPoint.AxisLabel = data[0];
            tempSeries.Points.Add(tempPoint);
            currentSeries.Points.Add(currentPoint);

            this.lblTime.Text = data[0];
            this.lblCurrent.Text = data[1] + " A";
            this.lblTemp.Text = data[2] + @" °C";
            tempIndex++;
        }
        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            serial.Dispose();
            serial.Close();
        }

        private void cmbPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedPort = cmbPort.SelectedItem as String;
        }

    }


}
