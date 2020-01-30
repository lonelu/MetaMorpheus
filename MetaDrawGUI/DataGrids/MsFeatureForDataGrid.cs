using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class MsFeatureForDataGrid
    {
        public MsFeatureForDataGrid(MsFeature msFeature)
        {
            MsFeature = msFeature;
            if (msFeature.ScanNum > 0)
            {
                ScanNum = msFeature.ScanNum;
            }
            MonoMass = msFeature.MonoMass;
            Abundance = msFeature.Intensity;
            ApexRT = msFeature.ApexRT;
        }

        public int ScanNum { get; set; }
        public double MonoMass { get; set; }
        public double Abundance { get; set; }
        public double ApexRT { get; set; }

        public MsFeature MsFeature;
    }
}
