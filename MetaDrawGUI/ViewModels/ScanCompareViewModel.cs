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

            PlotModel model = new PlotModel { Title = "Spectrum anotation of Scan " + msDataScan.OneBasedScanNumber.ToString() + " and " + anotherScan.OneBasedScanNumber.ToString(), DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = 500,
                Maximum = 1600,
                AbsoluteMinimum = 500,
                AbsoluteMaximum = 1600
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = -100,
                Maximum = 100,
                AbsoluteMinimum = -100,
                AbsoluteMaximum = 100
            });

            //Draw the ms/ms scan peaks
            for (int i = 0; i < x.Length; i++)
            {
                var line = new LineSeries();
                line.Color = OxyColors.Black;
                line.StrokeThickness = 1;
                line.Points.Add(new DataPoint(x[i], 0));
                line.Points.Add(new DataPoint(x[i], y[i] / ymax * 100));
                model.Series.Add(line);
            }

            for (int i = 0; i < x2.Length; i++)
            {
                var line = new LineSeries();
                line.Color = OxyColors.Black;
                line.StrokeThickness = 1;
                line.Points.Add(new DataPoint(x2[i], 0));
                line.Points.Add(new DataPoint(x2[i], -y2[i] / ymax2 * 100));
                model.Series.Add(line);
            }
            //model.Axes[0].AxisChanged += XAxisChanged;
            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            return model;

        }

    }
}
