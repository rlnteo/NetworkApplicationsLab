using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/*DirekcijaTrke - TCP server
 * otvara TCP socket
 * pisemo info o socketu
 * prima podatke od automobila
 * smesta ih u Dictionary<string, List<double>>
*/

namespace DirekcijaTrke
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //“Pri pokretanju, server otvara TCP utičnicu i ispisuje podatke o njoj”
            const int serverPort = 5000;

            // Dictionary: kljuc = "broj-proizvodjac", vrednost = lista vremena krugova
            Dictionary<string, List<double>> rezultatPoKrugu = new Dictionary<string, List<double>>();

            // Kreiranje TCP socket-a
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, serverPort);

            serverSocket.Bind(localEndPoint);
            serverSocket.Listen(10);

            Console.WriteLine("Direkcija trke je pokrenuta.");
            Console.WriteLine($"IP: {IPAddress.Any}");
            Console.WriteLine($"Port: {serverPort}");
            Console.WriteLine("Čeka se povezivanje automobila...");

            //prihvatanje klijenata (bolida)
            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                IPEndPoint remoteEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;

                Console.WriteLine($"Automobil povezan sa adrese: {remoteEndPoint.Address}:{remoteEndPoint.Port}");

                //prijem podataka
                byte[] buffer = new byte[1024];
                int bytesRecieved = clientSocket.Receive(buffer);
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRecieved);

                //KAKAV FORMAT PORUKE OCEKUJEMO: "16-Ferrari;71.278"
                string[] delovi = message.Split(';');
                string kljuc = delovi[0]; // "16-Ferrari"
                double vremeKruga = double.Parse(delovi[1]); // 71.278

                //smestanje podataka u Dictionary
                if (!rezultatPoKrugu.ContainsKey(kljuc))
                {
                    rezultatPoKrugu[kljuc] = new List<double>();
                }

                rezultatPoKrugu[kljuc].Add(vremeKruga);
                Console.WriteLine($"Primljeni podaci -> {kljuc}, vreme kruga: {vremeKruga}s");

                //zatvaranje konekcije sa klijentom
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
        }
    }
}