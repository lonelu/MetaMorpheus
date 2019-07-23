﻿using System;
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
using MzLibUtil;

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
                    int colorId = rand.Next(0, 14);

                    foreach (var psm in ag_psms.OrderBy(p => p.MonoisotopicMass))
                    {
                        dataPoints.Add(new DataPoint(psm.RT, psm.MonoisotopicMass));

                        //var peakAnnotation = new TextAnnotation();
                        //peakAnnotation.Font = "Arial";
                        //peakAnnotation.FontSize = 8;
                        //peakAnnotation.FontWeight = 1.5;
                        //peakAnnotation.TextColor = oxyColors[colorId];
                        //peakAnnotation.StrokeThickness = 0;
                        //peakAnnotation.Text = ag_psms.First().glycanString;
                        //peakAnnotation.TextPosition = new DataPoint(psm.RT, psm.MonoisotopicMass);
                        //peakAnnotation.TextHorizontalAlignment = HorizontalAlignment.Left;
                        //model.Annotations.Add(peakAnnotation);
                    }

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

        //For simplicity, the A 291.09542 may not be considered.
        static double[] SugarMass = new double[10] { -406.15874, -365.13219, -203.07937, -162.05282, -146.05791, 146.05791, 162.05282, 203.07937, 365.13219, 406.15874 };
        static Tolerance tolerance = new PpmTolerance(5);

        public static List<HashSet<MsFeature>> GetGlycoFamilies(MsFeature[] msFeatures)
        {
            msFeatures = msFeatures.OrderBy(p => p.ApexRT).ToArray();
            for (int i = 0; i < msFeatures.Length; i++)
            {
                msFeatures[i].id = i;
            }

            HashSet<MsFeature>[] extractedFeatures = new HashSet<MsFeature>[msFeatures.Length];
            Object locker = new object();
            Parallel.For(0, msFeatures.Length, ind => {
                MsFeature[] allFeatures = new MsFeature[msFeatures.Length];
                lock (locker)
                {
                    Array.Copy(msFeatures, allFeatures, msFeatures.Length);
                }
                var featuresWithInRange = allFeatures.Where(p => p.ApexRT < msFeatures[ind].ApexRT + 0.5 && p.ApexRT > msFeatures[ind].ApexRT - 0.5).OrderBy(p => p.MonoMass).ToArray();

                extractedFeatures[ind] = ExtractGlycoMS1features(msFeatures[ind], featuresWithInRange);
            });

            int[] mark = new int[msFeatures.Length];

            for (int i = 0; i < msFeatures.Length; i++)
            {
                if (extractedFeatures[i].Count() == 0)
                {
                    mark[i] = 0;
                }
                else
                {
                    mark[i] = 1;
                }
            }

            List<HashSet<MsFeature>> allFamilies = new List<HashSet<MsFeature>>();

            for (int i = 0; i < msFeatures.Length; i++)
            {
                if (mark[i] == 1)
                {
                    HashSet<MsFeature> thisFamily = new HashSet<MsFeature>();
                    FamilyFeatures(i, extractedFeatures, mark, ref thisFamily);
                    allFamilies.Add(thisFamily);
                }
            }

            return allFamilies;
        }

        //To use this function, the input neuCodeIsotopicEnvelops has to be ordered already by monoisotopicMass
        private static HashSet<MsFeature> ExtractGlycoMS1features(MsFeature theFeature, MsFeature[] msFeatures)
        {
            HashSet<MsFeature> glycanCandidates = new HashSet<MsFeature>();

            if (msFeatures.Length == 0)
            {
                return glycanCandidates;
            }

            var masses = msFeatures.Select(p => p.MonoMass).ToArray();

            var families = SugarMass.Select(p => theFeature.MonoMass + p).ToArray();

            List<int> matchedInd = new List<int>();

            foreach (var fm in families)
            {
                var ind = Array.BinarySearch(masses, fm);
                if (ind < 0)
                {
                    ind = ~ind;
                }

                if (ind < masses.Length && tolerance.Within(fm, masses[ind]))
                {
                    matchedInd.Add(ind);
                }
                else if (ind > 0 && tolerance.Within(fm, masses[ind - 1]))
                {
                    matchedInd.Add(ind - 1);
                }
            }

            if (matchedInd.Count() >= 2)
            {
                glycanCandidates.Add(theFeature);
                foreach (var m in matchedInd)
                {
                    if (!glycanCandidates.Contains(msFeatures[m]))
                    {
                        glycanCandidates.Add(msFeatures[m]);

                    }
                }
            }


            return glycanCandidates;
        }

        private static void FamilyFeatures(int index, HashSet<MsFeature>[] extractedFeatures, int[] mark, ref HashSet<MsFeature> thisFamily)
        {
            if (mark[index] == 1)
            {
                thisFamily.Add(extractedFeatures[index].First());
                mark[index]--;
                foreach (var feature in extractedFeatures[index])
                {
                    if (mark[feature.id] == 1)
                    {
                        FamilyFeatures(feature.id, extractedFeatures, mark, ref thisFamily);
                    }
                    else
                    {
                        thisFamily.Add(feature);
                    }
                }
            }
        }

        public static PlotModel PlotGlycoFamily(List<HashSet<MsFeature>> msFeatures)
        {
            if (msFeatures.Count <= 0)
            {
                var reportModel = new PlotModel { Title = "Glycopeptide family", Subtitle = "no features" };
                return reportModel;
            }

            OxyColor[] oxyColors = new OxyColor[15] { OxyColor.Parse("#F8766D"), OxyColor.Parse("#E58700"), OxyColor.Parse("#C99800"), OxyColor.Parse("#A3A500"),
                OxyColor.Parse("#6BB100"),OxyColor.Parse("#00BA38"), OxyColor.Parse("#00BF7D"), OxyColor.Parse("#00C0AF"), OxyColor.Parse("#00BCD8"), OxyColor.Parse("#00B0F6"),
                OxyColor.Parse("#619CFF"), OxyColor.Parse("#B983FF"), OxyColor.Parse("#E76BF3"), OxyColor.Parse("#FD61D1"), OxyColor.Parse("#FF67A4")};


            var largestRT = msFeatures.SelectMany(p=>p).ToArray().Max(p => p.ApexRT) * 1.2;
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
