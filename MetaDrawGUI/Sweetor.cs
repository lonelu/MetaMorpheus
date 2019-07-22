using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ViewModels;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace MetaDrawGUI
{
    public class Sweetor
    {
        public void WritePGlycoResult(List<string> ResultFilePaths, List<SimplePsm> simplePsms)
        {
            foreach (var filepath in ResultFilePaths)
            {
                var ForderPath = Path.Combine(Path.GetDirectoryName(filepath), Path.GetFileNameWithoutExtension(filepath), "_pGlyco.mytsv");

                TsvReader_Glyco.WriteTsv(ForderPath, simplePsms.Where(p=>p.FileName == Path.GetFileName(filepath)).ToList());
            }
        }

        public PlotModel PlotGlycoRT(List<SimplePsm> simplePsms)
        {
            if (simplePsms.Count <= 0)
            {
                var reportModel = new PlotModel { Title = "Glycopeptide family", Subtitle = "no psms" };
                return reportModel;
            }

            OxyColor[] oxyColors = new OxyColor[15] { OxyColor.Parse("#F8766D"), OxyColor.Parse("#E58700"), OxyColor.Parse("#C99800"), OxyColor.Parse("#A3A500"),
                OxyColor.Parse("#6BB100"),OxyColor.Parse("#00BA38"), OxyColor.Parse("#00BF7D"), OxyColor.Parse("#00C0AF"), OxyColor.Parse("#00BCD8"), OxyColor.Parse("#00B0F6"),
                OxyColor.Parse("#619CFF"), OxyColor.Parse("#B983FF"), OxyColor.Parse("#E76BF3"), OxyColor.Parse("#FD61D1"), OxyColor.Parse("#FF67A4")};
            //OxyColor[] oxyColors = new OxyColor[3] { OxyColor.Parse("#f8766d"), OxyColor.Parse("#e58700"), OxyColor.Parse("#c99800") };

            foreach (var psm in simplePsms)
            {
                psm.iD = psm.BaseSeq + "_" + psm.Mod;
            }

            var psms_byId = simplePsms.GroupBy(p=>p.iD);

            var largestRT = simplePsms.Max(p => p.RT) * 1.2;
            var leastMass = simplePsms.Min(p => p.MonoisotopicMass) * 0.8;
            var largestMass = simplePsms.Max(p => p.MonoisotopicMass) * 1.2;

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
            foreach (var id_psms in psms_byId)
            {

                var psms_byAG = id_psms.GroupBy(p=>p.glycanAGNumber);

                foreach (var ag_psms in psms_byAG)
                {
                    List<DataPoint> dataPoints = new List<DataPoint>();
                    foreach (var psm in ag_psms.OrderBy(p => p.MonoisotopicMass))
                    {
                        dataPoints.Add(new DataPoint(psm.RT, psm.MonoisotopicMass));
                    }
                    int colorId = rand.Next(0, 14);
                    var line = new LineSeries();
                    line.Color = oxyColors[colorId];
                    line.MarkerType = MarkerType.Circle;
                    line.MarkerFill = oxyColors[colorId];
                    line.StrokeThickness = 1.5;
                    line.Points.AddRange(dataPoints);

                    model.Series.Add(line);
                }
            }

            return model;
        }
    }
}
