using Chemistry;
using EngineLayer;
using MassSpectrometry;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using Proteomics.Fragmentation;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MetaDrawGUI;

namespace ViewModels
{
    public class PsmAnnotationViewModel : INotifyPropertyChanged
    {
        private const double STROKE_THICKNESS_UNANNOTATED = 0.5;
        private const double STROKE_THICKNESS_ANNOTATED = 2.0;
        public PlotModel privateModel;

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

        public PlotModel PsmAnnoModel
        {
            get
            {
                return this.privateModel;
            }
            set
            {
                this.privateModel = value;
                NotifyPropertyChanged("PsmAnnoModel");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public PsmAnnotationViewModel()
        {
            // Create and Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            privateModel = new PlotModel { Title = "Spectrum Annotation", Subtitle = "using OxyPlot" };
        }

        // single peptides 
        public void DrawPeptideSpectralMatch(MsDataScan msDataScan, SimplePsm psmToDraw = null)
        {
            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            if (psmToDraw == null)
            {
                this.PsmAnnoModel = DrawScan(msDataScan);
            }
            else
            {
                this.PsmAnnoModel = Draw(msDataScan, psmToDraw);
            }
        }

        public PlotModel DrawScan(MsDataScan MsScanForDraw)
        {
            var x = MsScanForDraw.MassSpectrum.XArray;
            var y = MsScanForDraw.MassSpectrum.YArray;

            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();


            PlotModel model = new PlotModel { Title = "Spectrum anotation of Scan " + scanNum, DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = 0,
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

            LineSeries[] s0 = new LineSeries[x.Length];
            LineSeries[] s1 = new LineSeries[x.Length];
            LineSeries[] s2 = new LineSeries[x.Length];

            //Draw the ms/ms scan peaks
            for (int i = 0; i < x.Length; i++)
            {
                s0[i] = new LineSeries();
                s0[i].Color = OxyColors.Black;
                s0[i].StrokeThickness = 1;
                s0[i].Points.Add(new DataPoint(x[i], 0));
                s0[i].Points.Add(new DataPoint(x[i], y[i]));
                model.Series.Add(s0[i]);
            }
            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            return model;

        }

        private PlotModel Draw(MsDataScan msDataScan, SimplePsm psmToDraw)
        {
            // x is m/z, y is intensity
            var spectrumMzs = msDataScan.MassSpectrum.XArray;
            var spectrumIntensities = msDataScan.MassSpectrum.YArray;

            string subtitle = psmToDraw.BaseSeq;

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
                    string peakAnnotationText = peak.NeutralTheoreticalProduct.ProductType.ToString().ToLower() + peak.NeutralTheoreticalProduct.TerminusFragment.FragmentNumber + " (" + peak.Mz.ToString("F3") + ")";
                    if (peak.NeutralTheoreticalProduct.NeutralLoss != 0)
                    {
                        peakAnnotationText = peak.NeutralTheoreticalProduct.ProductType.ToString().ToLower() + peak.NeutralTheoreticalProduct.TerminusFragment.FragmentNumber + "-" + peak.NeutralTheoreticalProduct.NeutralLoss.ToString("F2") + " (" + peak.Mz.ToString("F3") + ")";
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
                        string peakAnnotationText = "beta-" + peak.NeutralTheoreticalProduct.ProductType.ToString().ToLower() + peak.NeutralTheoreticalProduct.TerminusFragment.FragmentNumber + " (" + peak.Mz.ToString("F3") + ")";
                        if (peak.NeutralTheoreticalProduct.NeutralLoss != 0)
                        {
                            peakAnnotationText = "beta-" + peak.NeutralTheoreticalProduct.ProductType.ToString().ToLower() + peak.NeutralTheoreticalProduct.TerminusFragment.FragmentNumber + "-" + peak.NeutralTheoreticalProduct.NeutralLoss.ToString("F2") + " (" + peak.Mz.ToString("F3") + ")";
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
    }
}
