using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using System.ComponentModel;
using MassSpectrometry;
using System.Collections.Generic;

namespace ViewModels
{
    public class DeconViewModel
    {
        public PlotModel privateModel;

        public DeconViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "Decon Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.privateModel = tmp;
        }

        public static PlotModel UpdataModelForDecon(MsDataScan MsScanForDraw, IsotopicEnvelope isotopicEnvelope)
        {
            var model = DrawDecon(MsScanForDraw, isotopicEnvelope);
            return model;
        }

        public static PlotModel DrawDecon(MsDataScan MsScanForDraw, IsotopicEnvelope isotopicEnvelope)
        {
            var x = MsScanForDraw.MassSpectrum.XArray;
            var y = MsScanForDraw.MassSpectrum.YArray;
            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();

            PlotModel model = new PlotModel { Title = "Spectrum Decon of Scan ", DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = isotopicEnvelope.peaks.First().mz - 2,
                Maximum = isotopicEnvelope.peaks.Last().mz + 2,
                AbsoluteMinimum = isotopicEnvelope.peaks.First().mz - 10,
                AbsoluteMaximum = isotopicEnvelope.peaks.Last().mz + 10,
            });
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = isotopicEnvelope.peaks.First().intensity * 1.3,
                AbsoluteMinimum = 0,
                // AbsoluteMaximum = isotopicEnvelope.peaks.First().intensity * 2
                AbsoluteMaximum = y.Max() * 1.2
            });

            foreach (var peak in isotopicEnvelope.peaks)
            {
                var sPeak = new LineSeries();
                sPeak.Color = OxyColors.Red;
                sPeak.StrokeThickness = 2;
                sPeak.Points.Add(new DataPoint(peak.mz, 0));
                sPeak.Points.Add(new DataPoint(peak.mz, peak.intensity));
                model.Series.Add(sPeak);
            }

            var peakAnno = new TextAnnotation();
            peakAnno.TextRotation = 90;
            peakAnno.Font = "Arial";
            peakAnno.FontSize = 12;
            peakAnno.TextColor = OxyColors.Red;
            peakAnno.StrokeThickness = 0;
            peakAnno.TextPosition = new DataPoint(isotopicEnvelope.peaks[0].mz, isotopicEnvelope.peaks[0].intensity);
            peakAnno.Text = isotopicEnvelope.monoisotopicMass.ToString("f1") + "@" + isotopicEnvelope.charge.ToString();

            //Draw the ms/ms scan peaks
            LineSeries[] s0 = new LineSeries[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                s0[i] = new LineSeries();
                s0[i].Color = OxyColors.Blue;
                s0[i].StrokeThickness = 1;
                s0[i].Points.Add(new DataPoint(x[i], 0));
                s0[i].Points.Add(new DataPoint(x[i], y[i]));
                model.Series.Add(s0[i]);
            }

            model.Annotations.Add(peakAnno);
            //model.Axes[0].AxisChanged += XAxisChanged;
            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            return model;
        }

        public static PlotModel UpdateModelForDeconModel(MzSpectrumBU mzSpectrumBU, int ind)
        {
            PlotModel model = new PlotModel { Title = "Decon Model", DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = 0,
                Maximum = mzSpectrumBU.AllMasses.Last().Last(),
            });
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = 1.3,

            });

            List<LineSeries> lsPeaks = new List<LineSeries>();
            for (int i = ind; i < ind+50; i++)
            {
                for (int j = 0; j < mzSpectrumBU.AllMasses[i].Length; j++)
                {
                    var line = new LineSeries();
                    line.Color = OxyColors.Red;
                    line.StrokeThickness = 1;
                    line.Points.Add(new DataPoint(mzSpectrumBU.AllMasses[i][j], 0));
                    line.Points.Add(new DataPoint(mzSpectrumBU.AllMasses[i][j], mzSpectrumBU.AllIntensities[i][j]));
                    model.Series.Add(line);
                }
            }
            return model;
        }

        public static PlotModel ResetDeconModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "Decon Spectrum Annotation", Subtitle = "using OxyPlot" };

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
