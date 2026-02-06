using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Garaza
{
    internal class Program
    {
        private const int AutoUdpPort = 6000;
        private const int GarazaUdpPortZaPrijemStanja = 6001;

        static void Main(string[] args)
        {
            // ===== PODACI O AUTOMOBILIMA (MAX 2) =====
            List<Socket> autoTcpSockets = new List<Socket>();
            Dictionary<Socket, EndPoint> autoUdpEndpoints = new Dictionary<Socket, EndPoint>();
            HashSet<string> alarmiraniAutomobili = new HashSet<string>();


            // ===== TCP SERVER (GARAŽA) =====
            Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 5000);
            tcpSocket.Bind(serverEP);
            tcpSocket.Listen(2);
            tcpSocket.Blocking = false;

            Console.WriteLine("Garaža je pokrenuta.");
            Console.WriteLine($"TCP utičnica: {serverEP}");
            Console.WriteLine("---------------------------------------");

            // ===== UNOS STAZE =====
            Console.Write("Unesite dužinu staze (u km): ");
            double duzinaStaze = double.Parse(Console.ReadLine());

            Console.Write("Unesite osnovno vreme kruga (u sekundama): ");
            double osnovnoVreme = double.Parse(Console.ReadLine());

            // ===== UDP SOCKET ZA SLANJE =====
            Socket udpSocketSend = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // ===== UDP SOCKET ZA PRIJEM STANJA =====
            Socket udpSocketRecv = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocketRecv.Bind(new IPEndPoint(IPAddress.Any, GarazaUdpPortZaPrijemStanja));
            udpSocketRecv.Blocking = false;

            Console.WriteLine("UDP prijem stanja na portu 6001");
            Console.WriteLine("---------------------------------------");

            // ===== IZBOR GUMA I GORIVA =====
            Console.Write("Izaberite komponentu guma (M/S/T): ");
            char gume = Console.ReadKey().KeyChar;
            Console.WriteLine();

            Console.Write("Unesite količinu goriva: ");
            double gorivo = double.Parse(Console.ReadLine());

            string porukaStaza = $"Konfiguracija staze: {duzinaStaze},{osnovnoVreme}";
            string porukaIzlazak = $"Izlazak na stazu: {gume},{gorivo}";

            byte[] dataStaza = Encoding.UTF8.GetBytes(porukaStaza);
            byte[] dataIzlazak = Encoding.UTF8.GetBytes(porukaIzlazak);

            byte[] buf = new byte[1024];

            Console.WriteLine("\n\nKomande: b=brže, s=sporije, m=srednje, q=izlaz\n");


            // ================== GLAVNA PETLJA ==================
            while (true)
            {
                List<Socket> checkRead = new List<Socket> { tcpSocket, udpSocketRecv };
                foreach (var s in autoTcpSockets)
                    checkRead.Add(s);

                Socket.Select(checkRead, null, null, 1000 * 1000);

                // ===== PRIHVATANJE NOVIH AUTOMOBILA =====
                if (checkRead.Contains(tcpSocket))
                {
                    while (autoTcpSockets.Count < 2)
                    {
                        try
                        {
                            Socket auto = tcpSocket.Accept();
                            auto.Blocking = false;
                            autoTcpSockets.Add(auto);

                            byte[] tmp = new byte[1024];
                            int n = auto.Receive(tmp);
                            string msg = Encoding.UTF8.GetString(tmp, 0, n);

                            if (msg.StartsWith("UDPPORT:"))
                            {
                                int port = int.Parse(msg.Split(':')[1]);

                                EndPoint autoUdpEP = new IPEndPoint(
                                    ((IPEndPoint)auto.RemoteEndPoint).Address,
                                    port
                                );

                                autoUdpEndpoints[auto] = autoUdpEP;

                                Console.WriteLine($"Automobil povezan: {auto.RemoteEndPoint}, UDP port {port}");

                                // Pošalji konfiguraciju
                                udpSocketSend.SendTo(dataStaza, autoUdpEP);
                                udpSocketSend.SendTo(dataIzlazak, autoUdpEP);
                            }

                        }
                        catch
                        {
                            break;
                        }
                    }
                }

                // ===== KOMANDE TEMPA SA TASTATURE =====
                if (Console.KeyAvailable)
                {
                    char c = char.ToLowerInvariant(Console.ReadKey(true).KeyChar);

                    string tempoMsg = null;
                    if (c == 'b') tempoMsg = "Tempo: Brze";
                    else if (c == 's') tempoMsg = "Tempo: Sporije";
                    else if (c == 'm') tempoMsg = "Tempo: Srednje";
                    else if (c == 'q') tempoMsg = "Izlazak";


                    if (tempoMsg != null)
                    {
                        byte[] tdata = Encoding.UTF8.GetBytes(tempoMsg);
                        
                        Console.Write("Kom vozilu zelite da posaljete komandu (1 ili 2): ");
                        char izborAuta = Console.ReadKey().KeyChar;
                        Console.WriteLine();

                        int index = izborAuta == '2' ? 1 : 0;

                        if (index < autoTcpSockets.Count)
                        {
                            Socket autoSocket = autoTcpSockets[index];
                            EndPoint ep = autoUdpEndpoints[autoSocket];
                            udpSocketSend.SendTo(tdata, ep);
                        }



                        Console.WriteLine("Poslata direktiva: " + tempoMsg);
                    }
                }

                // ===== PRIJEM STANJA OD AUTOMOBILA =====
                if (checkRead.Contains(udpSocketRecv))
                {
                    EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                    int n = udpSocketRecv.ReceiveFrom(buf, ref from);
                    string stanje = Encoding.UTF8.GetString(buf, 0, n);
                    Console.WriteLine("Primljeno stanje: " + stanje);

                    // Auto 1-Mercedes | Gume: 73.60, Gorivo: 6.00
                    string[] delovi = stanje.Split('|');

                    // Identitet auta
                    string autoInfo = delovi[0].Trim();
                    string[] autoParts = autoInfo.Split(' ')[1].Split('-');

                    string trkackiBroj = autoParts[0];
                    string marka = autoParts[1];

                    // Vrijednosti
                    string[] vrednosti = delovi[1].Split(',');

                    double stanjeGume = double.Parse(
                        vrednosti[0].Split(':')[1],
                        System.Globalization.CultureInfo.InvariantCulture
                    );

                    double stanjeGorivo = double.Parse(
                        vrednosti[1].Split(':')[1],
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    double ukupnoGume = gume == 'M' ? 80 : gume == 'S' ? 100 : 120;
                    double procenatGuma = stanjeGume / ukupnoGume;
                    bool alarmGume = stanjeGume < 0.25 * ukupnoGume;

                    double gorivoZaDvaKruga = 2 * duzinaStaze * PotrosnjaGorivaPoMarki(marka);
                    bool alarmGorivo = gorivo < gorivoZaDvaKruga;

                    string autoKey = $"{trkackiBroj}-{marka}";

                    if (!alarmiraniAutomobili.Contains(autoKey) && (alarmGorivo || alarmGume))
                    {
                        if (alarmGorivo)
                            Console.WriteLine($"ALARM ({autoKey}): nema goriva za 2 kruga!");

                        if (alarmGume)
                            Console.WriteLine($"ALARM ({autoKey}): gume ispod 25%!");

                        alarmiraniAutomobili.Add(autoKey);

                        byte[] izlaz = Encoding.UTF8.GetBytes("Izlazak");

                        Socket autoSocket = autoUdpEndpoints
                            .First(x => ((IPEndPoint)x.Value).Address.Equals(((IPEndPoint)from).Address))
                            .Key;

                        udpSocketSend.SendTo(izlaz, autoUdpEndpoints[autoSocket]);
                    }


                }
            }

            // ===== ZATVARANJE =====
            udpSocketSend.Close();
            udpSocketRecv.Close();
            tcpSocket.Close();
            Console.WriteLine("Garaža završava sa radom.");
        }
        static double PotrosnjaGorivaPoMarki(string marka)
        {
            switch (marka)
            {
                case "Mercedes": return 0.6;
                case "Ferari": return 0.5;
                case "Reno": return 0.7;
                case "Honda": return 0.6;
                default: return 0.6;
            }
        }

    }
}
