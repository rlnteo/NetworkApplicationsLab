using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automobil
{
    public class KonfiguracijaAutomobila
    {
        public string Marka { get; set; }
        public double PotrosnjaGuma { get; set; }
        public double PotrosnjaGoriva { get; set; }

        public KonfiguracijaAutomobila(string marka, double potrosnjaGuma, double potrosnjaGoriva)
        {
            Marka = marka;
            PotrosnjaGuma = potrosnjaGuma;
            PotrosnjaGoriva = potrosnjaGoriva;
        }
    }
}
