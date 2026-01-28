using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Automobil
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Izaberite proizvođača automobila:");
            Console.WriteLine("1 - Mercedes");
            Console.WriteLine("2 - Ferari");
            Console.WriteLine("3 - Reno");
            Console.WriteLine("4 - Honda");

            int izbor = int.Parse(Console.ReadLine());

            KonfiguracijaAutomobila konfiguracija = null;

            switch (izbor)
            {
                case 1:
                    konfiguracija = new KonfiguracijaAutomobila("Mercedes", 0.3, 0.6);
                    Console.WriteLine("Izabran Mercedes");
                    break;

                case 2:
                    konfiguracija = new KonfiguracijaAutomobila("Ferari", 0.3, 0.5);
                    Console.WriteLine("Izabran Ferari");
                    break;

                case 3:
                    konfiguracija = new KonfiguracijaAutomobila("Reno", 0.4, 0.7);
                    Console.WriteLine("Izabran Reno");
                    break;

                case 4:
                    konfiguracija = new KonfiguracijaAutomobila("Honda", 0.2, 0.6);
                    Console.WriteLine("Izabrana Honda");
                    break;

                default:
                    Console.WriteLine("Neispravan izbor!");
                    return;
            }

            Console.WriteLine($"Marka automobila: {konfiguracija.Marka}");
            Console.WriteLine($"Potrošnja guma: {konfiguracija.PotrosnjaGuma}");
            Console.WriteLine($"Potrošnja goriva: {konfiguracija.PotrosnjaGoriva} l/km");


            Socket tcpSocket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

            IPEndPoint garazaEP = new IPEndPoint(IPAddress.Loopback, 5000);

            tcpSocket.Connect(garazaEP);

            Console.WriteLine("TCP konekcija sa garažom uspostavljena.");




            UdpClient udpClient = new UdpClient(0);

            IPEndPoint udpEP = udpClient.Client.LocalEndPoint as IPEndPoint;

            Console.WriteLine($"UDP utičnica otvorena na portu {udpEP.Port}");


            Console.WriteLine("Izaberite komponentu guma:");
            Console.WriteLine("M - Meke (80 km)");
            Console.WriteLine("S - Srednje (100 km)");
            Console.WriteLine("T - Tvrde (120 km)");

            char gume = char.Parse(Console.ReadLine().ToUpper());

            double maxTrajanjeGuma = 0;

            switch (gume)
            {
                case 'M':
                    maxTrajanjeGuma = 80;
                    break;
                case 'S':
                    maxTrajanjeGuma = 100;
                    break;
                case 'T':
                    maxTrajanjeGuma = 120;
                    break;
                default:
                    Console.WriteLine("Neispravan izbor guma!");
                    return;
            }

            double trenutnoStanjeGuma = maxTrajanjeGuma;
            double trenutnoGorivo = 100; // početno gorivo

            Console.WriteLine($"Gume: {gume}, trajanje {maxTrajanjeGuma} km");
            Console.WriteLine($"Početno gorivo: {trenutnoGorivo} litara");

        }
    }
}
