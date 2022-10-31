using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace DicomServer
{
    public static class ChartManager
    {
        public static Chart CreateNewWaveformChart(string name, string accn, string mnfc, string model, double scale, string title, short max)
        {
            string header = $"PATIENT NAME: {name.ToUpper()} {Environment.NewLine}ACCESSION NUMBER: {accn}";
            string fab = $"{mnfc}: {model}";
            string sub = $"{title} [Scale: {scale} px]";

            Chart chart = new Chart();
            chart.Titles.Add(header).Alignment = ContentAlignment.MiddleLeft;
            chart.Titles.Add(fab).Alignment = ContentAlignment.MiddleRight;
            chart.Titles.Add(sub).Alignment = ContentAlignment.MiddleLeft;
            chart.Titles[0].Font = new Font("Arial", 18);
            chart.Titles[1].Font = new Font("Arial", 12);
            chart.Titles[1].Docking = Docking.Right;
            chart.Titles[2].Font = new Font("Arial", 18);

            ChartArea area = new ChartArea();

            area.AxisX.MajorGrid.LineColor = Color.LightSalmon;
            area.AxisY.MajorGrid.LineColor = Color.LightSalmon;

            area.AxisX.Interval = (4.1 * 25);
            area.AxisY.Interval = (4.1 * 25);

            area.AxisY.Maximum = max;

            area.AxisX.LabelStyle.Enabled = false;
            area.AxisY.LabelStyle.Enabled = false;

            chart.ChartAreas.Add(area);

            return chart;
        }
        public static Series AddNewWaveformSeries(Chart c)
        {
            Series serie = new Series
            {
                Color = Color.Black,
                ChartType = SeriesChartType.Spline,
            };
            c.Series.Add(serie);

            return serie;
        }

        public static bool SaveWaveformChart(Chart c)
        {
            try
            {
                c.Width = Screen.PrimaryScreen.WorkingArea.Width; //(int)Math.Ceiling(posX);
                c.Height = Screen.PrimaryScreen.WorkingArea.Height; // (int)Math.Ceiling(height) /2;

                //MakeSquare(c);

                c.Invalidate(); //Redraw the chart

                int fileCount = Directory.GetFiles(@"C:/Users/julio/Desktop/charts", "*.jpg", SearchOption.TopDirectoryOnly).Length;

                using (FileStream s = new FileStream($"C:/Users/julio/Desktop/charts/{fileCount}.jpg", FileMode.Create))
                    c.SaveImage(s, ChartImageFormat.Jpeg);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void MakeSquare(Chart chart)
        {
            ChartArea ca = chart.ChartAreas[0];

            // store the original value:
            var caip = ca.InnerPlotPosition;

            // get the current chart area :
            ElementPosition cap = ca.Position;

            // get both area sizes in pixels:
            Size CaSize = new Size((int)(cap.Width * chart.ClientSize.Width / 100f),
                                    (int)(cap.Height * chart.ClientSize.Height / 100f));

            Size CaIpSize = new Size((int)(caip.Width * CaSize.Width / 100f),
                                    (int)(caip.Height * CaSize.Height / 100f));

            // we need to use the smaller side:
            int IpNewSdide = Math.Min(CaIpSize.Width, CaIpSize.Height);

            // calculate the scaling factors
            float px = caip.Width / CaIpSize.Width * IpNewSdide;
            float py = caip.Height / CaIpSize.Height * IpNewSdide;

            // use one or the other:
            if (CaIpSize.Width < CaIpSize.Height)
                ca.InnerPlotPosition = new ElementPosition(caip.X, caip.Y, caip.Width, py);
            else
                ca.InnerPlotPosition = new ElementPosition(caip.X, caip.Y, px, caip.Height);
        }
    }
}
