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
        private const int GarazaUdpPortZaSimulaciju = 6001;
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
            clientSocket.Connect(garazaEP);

            Console.WriteLine("\n---------------------------------------\n");
            Console.WriteLine("TCP konekcija sa garažom uspostavljena:");
            Console.WriteLine($"Lokalna adresa: {clientSocket.LocalEndPoint}");
            Console.WriteLine($"Adresa garaže: {clientSocket.RemoteEndPoint}");
            Console.WriteLine("\n---------------------------------------\n");


            Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint udpEP = new IPEndPoint(IPAddress.Any, 6000);
            udpClient.Bind(udpEP);

            udpClient.Blocking = false;

            Console.WriteLine("\n---------------------------------------\n");
            Console.WriteLine($"UDP utičnica otvorena:");
            Console.WriteLine($"Lokalna adresa: {udpEP.Address}:{udpEP.Port}");
            Console.WriteLine("\n---------------------------------------\n");

            // endpoint garaže na koji ŠALJEMO stanje posle kruga
            EndPoint garazaUdpEP = new IPEndPoint(IPAddress.Loopback, GarazaUdpPortZaSimulaciju);

            byte[] prijemniBafer = new byte[1024];
            EndPoint posiljalacEP = new IPEndPoint(IPAddress.Any, 0);

            double duzinaStaze = 0;
            double osnovnoVreme = 0;
            bool stazaPrimljena = false;

            bool izlazakPrimljen = false;
            string izlazakGuma = null;
            double izlazakGorivo = 0;


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
                        // PORUKA: TEMPO (opciono)
                        // ===============================
                        else if (poruka.StartsWith("Tempo"))
                        {
                            // Primeri: "Tempo: Brze", "Tempo: Sporije", "Tempo: Srednje"
                            string[] delovi = poruka.Split(':');
                            string tempoTxt = delovi.Length > 1 ? delovi[1].Trim().ToLowerInvariant() : "";

                            if (tempoTxt == "brze")
                                konfiguracija.Tempo = KonfiguracijaAutomobila.TempoVoznje.Brze;
                            else if (tempoTxt == "sporije")
                                konfiguracija.Tempo = KonfiguracijaAutomobila.TempoVoznje.Sporije;
                            else
                                konfiguracija.Tempo = KonfiguracijaAutomobila.TempoVoznje.Srednje;

                            Console.WriteLine("Primljena direktiva tempa: " + konfiguracija.Tempo);
                        }
                        // ===============================
                        // PORUKA: IZLAZAK NA STAZU
                        // ===============================
                        else if (poruka.StartsWith("Izlazak na stazu") && stazaPrimljena == true)
                        {
                            string[] delovi = poruka.Split(':');
                            string[] vrednosti = delovi[1].Trim().Split(',');

                            izlazakGuma = vrednosti[0].Trim();
                            izlazakGorivo = double.Parse(vrednosti[1], CultureInfo.InvariantCulture);
                            izlazakPrimljen = true;

                            Console.WriteLine($"Primljen izlazak na stazu -> gume: {izlazakGuma}, gorivo: {izlazakGorivo}");

                        }
                        if (stazaPrimljena && izlazakPrimljen)
                        {
                            konfiguracija.StanjeGuma = izlazakGuma;
                            konfiguracija.TrenutnoGume = TrajanjeGuma(konfiguracija.StanjeGuma);
                            konfiguracija.TrenutnoGorivo = izlazakGorivo;

                            Console.WriteLine($"Gume: {konfiguracija.StanjeGuma}, trajanje {konfiguracija.TrenutnoGume} km");
                            Console.WriteLine($"Početno gorivo: {konfiguracija.TrenutnoGorivo} litara");
                            Console.WriteLine($"Dužina: {duzinaStaze} km");

                            SimulirajVoznju(udpClient, garazaUdpEP, duzinaStaze, osnovnoVreme, konfiguracija);
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
                try { udpClient.Close(); } catch { }
                try { clientSocket.Close(); } catch { }
            }

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
        static void SimulirajVoznju(Socket udpClient, EndPoint garazaUdpEP, double duzinaStaze, double osnovnoVreme, KonfiguracijaAutomobila konfiguracija)
        {
            Socket direkcijaTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            direkcijaTcp.Connect(new IPEndPoint(IPAddress.Loopback, 5002));

            string trkackiBroj = PreuzmiTrkackiBroj(direkcijaTcp);
            Console.WriteLine("Trkački broj: " + trkackiBroj);

            int brojKruga = 1;

            Console.WriteLine("\n********************* Automobil je izašao na stazu *********************\n");

            // ukupan kapacitet guma (za 35% uslov)
            double ukupnoGume = TrajanjeGuma(konfiguracija.StanjeGuma);
            double vremeKruga = osnovnoVreme;

            while (konfiguracija.TrenutnoGorivo > 0 && konfiguracija.TrenutnoGume > 0)
            {
                ObradiSveTempoPoruke(udpClient, konfiguracija);
                // 1) Potrošnja (azurira se odmah na konfiguraciji)
                double potrosnjaGuma = konfiguracija.PotrosnjaGuma;
                double potrosnjaGoriva = konfiguracija.PotrosnjaGoriva;

                if (konfiguracija.Tempo == KonfiguracijaAutomobila.TempoVoznje.Brze)
                {
                    potrosnjaGuma += 0.3;
                    potrosnjaGoriva += 0.3;
                }

                konfiguracija.TrenutnoGume -= duzinaStaze * potrosnjaGuma;
                konfiguracija.TrenutnoGorivo -= duzinaStaze * potrosnjaGoriva;

                if (konfiguracija.TrenutnoGorivo <= 0)
                {
                    Console.WriteLine("Nema goriva.");
                    break;
                }

                if (konfiguracija.TrenutnoGume <= 0)
                {
                    Console.WriteLine("Gume su istrošene.");
                    break;
                }

                // 2) Tempo goriva
                double tempoGoriva = 1.0 / konfiguracija.TrenutnoGorivo;

                // 3) Tempo guma (po opisu)
                double tempoGuma;
                if (konfiguracija.StanjeGuma == "M") tempoGuma = 1.2 * brojKruga;
                else if (konfiguracija.StanjeGuma == "S") tempoGuma = brojKruga;
                else tempoGuma = 0.8 * brojKruga;

                // 4) Poseban slučaj: < 35% guma => tempoGuma - 0.6
                double procenatGuma = ukupnoGume <= 0 ? 0.0 : (konfiguracija.TrenutnoGume / ukupnoGume);
                if (procenatGuma < 0.35)
                {
                    tempoGuma -= 0.6;
                }

                // 5) Vreme kruga (KT)
                vremeKruga = osnovnoVreme - (tempoGoriva + tempoGuma);

                // Sporije: vreme se uvećava za 0.2 za svaki novi krug
                if (konfiguracija.Tempo == KonfiguracijaAutomobila.TempoVoznje.Sporije)
                {
                    vremeKruga += 0.2 * brojKruga;
                }

                // Zaštita da ne postane 0/negativno (inače dobijaš "nerealne uslove")
                if (vremeKruga < 1.0)
                {
                    vremeKruga = 1.0;
                }
                // 6) Ispis + sleep na vreme kruga
                Console.WriteLine($"Krug {brojKruga}");
                Console.WriteLine($"Tempo: {konfiguracija.Tempo}");
                Console.WriteLine($"Vreme kruga: {vremeKruga:F2} s");
                Console.WriteLine($"Preostalo gorivo: {konfiguracija.TrenutnoGorivo:F2} l");
                Console.WriteLine($"Preostale gume: {konfiguracija.TrenutnoGume:F2} km");
                Console.WriteLine("--------------------------------");

                Thread.Sleep((int)(vremeKruga * 1000));

                // (E) pošalji Direkciji vreme kruga: "broj-proizvodjac;vreme"
                string key = trkackiBroj + "-" + konfiguracija.Marka;
                string porukaDirekciji = key + ";" + vremeKruga.ToString(CultureInfo.InvariantCulture);
                direkcijaTcp.Send(Encoding.UTF8.GetBytes(porukaDirekciji));


                // (F) pošalji Garaži stanje (UDP)
                string stanje = "Stanje: Gume - " +
                                konfiguracija.TrenutnoGume.ToString(CultureInfo.InvariantCulture) + ", Gorivo - " +
                                konfiguracija.TrenutnoGorivo.ToString(CultureInfo.InvariantCulture);

                byte[] dataStanje = Encoding.UTF8.GetBytes(stanje);
                udpClient.SendTo(dataStanje, 0, dataStanje.Length, SocketFlags.None, garazaUdpEP);


                brojKruga++;
            }

            Console.WriteLine("\nAutomobil završava vožnju.\n");

            try { direkcijaTcp.Close(); } catch { }
        }

        private static string PreuzmiTrkackiBroj(Socket direkcijaTcp)
        {
            // Minimalno uklapanje sa postojećom Direkcijom:
            // Direkcija parsira "key;time" i zatim šalje nazad ono što operator ukuca.
            // Pošaljemo inicijalnu poruku i čekamo odgovor.
            string init = "prijava;0";
            direkcijaTcp.Send(Encoding.UTF8.GetBytes(init));

            byte[] buf = new byte[1024];
            int n = direkcijaTcp.Receive(buf);
            return Encoding.UTF8.GetString(buf, 0, n).Trim();
        }

        private static void ObradiSveTempoPoruke(Socket udpClient, KonfiguracijaAutomobila konfiguracija)
        {
            try
            {
                while (udpClient.Poll(0, SelectMode.SelectRead))
                {
                    EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    byte[] b = new byte[1024];
                    int n = udpClient.ReceiveFrom(b, ref ep);

                    string poruka = Encoding.UTF8.GetString(b, 0, n).Trim();
                    if (!poruka.StartsWith("Tempo"))
                        continue;

                    string[] delovi = poruka.Split(':');
                    string tempoTxt = delovi.Length > 1 ? delovi[1].Trim().ToLowerInvariant() : "";

                    if (tempoTxt == "brze")
                        konfiguracija.Tempo = KonfiguracijaAutomobila.TempoVoznje.Brze;
                    else if (tempoTxt == "sporije")
                        konfiguracija.Tempo = KonfiguracijaAutomobila.TempoVoznje.Sporije;
                    else
                        konfiguracija.Tempo = KonfiguracijaAutomobila.TempoVoznje.Srednje;
                }
            }
            catch (SocketException)
            {
                // non-blocking socket: ignore
            }
        }
    }
}
