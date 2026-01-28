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
            const int serverPort = 5002;

            // Dictionary: kljuc = "broj-proizvodjac", vrednost = lista vremena krugova
            Dictionary<string, List<double>> resultByLap = new Dictionary<string, List<double>>();

            // Kreiranje TCP socket-a
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, serverPort);

            serverSocket.Bind(localEndPoint);
            serverSocket.Listen(10);

            Console.WriteLine("Direkcija trke je pokrenuta.");
            Console.WriteLine($"IP: {IPAddress.Any}");
            Console.WriteLine($"Port: {serverPort}");
            Console.WriteLine("Čeka se povezivanje automobila...");


            Socket clientSocket = serverSocket.Accept();
            IPEndPoint remoteEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;

            Console.WriteLine($"Automobil povezan sa adrese: {remoteEndPoint.Address}:{remoteEndPoint.Port}");

            //prijem podataka
            byte[] buffer = new byte[1024];

            //prihvatanje klijenata (bolida)
            while (true)
            {
                try
                {
                    int bytesRecieved = clientSocket.Receive(buffer);
                    if (bytesRecieved == 0)
                    {
                        Console.WriteLine("Klijent je zavrsio sa radom (nema podataka).");
                        continue;
                    }
                    string message = Encoding.UTF8.GetString(buffer);

                    Console.WriteLine(message);
                    if (message == "kraj")
                    {
                        Console.WriteLine("Server je zavrsio sa radom po zahtevu klijenta.");
                        break;
                    }


                    string[] parts = message.Split(';');
                    if (parts.Length != 2)
                    {
                        Console.WriteLine($"Neispravan format poruke: {message}");
                        continue;
                    }

                    string response = Console.ReadLine();

                    bytesRecieved = clientSocket.Send(Encoding.UTF8.GetBytes(response));
                    if (response == "kraj")
                    {
                        Console.WriteLine("Server je zavrsio sa radom po zahtevu korisnika.");
                        break;
                    }
           
                    string key = parts[0]; 
                    double lapTime; 

                    if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lapTime))
                    {
                        Console.WriteLine($"Neispravno vreme kruga: '{parts[1]}'");
                        continue;
                    }

                    //smestanje podataka u Dictionary
                    if (!resultByLap.ContainsKey(key))
                    {
                        resultByLap[key] = new List<double>();
                    }

                    resultByLap[key].Add(lapTime);
                    Console.WriteLine($"Primljeni podaci -> {key}, vreme kruga: {lapTime}s");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do Socket greske: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greska: {ex.Message}");
                    break;
                }
            }
            Console.WriteLine("Server zavrsava sa radom");
            Console.ReadKey();
            clientSocket.Close();
            serverSocket.Close();
        }
    }
}