using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Garaza
{
    internal class Program
    {
        private const int AutoUdpPort = 6000;
        private const int GarazaUdpPortZaPrijemStanja = 6001;
        static void Main(string[] args)
        {
            Socket tcpSocket = new Socket(
              AddressFamily.InterNetwork,
              SocketType.Stream,
              ProtocolType.Tcp
            );

            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 5000);
            tcpSocket.Bind(serverEP);
            tcpSocket.Listen(2);

            Console.WriteLine("Garaža je pokrenuta.");
            Console.WriteLine("\n---------------------------------------\n");
            Console.WriteLine($"TCP utičnica otvorena na adresi: {serverEP}");
            Console.WriteLine("\n---------------------------------------\n");
            Console.WriteLine();

            Console.Write("Unesite dužinu staze (u km): ");
            string unosStaze = Console.ReadLine();
            double duzinaStaze = double.Parse(unosStaze);

            Console.Write("Unesite osnovno vreme kruga (u sekundama): ");
            string unosVremena = Console.ReadLine();
            double osnovnoVreme = double.Parse(unosVremena);


            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint udpEP = new IPEndPoint(IPAddress.Loopback, AutoUdpPort);

            Console.WriteLine("\n---------------------------------------\n");
            Console.WriteLine($"UDP utičnica otvorena na portu {udpEP.Port}");
            Console.WriteLine("\n---------------------------------------\n");

            string porukaStaza = $"Konfiguracija staze: {duzinaStaze},{osnovnoVreme}";
            byte[] dataStaza = Encoding.UTF8.GetBytes(porukaStaza);

            udpSocket.SendTo(
                dataStaza,
                0,
                dataStaza.Length,
                SocketFlags.None,
                udpEP
            );

            Console.WriteLine("Poslata konfiguracija staze automobilu:");
            Console.WriteLine(porukaStaza);
            Console.WriteLine();

            Console.WriteLine($"Izaberite komponentu guma (M/S/T): ");
            char gume = Console.ReadKey().KeyChar;
            Console.WriteLine();

            Console.WriteLine($"Unesite kolicinu goriva: ");
            double gorivo = double.Parse(Console.ReadLine());

            string poruka = $"Izlazak na stazu: {gume},{gorivo}";

            IPEndPoint autoEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6000);
            byte[] data = Encoding.UTF8.GetBytes(poruka);
            udpSocket.SendTo(data, 0, data.Length, SocketFlags.None, autoEP);

            Console.WriteLine($"Direktiva poslata automobilu:\n {poruka}");

            // 3) UDP prijem stanja od automobila (6001)
            Socket udpSocketRecv = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocketRecv.Bind(new IPEndPoint(IPAddress.Any, GarazaUdpPortZaPrijemStanja));


            Console.WriteLine();
            Console.WriteLine("Garaža prima stanje na UDP 6001.");
            Console.WriteLine("Komande tempa: b=Brze, s=Sporije, m=Srednje, q=izlaz");

            byte[] buf = new byte[1024];

            while (true)
            {
                // Non-blocking tempo input
                if (Console.KeyAvailable)
                {
                    char c = char.ToLowerInvariant(Console.ReadKey(true).KeyChar);
                    if (c == 'q') break;

                    string tempoMsg = null;
                    if (c == 'b') tempoMsg = "Tempo: Brze";
                    else if (c == 's') tempoMsg = "Tempo: Sporije";
                    else if (c == 'm') tempoMsg = "Tempo: Srednje";

                    if (tempoMsg != null)
                    {
                        byte[] tdata = Encoding.UTF8.GetBytes(tempoMsg);
                        udpSocket.SendTo(tdata, autoEP);
                        Console.WriteLine("Poslata direktiva -> " + tempoMsg);
                    }
                }

                // Blokirajuće čekanje stanja (može i sa timeoutom, ali ovako je najjednostavnije)
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                int n = udpSocketRecv.ReceiveFrom(buf, ref from);
                string stanje = Encoding.UTF8.GetString(buf, 0, n).Trim();

                Console.WriteLine("Primljeno stanje: " + stanje);
            }

            Console.WriteLine();
            Console.WriteLine("Garaža završava sa radom.");
            udpSocket.Close();
            tcpSocket.Close();
        }
    }
}
