using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using EngineLayer.CrosslinkSearch;
using EngineLayer;
using System.ComponentModel;
using MassSpectrometry;

namespace ViewModels
{
    public class DeconViewModel : INotifyPropertyChanged
    {
        private PlotModel deconModel;

        public PlotModel DeconModel
        {
            get
            {
                return this.deconModel;
            }
            set
            {
                this.deconModel = value;
                NotifyPropertyChanged("DeconModel");
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

        public DeconViewModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "Decon Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.DeconModel = tmp;
        }

        public void UpdataModelForDecon(MsDataScan MsScanForDraw, IsotopicEnvelope isotopicEnvelope)
        {
            var x = MsScanForDraw.MassSpectrum.XArray;
            var y = MsScanForDraw.MassSpectrum.YArray;

            string scanNum = MsScanForDraw.OneBasedScanNumber.ToString();

            var peaks = isotopicEnvelope.peaks;

            PlotModel model = new PlotModel { Title = "Spectrum Decon of Scan ", DefaultFontSize = 15 };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "m/z", Minimum = peaks[0].mz - 2, Maximum = peaks.Last().mz + 2 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Intensity(counts)", Minimum = 0, Maximum = peaks.First().intensity * 1.5 });

            
            
            LineSeries[] lsPeaks = new LineSeries[peaks.Count];
            for (int i = 0; i < peaks.Count; i++)
            {
                lsPeaks[i] = new LineSeries();
                lsPeaks[i].Color = OxyColors.Red;
                lsPeaks[i].StrokeThickness = 1;
                lsPeaks[i].Points.Add(new DataPoint(peaks[i].mz, 0));
                lsPeaks[i].Points.Add(new DataPoint(peaks[i].mz, peaks[i].intensity));
                model.Series.Add(lsPeaks[i]);         
            }
            var peakAnno = new TextAnnotation();
            peakAnno.TextRotation = 90;
            peakAnno.Font = "Arial";
            peakAnno.FontSize = 12;
            peakAnno.TextColor = OxyColors.Red;
            peakAnno.StrokeThickness = 0;
            peakAnno.TextPosition = lsPeaks[0].Points[1];
            peakAnno.Text = isotopicEnvelope.monoisotopicMass.ToString("f1") + "@" + isotopicEnvelope.charge.ToString();

            model.Annotations.Add(peakAnno);

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.DeconModel = model;
        }

        public void ResetDeconModel()
        {
            // Create the plot model
            var tmp = new PlotModel { Title = "Decon Spectrum Annotation", Subtitle = "using OxyPlot" };

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.DeconModel = tmp;
        }
    }
}
