﻿using System.Linq;
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

        public static PlotModel UpdataModelForDecon(MsDataScan MsScanForDraw, IsoEnvelop isotopicEnvelope)
        {
            var model = DrawDecon(MsScanForDraw, isotopicEnvelope);
            return model;
        }

        public static PlotModel DrawDecon(MsDataScan MsScanForDraw, IsoEnvelop isotopicEnvelope)
        {
            var x = MsScanForDraw.MassSpectrum.XArray;
            var y = MsScanForDraw.MassSpectrum.YArray;
            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();

            PlotModel model = new PlotModel { Title = "Spectrum Decon of Scan ", DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = isotopicEnvelope.ExperimentIsoEnvelop.First().Mz - 2,
                Maximum = isotopicEnvelope.ExperimentIsoEnvelop.Last().Mz + 2,
                AbsoluteMinimum = isotopicEnvelope.ExperimentIsoEnvelop.First().Mz - 10,
                AbsoluteMaximum = isotopicEnvelope.ExperimentIsoEnvelop.Last().Mz + 10,
            });
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = isotopicEnvelope.ExperimentIsoEnvelop.First().Intensity * 1.3,
                AbsoluteMinimum = 0,
                // AbsoluteMaximum = isotopicEnvelope.peaks.First().intensity * 2
                AbsoluteMaximum = y.Max() * 1.2
            });

            foreach (var peak in isotopicEnvelope.ExperimentIsoEnvelop)
            {
                var sPeak = new LineSeries();
                sPeak.Color = OxyColors.OrangeRed;
                sPeak.StrokeThickness = 2.5;
                sPeak.Points.Add(new DataPoint(peak.Mz, 0));
                sPeak.Points.Add(new DataPoint(peak.Mz, peak.Intensity));
                model.Series.Add(sPeak);
            }

            if (isotopicEnvelope.Partner!=null)
            {
                foreach (var peak in isotopicEnvelope.Partner.ExperimentIsoEnvelop)
                {
                    var sPeak = new LineSeries();
                    sPeak.Color = OxyColors.Yellow;
                    sPeak.StrokeThickness = 2.5;
                    sPeak.Points.Add(new DataPoint(peak.Mz, 0));
                    sPeak.Points.Add(new DataPoint(peak.Mz, peak.Intensity));
                    model.Series.Add(sPeak);
                }
            }

            var peakAnno = new TextAnnotation();
            peakAnno.TextRotation = 90;
            peakAnno.Font = "Arial";
            peakAnno.FontSize = 12;
            peakAnno.TextColor = OxyColors.Red;
            peakAnno.StrokeThickness = 0;
            peakAnno.TextPosition = new DataPoint(isotopicEnvelope.ExperimentIsoEnvelop[0].Mz, isotopicEnvelope.ExperimentIsoEnvelop[0].Intensity);
            peakAnno.Text = isotopicEnvelope.MonoisotopicMass.ToString("f1") + "@" + isotopicEnvelope.Charge.ToString();

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

        public static PlotModel DrawDeconModel(int ind)
        {
            PlotModel model = new PlotModel { Title = "Decon Model", DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = 0,
                Maximum = IsoDecon.AllMasses.Last().Last(),
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
                for (int j = 0; j < IsoDecon.AllMasses[i].Length; j++)
                {
                    var line = new LineSeries();
                    line.Color = OxyColors.Red;
                    line.StrokeThickness = 1;
                    line.Points.Add(new DataPoint(IsoDecon.AllMasses[i][j], 0));
                    line.Points.Add(new DataPoint(IsoDecon.AllMasses[i][j], IsoDecon.AllIntensities[i][j]));
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

        public static PlotModel DrawIntensityDistibution(MsDataScan MsScanForDraw)
        {
            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();

            var y = MsScanForDraw.MassSpectrum.YArray.OrderByDescending(p=>p).ToArray();

            PlotModel model = new PlotModel { Title = "Intensity Distribution of Scan ", DefaultFontSize = 15 };

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Intensity Rank",
                Minimum = 0,
                Maximum = y.Length + 5,
                AbsoluteMinimum = -5,
                AbsoluteMaximum = y.Length + 10,
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = y.Max() * 1.2,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = y.Max() * 1.3
            });

            for (int i = 0; i < y.Length; i++)
            {
                var line = new LineSeries();
                line.Color = OxyColors.Blue;
                line.StrokeThickness = 1;
                line.Points.Add(new DataPoint(i, 0));
                line.Points.Add(new DataPoint(i, y[i]));
                model.Series.Add(line);
            }

            return model;
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
