using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Garaza
{
    internal class Program
    {
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
            IPEndPoint udpEP = new IPEndPoint(IPAddress.Loopback, 6000);

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

            Console.WriteLine();
            Console.WriteLine("Garaža završava sa radom.");
            udpSocket.Close();
            tcpSocket.Close();
        }
    }
}
