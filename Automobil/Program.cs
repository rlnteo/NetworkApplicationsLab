using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Automobil
{
    internal class Program
    {
        private static int trkackiBroj;
        static void Main(string[] args)
        {
            KonfiguracijaAutomobila konfiguracija = new KonfiguracijaAutomobila();

            Console.WriteLine("Izaberite proizvođača automobila:");
            Console.WriteLine("1 - Mercedes");
            Console.WriteLine("2 - Ferari");
            Console.WriteLine("3 - Reno");
            Console.WriteLine("4 - Honda");

            int izbor = int.Parse(Console.ReadLine());

            konfiguracija.IzborAutomobila(izbor);

            Console.WriteLine("\nIzabrana marka: " + konfiguracija.Marka);
            Console.WriteLine($"Potrošnja guma: {konfiguracija.PotrosnjaGuma}");
            Console.WriteLine($"Potrošnja goriva: {konfiguracija.PotrosnjaGoriva}");

            Socket clientSocket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

            IPEndPoint garazaEP = new IPEndPoint(IPAddress.Loopback, 5000);
            byte[] buffer = new byte[1024];

            Console.WriteLine("Povezivanje sa garažom...");
            clientSocket.Connect(garazaEP);  //tcp veza ka garaza 
            //Cilj je da se automobil veze sa garazom i potvrdi njeno POSTOJANJE - trenutno se samo konektujemo - garaza ima Accept i sve radii

            Console.WriteLine("\n---------------------------------------\n");
            Console.WriteLine("TCP konekcija sa garažom uspostavljena:");
            Console.WriteLine($"Lokalna adresa: {clientSocket.LocalEndPoint}");
            Console.WriteLine($"Adresa garaže: {clientSocket.RemoteEndPoint}");
            Console.WriteLine("\n---------------------------------------\n");


            Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint udpEP = new IPEndPoint(IPAddress.Any, 6000); 
            udpClient.Bind(udpEP); //otvaranje UDP uticnice za komunikaciju sa garazom nakon odabira proizvodjava

            udpClient.Blocking = false;

            Console.WriteLine("\n---------------------------------------\n");
            Console.WriteLine($"UDP utičnica otvorena:");
            Console.WriteLine($"Lokalna adresa: {udpEP.Address}:{udpEP.Port}");
            Console.WriteLine("\n---------------------------------------\n");


            byte[] prijemniBafer = new byte[1024];
            EndPoint posiljalacEP = new IPEndPoint(IPAddress.Any, 0);

            double duzinaStaze = 0;
            double osnovnoVreme = 0;
            bool stazaPrimljena = false;
            bool izlazakPrimljen = false;


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


                        // ===============================
                        // PORUKA: KONFIGURACIJA STAZE
                        // ===============================
                        if (poruka.StartsWith("Konfiguracija staze"))
                        {
                            string[] dijelovi = poruka.Split(':');
                            string[] vrijednosti = dijelovi[1].Trim().Split(',');

                            duzinaStaze = double.Parse(vrijednosti[0]);
                            osnovnoVreme = double.Parse(vrijednosti[1]);

                            stazaPrimljena = true;

                            Console.WriteLine($"Staza primljena:");
                            Console.WriteLine($"Dužina: {duzinaStaze} km");
                            Console.WriteLine($"Osnovno vreme: {osnovnoVreme} s");
                        }

                        // ===============================
                        // PORUKA: IZLAZAK NA STAZU
                        // ===============================
                        else if (poruka.StartsWith("Izlazak na stazu"))
                        {
                            string[] delovi = poruka.Split(':');
                            string[] vrednosti = delovi[1].Trim().Split(',');

                            string guma = vrednosti[0];
                            double gorivo = double.Parse(vrednosti[1]);

                            int maxTrajanjeGuma = TrajanjeGuma(guma);
                            double trenutnoGorivo = gorivo;

                            izlazakPrimljen = true;

                            Console.WriteLine($"Gume: {guma}, trajanje {maxTrajanjeGuma} km");
                            Console.WriteLine($"Početno gorivo: {trenutnoGorivo} litara");

                            //OVO JE SIGNAL DA SINULACIJA KRECE - PRIMIMO ONFO O TIPU GUME I STANJU GORIVA

                            //TCP prijava Direkciji trke - dobijamo trkacki broj
                            Socket direkcijaSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            direkcijaSocket.Connect(new IPEndPoint(IPAddress.Loopback, 5002));

                            trkackiBroj = PrijaviSeDirekciji(direkcijaSocket, konfiguracija.Marka);  //Prijavi se direkciji salje PRIJAVA;marka , PRIMA BROJ;n - parsira n i vrati ga
                            //CILJ JE DA DOBIJEMO ID (trkackiBroj) koji koristimo ka kljuc pri slanju vremena
                            Console.WriteLine($"Prijava Direkciji trke uspešna. Trkački broj: {trkackiBroj}");

                            //simulacija + slanje vremena Direkciji preko istog soktea
                            SimulirajVoznju( duzinaStaze, osnovnoVreme, guma, gorivo, konfiguracija, direkcijaSocket);

                            direkcijaSocket.Close();
                            break;
                        }


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
            finally
            {
                Console.WriteLine("Pritisnite taster za izlaz...");
                Console.ReadKey();
            }
        }

        //TCP nam salje "PRIJAVA;{marka}" i prima "BROJ;{n}" 
        static int PrijaviSeDirekciji(Socket direkcijaSocket, string marka)
        {
            string prijava = "PRIJAVA;" + marka;
            direkcijaSocket.Send(Encoding.UTF8.GetBytes(prijava));

            byte[] buffer = new byte[1024];
            int n = direkcijaSocket.Receive(buffer);
            string odgovor = Encoding.UTF8.GetString(buffer, 0, n); //OVO NAM JE BROJ;{n}

            string[] delovi = odgovor.Split(';');
            if (delovi.Length != 2 || !delovi[0].Equals("BROJ", StringComparison.OrdinalIgnoreCase))
            {
               throw new InvalidOperationException("Nevažeći odgovor od Direkcije trke.");
            }
            return int.Parse(delovi[1], CultureInfo.InvariantCulture);
        }

        static int TrajanjeGuma(string guma)
        {
            switch (guma)
            {
                case "M": return 80;
                case "S": return 100;
                case "T": return 120;
                default: return 0;
            }
        }

        // CILJ PRIJAVA DIREKCIJI I SIMULACIJA VOZNJE je da direkcija dobije poruku tipa.... i UPISUJE U SVOJ DICTIONARY - 
        static void SimulirajVoznju( double duzinaStaze, double osnovnoVreme, string guma, double pocetnoGorivo, KonfiguracijaAutomobila konfiguracija, Socket direkcijaSocket)
        {
            double trenutnoGorivo = pocetnoGorivo;
            double maxTrajanjeGuma = TrajanjeGuma(guma);
            int brojKruga = 1;

            Console.WriteLine("\n********************* Automobil je izašao na stazu *********************\n");

            while (trenutnoGorivo > 0 && maxTrajanjeGuma > 0)
            {
                // POTROŠNJA
                double potrosnjaGuma = konfiguracija.PotrosnjaGuma;
                double potrosnjaGoriva = konfiguracija.PotrosnjaGoriva;

                maxTrajanjeGuma -= (duzinaStaze * potrosnjaGuma);
                trenutnoGorivo -= duzinaStaze * potrosnjaGoriva;

                if (trenutnoGorivo <= 0)
                {
                    Console.WriteLine("Nema goriva.");
                    break;
                }

                // TEMPO GORIVA
                double tempoGoriva = 1 / trenutnoGorivo;

                // TEMPO GUMA
                double tempoGuma = 0;
                if (guma == "M") tempoGuma = 1.2 * brojKruga;
                else if (guma == "S") tempoGuma = brojKruga;
                else if (guma == "T") tempoGuma = 0.8 * brojKruga;

                // POSEBAN SLUČAJ: < 35% GUMA
                double procenatGuma = (double)maxTrajanjeGuma / TrajanjeGuma(guma);
                if (procenatGuma < 0.35)
                {
                    tempoGuma -= 0.6;
                }

                // VREME KRUGA
                double vremeKruga = osnovnoVreme - tempoGoriva - tempoGuma;
                if (vremeKruga <= 0)
                {
                    Console.WriteLine("Vreme kruga je postalo nevažeće.");
                    Console.WriteLine("Automobil završava vožnju zbog nerealnih uslova.\n");
                    break;
                }

                Console.WriteLine($"Krug {brojKruga}");
                Console.WriteLine($"Vreme kruga: {vremeKruga:F2} s");
                Console.WriteLine($"Preostalo gorivo: {trenutnoGorivo:F2} l");
                Console.WriteLine($"Preostale gume: {maxTrajanjeGuma:F2} km");
                Console.WriteLine("--------------------------------");

                // SLANJE TCP vremena Direkciji: "{broj}-{marka};{vreme}"
                string key = trkackiBroj + "-" + konfiguracija.Marka;
                string msg = key + ";" + vremeKruga.ToString(CultureInfo.InvariantCulture);
                direkcijaSocket.Send(Encoding.UTF8.GetBytes(msg));

                if (vremeKruga < 0.1)
                {
                    Console.WriteLine("Vreme kruga je prenisko/negativno. Prekid simulacije.");
                    break;
                }

                int sleepMs = (int)(vremeKruga * 1000); 
                Thread.Sleep(sleepMs);//simulira trajanje kruga 

                brojKruga++;
            }

            Console.WriteLine("\nAutomobil završava vožnju.\n");
        }

    }
}
