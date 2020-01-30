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
    public class GlycoViewModel
    {
        public static OxyColor[] oxyColors = new OxyColor[15] { OxyColor.Parse("#F8766D"), OxyColor.Parse("#E58700"), OxyColor.Parse("#C99800"), OxyColor.Parse("#A3A500"),
                OxyColor.Parse("#6BB100"),OxyColor.Parse("#00BA38"), OxyColor.Parse("#00BF7D"), OxyColor.Parse("#00C0AF"), OxyColor.Parse("#00BCD8"), OxyColor.Parse("#00B0F6"),
                OxyColor.Parse("#619CFF"), OxyColor.Parse("#B983FF"), OxyColor.Parse("#E76BF3"), OxyColor.Parse("#FD61D1"), OxyColor.Parse("#FF67A4")};

        public static PlotModel PlotGlycoFamily(List<HashSet<MsFeature>> msFeatures)
        {
            if (msFeatures.Count <= 0)
            {
                var reportModel = new PlotModel { Title = "Glycopeptide family", Subtitle = "no features" };
                return reportModel;
            }

            var largestRT = msFeatures.SelectMany(p => p).ToArray().Max(p => p.ApexRT) * 1.2;
            var leastMass = msFeatures.SelectMany(p => p).ToArray().Min(p => p.MonoMass) * 0.8;
            var largestMass = msFeatures.SelectMany(p => p).ToArray().Max(p => p.MonoMass) * 1.2;

            var model = new PlotModel { Title = "Glycopeptide family", Subtitle = "using OxyPlot" };


            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "RT",
                Minimum = 0,
                Maximum = largestRT,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = largestRT
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Monoisotopic Mass",
                Minimum = leastMass,
                Maximum = largestMass,
                AbsoluteMinimum = leastMass,
                AbsoluteMaximum = largestMass
            });

            Random rand = new Random();
            foreach (var features in msFeatures)
            {


                List<DataPoint> dataPoints = new List<DataPoint>();
                int colorId = rand.Next(0, 14);

                foreach (var f in features.OrderBy(p => p.MonoMass))
                {
                    dataPoints.Add(new DataPoint(f.ApexRT, f.MonoMass));
                }

                var line = new LineSeries();
                line.Color = oxyColors[colorId];
                line.MarkerType = MarkerType.Circle;
                line.MarkerFill = oxyColors[colorId];
                line.StrokeThickness = 1.5;
                line.Points.AddRange(dataPoints);
                model.Series.Add(line);



            }

            return model;
        }
    }
}
