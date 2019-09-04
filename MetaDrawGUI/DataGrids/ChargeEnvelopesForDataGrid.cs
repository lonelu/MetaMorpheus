//
using System.Collections.Generic;
using System.Linq;

namespace MetaDrawGUI
{
    public class ChargeEnvelopesForDataGrid
    {
        private new List<double> _deconMasses;

        public ChargeEnvelopesForDataGrid(int ind, double deconMz, double intensity, double mse, double ratio, int isoEnveNum, List<double> deconMasses)
        {
            Ind = ind;
            DeconMz = deconMz;
            Intensity = intensity;
            MSE = mse;
            Ratio = ratio;
            IsoEnveNum = isoEnveNum;
            _deconMasses = deconMasses;
        }

        public int Ind { get; set; }

        public double DeconMz { get; set; }

        public double Intensity { get; set; }

        public double MSE { get; set; }

        public double Ratio { get; set; }

        public double IsoEnveNum { get; set; }

        public string DeconMasses
        {
            get
            {
                return string.Join(",", _deconMasses.Take(3));
            }
        }

    }
}