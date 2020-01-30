using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using MassSpectrometry;
using System.Collections.Generic;
using Proteomics.Fragmentation;
using EngineLayer;
using Chemistry;

namespace ViewModels
{
    public class ScanCompareViewModel
    {
        public static PlotModel DrawScan(MsDataScan msDataScan, MsDataScan anotherScan)
        {
            var x = msDataScan.MassSpectrum.XArray;
            var y = msDataScan.MassSpectrum.YArray;
            var ymax = y.Max();

            var x2 = anotherScan.MassSpectrum.XArray;
            var y2 = anotherScan.MassSpectrum.YArray;
            var ymax2 = y2.Max();

            if (x.Length < 1 || x2.Length < 1)
            {
                PlotModel model_test = new PlotModel { Title = "There is no peaks in one of Scan " + msDataScan.OneBasedScanNumber.ToString() + " and " + anotherScan.OneBasedScanNumber.ToString(), DefaultFontSize = 15 };

                return model_test;
            }

            var xmin = x[0] < x2[0] ? x[0] : x2[0];
            var xmax = x.Last() > x2.Last() ? x.Last() : x2.Last();


            PlotModel model = new PlotModel { Title = "Spectrum anotation of Scan " + msDataScan.OneBasedScanNumber.ToString() + " and " + anotherScan.OneBasedScanNumber.ToString(), DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = xmin,
                Maximum = xmax,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = xmax * 1.2
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = -105,
                Maximum = 105,
                AbsoluteMinimum = -110,
                AbsoluteMaximum = 110
            });

            var xline = new LineSeries
            {
                Color = OxyColors.Black,
                StrokeThickness = 2,
            };
            xline.Points.Add(new DataPoint(xmin, 0));
            xline.Points.Add(new DataPoint(xmax, 0));
            model.Series.Add(xline);

            //Draw the ms/ms scan peaks
            for (int i = 0; i < x.Length; i++)
            {
                var line = new LineSeries();
                line.Color = OxyColors.DarkGray;
                line.StrokeThickness = 1;
                line.Points.Add(new DataPoint(x[i], 0));
                line.Points.Add(new DataPoint(x[i], y[i] / ymax * 100));
                model.Series.Add(line);
            }

            for (int i = 0; i < x2.Length; i++)
            {
                var line = new LineSeries();
                line.Color = OxyColors.DarkGray;
                line.StrokeThickness = 1;
                line.Points.Add(new DataPoint(x2[i], 0));
                line.Points.Add(new DataPoint(x2[i], -y2[i] / ymax2 * 100));
                model.Series.Add(line);
            }
            //model.Axes[0].AxisChanged += XAxisChanged;
            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            return model;

        }

        public static PlotModel DrawBoxVsNormalId(List<(int scanNum, double RT, int isMatch, double diff, int same)> ps)
        {

            PlotModel model = new PlotModel { Title = "Box vs Normal", DefaultFontSize = 15 };

            if (ps.Count < 1)
            {
                return model;
            }

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "RT",
                Minimum = ps.First().RT,
                Maximum = ps.Last().RT,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = ps.Last().RT * 1.2
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Diff",
                Minimum = ps.Max(p=>p.diff) + 5,
                Maximum = ps.Min(p=> p.diff) - 5,
                AbsoluteMinimum = -100,
                AbsoluteMaximum = 100
            });

            var x = ps.GroupBy(p => p.isMatch);

            foreach (var y in x)
            {
                var scatter = new ScatterSeries()
                {
                    Title = y.First().isMatch.ToString(),
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 2,

                };

                foreach (var z in y)
                {
                    scatter.Points.Add(new ScatterPoint(z.RT, z.diff));
                }

                model.Series.Add(scatter);
            }

            return model;
        }

    }
}
