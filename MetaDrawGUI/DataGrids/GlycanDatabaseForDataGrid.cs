using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class GlycanDatabaseForDataGrid
    {
        public GlycanDatabaseForDataGrid(int id, string comp, string structure)
        {
            Id = id;
            Composition = comp;
            Structure = structure;
        }

        public int Id { get; set; }
        public string Composition { get; set; }
        public string Structure { get; set; }
    }
}
