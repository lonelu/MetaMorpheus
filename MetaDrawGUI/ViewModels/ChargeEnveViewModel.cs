using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using System.ComponentModel;
using MassSpectrometry;
using MetaDrawGUI;
using System.Collections.Generic;

namespace ViewModels
{
    public class ChargeEnveViewModel
    {
        public PlotModel privateModel;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ChargeEnveViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "ChargeEnve Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.privateModel = tmp;
        }

        public void UpdataModelForChargeEnve_old(MsDataScan MsScanForDraw, ChargeDeconEnvelope chargeDeconEnvelope)
        {
            var x = chargeDeconEnvelope.mzFit;
            var y = chargeDeconEnvelope.intensitiesFit;
            var z = chargeDeconEnvelope.intensitiesModel;
            var charges = chargeDeconEnvelope.chargeStates;
            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();

            var isoEnves = chargeDeconEnvelope.isotopicEnvelopes;

            PlotModel model = new PlotModel { Title = "Spectrum anotation of Scan " + scanNum, DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Bottom,
                Title = "m/z",
                Minimum = 0,
                Maximum = MsScanForDraw.MassSpectrum.XArray.Max() * 1.02,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = MsScanForDraw.MassSpectrum.XArray.Max() * 1.2
            });

            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left,
                Title = "Intensity(counts)",
                Minimum = 0,
                Maximum = chargeDeconEnvelope.intensitiesFit.Max() * 1.2,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = chargeDeconEnvelope.intensitiesFit.Max() * 1.3
            });

            LineSeries[] sPeaks = new LineSeries[chargeDeconEnvelope.mzFit.Length];
            for (int i = 0; i < x.Length; i++)
            {
                sPeaks[i] = new LineSeries();
                sPeaks[i].Color = OxyColors.DimGray;
                sPeaks[i].StrokeThickness = 1.5;
                sPeaks[i].Points.Add(new DataPoint(x[i], 0));
                sPeaks[i].Points.Add(new DataPoint(x[i], y[i]));
                model.Series.Add(sPeaks[i]);
            }

            //Original peaks
            for (int i = 0; i < isoEnves.Count; i++)
            {
                foreach (var isoEnve in isoEnves)
                {
                    foreach (var peak in isoEnve.peaks)
                    {
                        var sPeak = new LineSeries();
                        sPeak.Color = OxyColors.Black;
                        sPeak.StrokeThickness = 1.5;
                        sPeak.Points.Add(new DataPoint(peak.mz, 0));
                        sPeak.Points.Add(new DataPoint(peak.mz, peak.intensity));
                        model.Series.Add(sPeak);
                    }
                }
            }

            LineSeries[] sPeaksModel = new LineSeries[chargeDeconEnvelope.mzFit.Length];
            for (int i = 0; i < x.Length; i++)
            {
                sPeaksModel[i] = new LineSeries();
                sPeaksModel[i].Color = OxyColors.Red;
                sPeaksModel[i].StrokeThickness = 1;
                sPeaksModel[i].Points.Add(new DataPoint(x[i], 0));
                sPeaksModel[i].Points.Add(new DataPoint(x[i], z[i]));
                model.Series.Add(sPeaksModel[i]);

                var peakAnno = new TextAnnotation();
                peakAnno.TextRotation = 90;
                peakAnno.Font = "Arial";
                peakAnno.FontSize = 12;
                peakAnno.TextColor = OxyColors.Red;
                peakAnno.StrokeThickness = 0;
                peakAnno.TextPosition = sPeaksModel[i].Points[1];
                peakAnno.Text = charges[i].ToString();
                model.Annotations.Add(peakAnno);
            }
            model.Axes[0].AxisChanged += XAxisChanged;
            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.privateModel = model;
        }

        public static PlotModel UpdataModelForChargeEnve(MsDataScan msDataScan, Dictionary<int, MzPeak> mz_zs)
        {
            // x is m/z, y is intensity
            var spectrumMzs = msDataScan.MassSpectrum.XArray;
            var spectrumIntensities = msDataScan.MassSpectrum.YArray;


            PlotModel model = new PlotModel { Title = "Spectrum Annotation of Scan #" + msDataScan.OneBasedScanNumber, DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "m/z", Minimum = 0, Maximum = spectrumMzs.Max() * 1.02, AbsoluteMinimum = 0, AbsoluteMaximum = spectrumMzs.Max() * 5 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Intensity", Minimum = 0, Maximum = spectrumIntensities.Max() * 1.2, AbsoluteMinimum = 0, AbsoluteMaximum = spectrumIntensities.Max() * 1.3 });
            model.Axes[1].Zoom(0, spectrumIntensities.Max() * 1.1);

            LineSeries[] allIons = new LineSeries[spectrumMzs.Length];

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
                allIons[i].StrokeThickness = 1;
                allIons[i].Points.Add(new DataPoint(spectrumMzs[i], 0));
                allIons[i].Points.Add(new DataPoint(spectrumMzs[i], spectrumIntensities[i]));
                model.Series.Add(allIons[i]);
            }

            foreach (var mzz in mz_zs)
            {
                var mzzLine = new LineSeries();
                mzzLine.Color = OxyColors.Red;
                mzzLine.StrokeThickness = 3;
                mzzLine.Points.Add( new DataPoint( mzz.Value.Mz, 0));
                mzzLine.Points.Add(new DataPoint(mzz.Value.Mz, mzz.Value.Intensity));
                model.Series.Add(mzzLine);

                var peakAnno = new TextAnnotation();
                peakAnno.TextRotation = 90;
                peakAnno.Font = "Arial";
                peakAnno.FontSize = 12;
                peakAnno.TextColor = OxyColors.Red;
                peakAnno.StrokeThickness = 0;
                peakAnno.TextPosition = new DataPoint(mzz.Value.Mz, mzz.Value.Intensity);
                peakAnno.Text = mzz.Key.ToString() + "+";
                model.Annotations.Add(peakAnno);
            }

            // Axes are created automatically if they are not defined      
            return model;
        }

        public void ResetDeconModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "ChargeEnve Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.privateModel = tmp;
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
