using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automobil
{
    public class KonfiguracijaAutomobila
    {
        public enum TempoVoznje
        {
            Srednje,
            Brze,
            Sporije
        }
        public string Marka { get; set; }
        public string TrkackiBroj { get; set; }
        public double PotrosnjaGuma { get; set; }
        public double PotrosnjaGoriva { get; set; }
        public double TrenutnoGume { get; set; }
        public double TrenutnoGorivo { get; set; }
        public string StanjeGuma { get; set; }
        public bool Aktivan { get; set; } = true;


        public TempoVoznje Tempo { get; set; } = TempoVoznje.Srednje;
        public KonfiguracijaAutomobila() { }
        public KonfiguracijaAutomobila(string marka, double potrosnjaGuma, double potrosnjaGoriva)
        {
            Marka = marka;
            PotrosnjaGuma = potrosnjaGuma;
            PotrosnjaGoriva = potrosnjaGoriva;
        }
        public void IzborAutomobila(int izbor)
        {
            switch (izbor)
            {
                case 1:
                    Marka = "Mercedes";
                    PotrosnjaGuma = 0.3;
                    PotrosnjaGoriva = 0.6;
                    break;

                case 2:
                    Marka = "Ferari";
                    PotrosnjaGuma = 0.3;
                    PotrosnjaGoriva = 0.5;
                    break;

                case 3:
                    Marka = "Reno";
                    PotrosnjaGuma = 0.4;
                    PotrosnjaGoriva = 0.7;
                    break;

                case 4:
                    Marka = "Honda";
                    PotrosnjaGuma = 0.2;
                    PotrosnjaGoriva = 0.6;
                    break;

                default:
                    Console.WriteLine("Neispravan izbor.");
                    break;
            }
        }
    }
}
