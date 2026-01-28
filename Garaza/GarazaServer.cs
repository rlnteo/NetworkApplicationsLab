using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Garaza
{
    public class GarazaServer
    {
        public void Pokreni()
        {
            Socket serverSocket = new Socket(
               AddressFamily.InterNetwork,
               SocketType.Stream,
               ProtocolType.Tcp
             );

            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 19010);
            serverSocket.Bind(serverEP);
            serverSocket.Listen(1);

            Console.WriteLine("Garaža je pokrenuta.");
            Console.WriteLine($"TCP utičnica otvorena na adresi: {serverEP}");
            Console.WriteLine();

            Console.Write("Unesite dužinu staze (u km): ");
            string unosStaze = Console.ReadLine();
            double duzinaStaze = double.Parse(unosStaze);

            Console.Write("Unesite osnovno vreme kruga (u sekundama): ");
            string unosVremena = Console.ReadLine();
            double osnovnoVreme = double.Parse(unosVremena);
     

            
        }
    }
}
