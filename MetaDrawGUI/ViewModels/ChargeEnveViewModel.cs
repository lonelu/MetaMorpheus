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
    public class ChargeEnveViewModel
    {
        public PlotModel privateModel;

        public ChargeEnveViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "ChargeEnve Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.privateModel = tmp;
        }

        public static PlotModel DrawCharEnvelopModel(MsDataScan MsScanForDraw, ChargeEnvelop chargeDeconEnvelope)
        {
            var x = chargeDeconEnvelope.distributions.Select(p=>p.mz).ToArray();
            var y = chargeDeconEnvelope.distributions.Select(p=>p.intensity).ToArray();
            //var scale = y.Sum() / chargeDeconEnvelope.IntensityModel.Sum();
            //var intensityModel = chargeDeconEnvelope.IntensityModel.Select(p=>p*scale).ToArray();
            var charges = chargeDeconEnvelope.distributions.Select(p=>p.charge);
            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();

            PlotModel model = new PlotModel { Title = "Spectrum anotation of Scan " + scanNum, DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = 0,
                Maximum = x.Max() * 1.02,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = x.Max() * 1.2
            });

            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = y.Max() * 1.2,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = y.Max() * 1.3
            });


            for (int i = 0; i < x.Length; i++)
            {
                var line = new LineSeries();
                line.Color = OxyColors.DimGray;
                line.StrokeThickness = 3;
                line.Points.Add(new DataPoint(x[i], 0));
                line.Points.Add(new DataPoint(x[i], y[i]));
                model.Series.Add(line);
            }

            //for (int i = 0; i < x.Length; i++)
            //{
            //    var line = new LineSeries();
            //    line.Color = OxyColors.Red;
            //    line.StrokeThickness = 1.5;
            //    line.Points.Add(new DataPoint(x[i], 0));
            //    line.Points.Add(new DataPoint(x[i], intensityModel[i]));
            //    model.Series.Add(line);
            //}
            
            return model;
        }

        public static PlotModel DrawCharEnvelopMatch(MsDataScan msDataScan, ChargeEnvelop chargeEnvelop)
        {
            // x is m/z, y is intensity
            var spectrumMzs = msDataScan.MassSpectrum.XArray;
            var spectrumIntensities = msDataScan.MassSpectrum.YArray;


            PlotModel model = new PlotModel { Title = "Spectrum Annotation of Scan #" + msDataScan.OneBasedScanNumber, DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "m/z", Minimum = 0, Maximum = spectrumMzs.Max() * 1.02, AbsoluteMinimum = 0, AbsoluteMaximum = spectrumMzs.Max() * 5 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Intensity", Minimum = 0, Maximum = spectrumIntensities.Max() * 1.2, AbsoluteMinimum = 0, AbsoluteMaximum = spectrumIntensities.Max() * 1.3 });
            model.Axes[1].Zoom(0, spectrumIntensities.Max() * 1.1);


            // draw the remaining unmatched peaks
            for (int i = 0; i < spectrumMzs.Length; i++)
            {
                var line = new LineSeries();
                line.Color = OxyColors.DimGray;
                line.StrokeThickness = 1;
                line.Points.Add(new DataPoint(spectrumMzs[i], 0));
                line.Points.Add(new DataPoint(spectrumMzs[i], spectrumIntensities[i]));
                model.Series.Add(line);
            }

            foreach (var distri in chargeEnvelop.distributions)
            {
                if (distri.isoEnvelop!=null)
                {
                    foreach (var peak in distri.isoEnvelop.ExperimentIsoEnvelop)
                    {
                        var line = new LineSeries();
                        line.Color = OxyColors.Red;
                        line.StrokeThickness = 3;
                        line.Points.Add(new DataPoint(peak.Mz, 0));
                        line.Points.Add(new DataPoint(peak.Mz, peak.Intensity));
                        model.Series.Add(line);
                    }
                }

                    var mzzLine = new LineSeries();
                    mzzLine.Color = OxyColors.Red;
                    mzzLine.StrokeThickness = 3;
                    mzzLine.Points.Add(new DataPoint(distri.mz, 0));
                    mzzLine.Points.Add(new DataPoint(distri.mz, distri.intensity));
                    model.Series.Add(mzzLine);

                    var peakAnno = new TextAnnotation();
                    peakAnno.TextRotation = 90;
                    peakAnno.Font = "Arial";
                    peakAnno.FontSize = 12;
                    peakAnno.TextColor = OxyColors.Red;
                    peakAnno.StrokeThickness = 0;
                    peakAnno.TextPosition = new DataPoint(distri.mz, distri.intensity);
                    peakAnno.Text = distri.charge.ToString() + "+";
                    model.Annotations.Add(peakAnno);
                
            }

            // Axes are created automatically if they are not defined      
            return model;
        }

        public static PlotModel UpdataModelForChargeEnve(PlotModel model, List<(int charge, double mz, double intensity, int index)> mz_zs)
        {
            Random rand = new Random();
            int colorId = rand.Next(0, 14);

            foreach (var mzz in mz_zs)
            {
                var mzzLine = new LineSeries();
                
                mzzLine.Color = GlycoViewModel.oxyColors[colorId];
                mzzLine.StrokeThickness = 2;
                mzzLine.Points.Add(new DataPoint(mzz.mz, 0));
                mzzLine.Points.Add(new DataPoint(mzz.mz, mzz.intensity));
                model.Series.Add(mzzLine);

                var peakAnno = new TextAnnotation();
                peakAnno.TextRotation = 90;
                peakAnno.Font = "Arial";
                peakAnno.FontSize = 12;
                peakAnno.TextColor = OxyColors.Red;
                peakAnno.StrokeThickness = 0;
                peakAnno.TextPosition = new DataPoint(mzz.mz, mzz.intensity);
                peakAnno.Text = mzz.charge.ToString() + "+";
                model.Annotations.Add(peakAnno);
            }

            // Axes are created automatically if they are not defined      
            return model;
        }

        public static PlotModel ResetDeconModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "ChargeEnve Spectrum Annotation", Subtitle = "using OxyPlot" };

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
