using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class MsFeatureForDataGrid
    {
        public MsFeatureForDataGrid(double monoMass, double abundance, double apexRT)
        {
            MonoMass = monoMass;
            Abundance = abundance;
            ApexRT = apexRT;
        }

        public double MonoMass { get; set; }
        public double Abundance { get; set; }
        public double ApexRT { get; set; }
    }
}
