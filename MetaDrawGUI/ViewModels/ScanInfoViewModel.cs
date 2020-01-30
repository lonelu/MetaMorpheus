using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using System.ComponentModel;
using MassSpectrometry;
using MetaDrawGUI;
using System.Collections.Generic;
using System;

namespace ViewModels
{
    public class ScanInfoViewModel
    {
        public PlotModel privateModel;

        public static PlotModel DrawScanInfo_PT_Model(List<ScanInfo> scanInfos)
        {
            var maxTime = scanInfos.Max(p => p.PreviousTime);

            PlotModel model = new PlotModel { Title = "PreScanTime", DefaultFontSize = 15 };

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Retetion Time/min",
                Minimum = 0,
                Maximum = scanInfos.Last().RententionTime,
                AbsoluteMinimum = -10,
                AbsoluteMaximum = scanInfos.Last().RententionTime * 1.2
            });


            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = maxTime * 1.2,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = maxTime * 1.2
            });

            var x = scanInfos.GroupBy(p => p.types).OrderBy(p=>p.Key);

            foreach (var y in x)
            {
                var scatter = new ScatterSeries()
                {
                    Title = y.First().types,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 2,
                    
                };

                foreach (var z in y)
                {
                    scatter.Points.Add(new ScatterPoint(z.RententionTime, z.PreviousTime));
                }

                model.Series.Add(scatter);
            }

            return model;
        }


        public static PlotModel DrawScanInfo_IJ_Model(List<ScanInfo> scanInfos)
        {
            var maxTime = scanInfos.Max(p => p.InjectTime);

            PlotModel model = new PlotModel { Title = "InjectTime", DefaultFontSize = 15 };

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Retetion Time/min",
                Minimum = 0,
                Maximum = scanInfos.Last().RententionTime,
                AbsoluteMinimum = -10,
                AbsoluteMaximum = scanInfos.Last().RententionTime * 1.2
            });


            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = maxTime * 1.2,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = 2000
            });

            var x = scanInfos.GroupBy(p => p.ScanType).OrderBy(p=>p.Key);

            foreach (var y in x)
            {
                var scatter = new ScatterSeries()
                {
                    Title = y.First().ScanType,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 2,

                };

                foreach (var z in y)
                {
                    scatter.Points.Add(new ScatterPoint(z.RententionTime, z.InjectTime));
                }

                model.Series.Add(scatter);
            }

            return model;
        }

    }
}
