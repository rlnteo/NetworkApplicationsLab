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


            Socket clientSocket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

            IPEndPoint garazaEP = new IPEndPoint(IPAddress.Loopback, 5000);
            byte[] buffer = new byte[1024];

            Console.WriteLine("Povezivanje sa garažom...");
            clientSocket.Connect(garazaEP);
            Console.WriteLine("TCP konekcija sa garažom uspostavljena:");
            Console.WriteLine($"Lokalna adresa: {clientSocket.LocalEndPoint}");
            Console.WriteLine($"Adresa garaže: {clientSocket.RemoteEndPoint}");


            Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint udpEP = new IPEndPoint(IPAddress.Any, 6000);
            udpClient.Bind(udpEP);

            udpClient.Blocking = false;

            Console.WriteLine($"UDP utičnica otvorena:");
            Console.WriteLine($"Lokalna adresa: {udpEP.Address}:{udpEP.Port}");


            byte[] prijemniBafer = new byte[1024];
            EndPoint posiljalacEP = new IPEndPoint(IPAddress.Any, 0);


            try
            {
                Console.WriteLine("Čekam UDP poruku od garaže...");

                while (true)
                {
                    List<Socket> checkRead = new List<Socket> { udpClient };
                    List<Socket> checkError = new List<Socket> { udpClient };

                    Socket.Select(checkRead, null, checkError, 1000 * 1000);

                    if (checkRead.Count > 0)
                    {
                        int brBajta = udpClient.ReceiveFrom(prijemniBafer, ref posiljalacEP);
                        string poruka = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);

                        Console.WriteLine($"Primljena UDP poruka: {poruka}");

                        // Obrada poruke 
                        string[] delovi = poruka.Split(':');

                        string parametri = delovi[1].Trim(); // "M,50"
                        string[] vrednosti = parametri.Split(',');

                        string guma = vrednosti[0];           // M / S / T
                        double gorivo = double.Parse(vrednosti[1]);

                        // Odredi trajanje guma
                        int maxTrajanjeGuma = 0;

                        if (guma == "M") maxTrajanjeGuma = 80;
                        else if (guma == "S") maxTrajanjeGuma = 100;
                        else if (guma == "T") maxTrajanjeGuma = 120;

                        // Sačuvaj stanje
                        double trenutnoGorivo = gorivo;

                        Console.WriteLine($"Gume: {guma}, trajanje {maxTrajanjeGuma} km");
                        Console.WriteLine($"Početno gorivo: {trenutnoGorivo} litara");

                        break;
                    }

                    if (checkError.Count > 0)
                    {
                        Console.WriteLine("Greška na UDP utičnici.");
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri prijemu podataka: {ex.Message}");
            }


        }
    }
}
