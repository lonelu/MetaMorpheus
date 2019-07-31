using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chemistry;
using MassSpectrometry;
using MzLibUtil;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace MetaDrawGUI
{
    public class PeakViewModel
    {
        public PlotModel privateModel;      

        public PeakViewModel()
        {
            // Create and Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            privateModel = new PlotModel { Title = "Peak Annotation", Subtitle = "using OxyPlot" };
        }

        public void DrawPeak(ChromatographicPeak peak)
        {
            PlotModel model = new PlotModel { Title = peak.Sequence + "; " + peak.PrecursorZ };

            if (!peak.Timepoints.Any())
            {
                return;
            }

            var groupedByCharge = peak.Timepoints.GroupBy(p => p.Charge);

            foreach (var charge in groupedByCharge)
            {
                int minIndex = charge.Min(p => p.Index);
                int maxIndex = charge.Max(p => p.Index);
                var chargeTimepointSeries = new LineSeries { Title = charge.Key + "; " + peak.Mass.ToMz(charge.Key).ToString("F3") };

                for (int i = minIndex; i <= maxIndex; i++)
                {
                    var timepoint = charge.FirstOrDefault(p => p.Index == i);

                    if (timepoint != null)
                    {
                        chargeTimepointSeries.Points.Add(new DataPoint(timepoint.RT, timepoint.Intensity));
                    }
                    else
                    {
                        //chargeTimepointSeries.Points.Add(new DataPoint(i, 0));
                    }
                }
                
                model.Series.Add(chargeTimepointSeries);
            }

            if (!double.IsNaN(peak.Ms2Rt))
            {
                LineAnnotation Line = new LineAnnotation
                {
                    StrokeThickness = 1,
                    Color = OxyColors.Crimson,
                    Type = LineAnnotationType.Vertical,
                    Text = peak.Sequence,
                    X = peak.Ms2Rt,
                    Y = 100
                };

                model.Annotations.Add(Line);
            }

            privateModel = model;
        }

        public void DrawXic(double mass, int charge, double rt, MsDataFile file, Tolerance massTolerance, double rtTol, int numPeaks, string filename)
        {
            PlotModel model = new PlotModel { Title = filename + "rt" + rt.ToString("F2") + "m/z " + mass.ToMz(charge).ToString("F3") + "; z=" + charge };

            var ms1 = file.GetAllScansList().Where(p => p.MsnOrder == 1).ToList();
            var rts = ms1.Select(p => p.RetentionTime).ToList();

            int ind = rts.BinarySearch(rt - rtTol);
            if (ind < 0)
            {
                ind = ~ind;
            }

            if (ind >= rts.Count)
            {
                ind = rts.Count - 1;
            }

            for (int i = 0; i <= numPeaks; i++)
            {
                var isotopeTimepointSeries = new LineSeries { Title = i.ToString() };
                var isotopeMass = mass + i * Constants.C13MinusC12;

                for (int t = ind; t < ms1.Count; t++)
                {
                    var ms1scan = ms1[t];

                    if (ms1scan.RetentionTime > rt + rtTol)
                    {
                        break;
                    }

                    int? indexOfClosest = ms1scan.MassSpectrum.GetClosestPeakIndex(isotopeMass.ToMz(charge));
                    if (indexOfClosest.HasValue && massTolerance.Within(ms1scan.MassSpectrum.XArray[indexOfClosest.Value].ToMass(charge), isotopeMass))
                    {
                        isotopeTimepointSeries.Points.Add(new DataPoint(ms1scan.RetentionTime, ms1scan.MassSpectrum.YArray[indexOfClosest.Value]));
                    }
                    else
                    {
                        isotopeTimepointSeries.Points.Add(new DataPoint(ms1scan.RetentionTime, 0));
                    }
                }
                model.Series.Add(isotopeTimepointSeries);
            }
            
            if (!double.IsNaN(rt))
            {
                LineAnnotation Line = new LineAnnotation
                {
                    StrokeThickness = 2,
                    Color = OxyColors.Crimson,
                    Type = LineAnnotationType.Vertical,
                    Text = mass.ToString("F3"),
                    X = rt,
                    Y = 100
                };

                model.Annotations.Add(Line);
            }

            privateModel = model;
        }

        public void ResetViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "XIC" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            privateModel = tmp;
        }

    }
}
