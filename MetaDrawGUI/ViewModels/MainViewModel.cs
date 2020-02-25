﻿using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using MassSpectrometry;
using System.Collections.Generic;
using Proteomics.Fragmentation;
using EngineLayer;
using Chemistry;
using MetaDrawGUI;

namespace ViewModels
{
    public class MainViewModel
    {
        public PlotModel privateModel;

        private const double STROKE_THICKNESS_UNANNOTATED = 0.5;
        private const double STROKE_THICKNESS_ANNOTATED = 2.0;

        private static Dictionary<ProductType, OxyColor> productTypeDrawColors = new Dictionary<ProductType, OxyColor>
        {
          { ProductType.b, OxyColors.Blue },
          { ProductType.y, OxyColors.Purple },
          { ProductType.c, OxyColors.Gold },
          { ProductType.zDot, OxyColors.Orange },
          { ProductType.D, OxyColors.DodgerBlue },
          { ProductType.M, OxyColors.Firebrick }
        };

        private static Dictionary<ProductType, OxyColor> betaPeptideProductTypeDrawColors = new Dictionary<ProductType, OxyColor>
        {
          { ProductType.b, OxyColors.LightBlue },
          { ProductType.y, OxyColors.MediumPurple },
          { ProductType.c, OxyColors.LightGoldenrodYellow },
          { ProductType.zDot, OxyColors.OrangeRed },
          { ProductType.D, OxyColors.AliceBlue },
          { ProductType.M, OxyColors.LightCoral }
        };

        public MainViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.privateModel = tmp;
        }

        public static PlotModel DrawScan(MsDataScan MsScanForDraw)
        {
            var x = MsScanForDraw.MassSpectrum.XArray;
            var y = MsScanForDraw.MassSpectrum.YArray;

            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();

            string scanInfo = MsScanForDraw.ScanFilter;

            PlotModel model = new PlotModel { Title = "Spectrum anotation of Scan " + scanNum, Subtitle = scanInfo, DefaultFontSize = 15, SubtitleFontSize = 12};
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = x.First(),
                Maximum = x.Last() * 1.02,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = x.Max() * 1.02
            });
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = y.Max() * 1.2,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = y.Max() * 1.2
            });

            //Draw the ms/ms scan peaks
            for (int i = 0; i < x.Length; i++)
            {
                var line = new LineSeries();
                line.Color = OxyColors.Black;
                line.StrokeThickness = 1;
                line.Points.Add(new DataPoint(x[i], 0));
                line.Points.Add(new DataPoint(x[i], y[i]));
                model.Series.Add(line);
            }
   
            return model;

        }

        public static PlotModel DrawPeptideSpectralMatch(MsDataScan msDataScan, PsmFromTsv psmToDraw)
        {
            // x is m/z, y is intensity
            var spectrumMzs = msDataScan.MassSpectrum.XArray;
            var spectrumIntensities = msDataScan.MassSpectrum.YArray;

            string subtitle = psmToDraw.FullSequence;
            if (psmToDraw != null && psmToDraw.BetaPeptideBaseSequence != null)
            {
                subtitle = psmToDraw.FullSequence + "\n" + psmToDraw.BetaPeptideFullSequence;
            }
            PlotModel model = new PlotModel { Title = "Spectrum Annotation of Scan #" + msDataScan.OneBasedScanNumber, DefaultFontSize = 15, Subtitle = subtitle };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "m/z", Minimum = 0, Maximum = spectrumMzs.Max() * 1.02, AbsoluteMinimum = 0, AbsoluteMaximum = spectrumMzs.Max() * 5 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Intensity", Minimum = 0, Maximum = spectrumIntensities.Max() * 1.2, AbsoluteMinimum = 0, AbsoluteMaximum = spectrumIntensities.Max() * 1.3 });
            model.Axes[1].Zoom(0, spectrumIntensities.Max() * 1.1);

            LineSeries[] allIons = new LineSeries[spectrumMzs.Length];

            // draw the matched peaks; if the PSM is null, we're just drawing the peaks in the scan without annotation, so skip this part
            if (psmToDraw != null)
            {
                foreach (var peak in psmToDraw.MatchedIons)
                {
                    OxyColor ionColor;

                    if (productTypeDrawColors.ContainsKey(peak.NeutralTheoreticalProduct.ProductType))
                    {
                        ionColor = productTypeDrawColors[peak.NeutralTheoreticalProduct.ProductType];
                    }
                    else
                    {
                        ionColor = OxyColors.Turquoise;
                    }

                    int i = msDataScan.MassSpectrum.GetClosestPeakIndex(peak.NeutralTheoreticalProduct.NeutralMass.ToMz(peak.Charge)).Value;

                    // peak line
                    allIons[i] = new LineSeries();
                    allIons[i].Color = ionColor;
                    allIons[i].StrokeThickness = STROKE_THICKNESS_ANNOTATED;
                    allIons[i].Points.Add(new DataPoint(peak.Mz, 0));
                    allIons[i].Points.Add(new DataPoint(peak.Mz, spectrumIntensities[i]));

                    // peak annotation
                    string peakAnnotationText = peak.NeutralTheoreticalProduct.ProductType.ToString().ToLower() + peak.NeutralTheoreticalProduct.FragmentNumber + " (" + peak.Mz.ToString("F3") + ")";
                    if (peak.NeutralTheoreticalProduct.NeutralLoss != 0)
                    {
                        peakAnnotationText = peak.NeutralTheoreticalProduct.ProductType.ToString().ToLower() + peak.NeutralTheoreticalProduct.FragmentNumber + "-" + peak.NeutralTheoreticalProduct.NeutralLoss.ToString("F2") + " (" + peak.Mz.ToString("F3") + ")";
                    }

                    var peakAnnotation = new TextAnnotation();
                    peakAnnotation.TextRotation = -60;
                    peakAnnotation.Font = "Arial";
                    peakAnnotation.FontSize = 12;
                    peakAnnotation.FontWeight = 2.0;
                    peakAnnotation.TextColor = ionColor;
                    peakAnnotation.StrokeThickness = 0;
                    peakAnnotation.Text = peakAnnotationText;
                    peakAnnotation.TextPosition = new DataPoint(allIons[i].Points[1].X, allIons[i].Points[1].Y + peakAnnotation.Text.Length * 1.5 / 4);
                    peakAnnotation.TextHorizontalAlignment = HorizontalAlignment.Left;
                    model.Annotations.Add(peakAnnotation);

                    model.Series.Add(allIons[i]);
                }

                if (psmToDraw.BetaPeptideBaseSequence != null)
                {
                    foreach (var peak in psmToDraw.BetaPeptideMatchedIons)
                    {
                        OxyColor ionColor;

                        if (productTypeDrawColors.ContainsKey(peak.NeutralTheoreticalProduct.ProductType))
                        {
                            ionColor = betaPeptideProductTypeDrawColors[peak.NeutralTheoreticalProduct.ProductType];
                        }
                        else
                        {
                            ionColor = OxyColors.Turquoise;
                        }

                        int i = msDataScan.MassSpectrum.GetClosestPeakIndex(peak.NeutralTheoreticalProduct.NeutralMass.ToMz(peak.Charge)).Value;

                        // peak line
                        allIons[i] = new LineSeries();
                        allIons[i].Color = ionColor;
                        allIons[i].StrokeThickness = STROKE_THICKNESS_ANNOTATED;
                        allIons[i].Points.Add(new DataPoint(peak.Mz, 0));
                        allIons[i].Points.Add(new DataPoint(peak.Mz, spectrumIntensities[i]));

                        // peak annotation
                        string peakAnnotationText = "beta-" + peak.NeutralTheoreticalProduct.ProductType.ToString().ToLower() + peak.NeutralTheoreticalProduct.FragmentNumber + " (" + peak.Mz.ToString("F3") + ")";
                        if (peak.NeutralTheoreticalProduct.NeutralLoss != 0)
                        {
                            peakAnnotationText = "beta-" + peak.NeutralTheoreticalProduct.ProductType.ToString().ToLower() + peak.NeutralTheoreticalProduct.FragmentNumber + "-" + peak.NeutralTheoreticalProduct.NeutralLoss.ToString("F2") + " (" + peak.Mz.ToString("F3") + ")";
                        }

                        var peakAnnotation = new TextAnnotation();
                        peakAnnotation.TextRotation = -60;
                        peakAnnotation.Font = "Arial";
                        peakAnnotation.FontSize = 12;
                        peakAnnotation.FontWeight = 2.0;
                        peakAnnotation.TextColor = ionColor;
                        peakAnnotation.StrokeThickness = 0;
                        peakAnnotation.Text = peakAnnotationText;
                        peakAnnotation.TextPosition = new DataPoint(allIons[i].Points[1].X, allIons[i].Points[1].Y + peakAnnotation.Text.Length * 1.5 / 4);
                        peakAnnotation.TextHorizontalAlignment = HorizontalAlignment.Left;
                        model.Annotations.Add(peakAnnotation);

                        model.Series.Add(allIons[i]);
                    }
                }
            }

            // draw the remaining unmatched peaks
            for (int i = 0; i < spectrumMzs.Length; i++)
            {
                // peak has already been drawn (it is a matched peak)
                if (allIons[i] != null)
                {
                    continue;
                }

                allIons[i] = new LineSeries();
                allIons[i].Color = OxyColors.DimGray;
                allIons[i].StrokeThickness = STROKE_THICKNESS_UNANNOTATED;
                allIons[i].Points.Add(new DataPoint(spectrumMzs[i], 0));
                allIons[i].Points.Add(new DataPoint(spectrumMzs[i], spectrumIntensities[i]));
                model.Series.Add(allIons[i]);
            }

            // Axes are created automatically if they are not defined      
            return model;
        }

        public static PlotModel DrawScan(MsDataScan MsScanForDraw, MsFeature msFeature)
        {
            var x = MsScanForDraw.MassSpectrum.XArray;
            var y = MsScanForDraw.MassSpectrum.YArray;

            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();

            PlotModel model = new PlotModel { Title = "Spectrum anotation of Scan " + scanNum, DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = x.Min() * 0.98,
                Maximum = x.Max() * 1.02,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = x.Max() * 1.02
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = y.Max() * 1.2,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = y.Max() * 1.2
            });

            //Draw the ms/ms scan peaks
            for (int i = 0; i < x.Length; i++)
            {
                var line = new LineSeries();
                line.Color = OxyColors.Gray;
                line.StrokeThickness = 1;
                line.Points.Add(new DataPoint(x[i], 0));
                line.Points.Add(new DataPoint(x[i], y[i]));
                model.Series.Add(line);
            }

            for (int i = 0; i < msFeature.PeakMzs.Count; i++)
            {
                var line = new LineSeries();
                line.Color = OxyColors.Red;
                line.StrokeThickness = 1.5;
                line.Points.Add(new DataPoint(msFeature.PeakMzs[i], 0));
                line.Points.Add(new DataPoint(msFeature.PeakMzs[i], msFeature.PeakIntensities[i]));
                model.Series.Add(line);
            }

            return model;

        }

        public static PlotModel ResetViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            return tmp;
        }

        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            double fold = (this.privateModel.Axes[0].ActualMaximum - this.privateModel.Axes[0].ActualMinimum) / (this.privateModel.Axes[0].AbsoluteMaximum - this.privateModel.Axes[0].AbsoluteMinimum);
            this.privateModel.Axes[1].Minimum = 0;
            this.privateModel.Axes[1].Maximum = this.privateModel.Axes[1].AbsoluteMaximum * 0.6 * fold;

            foreach (var series in this.privateModel.Series)
            {
                if (series is LineSeries)
                {
                    var x = (LineSeries)series;
                    if (x.Points[1].X >= this.privateModel.Axes[0].ActualMinimum && x.Points[1].X <= this.privateModel.Axes[0].ActualMaximum)
                    {
                        if (x.Points[1].Y > this.privateModel.Axes[1].Maximum)
                        {
                            this.privateModel.Axes[1].Maximum = x.Points[1].Y * 1.2;
                        }
                    }

                }
            }
        }
    }
}