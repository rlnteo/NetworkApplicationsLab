using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DirekcijaTrke
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const int serverPort = 5002;

            // key = "broj-proizvodjac", value = lista vremena krugova
            Dictionary<string, List<double>> resultByLap = new Dictionary<string, List<double>>();

            // aktivni automobili na stazi (samo trkacki brojevi)
            HashSet<string> aktivniAutomobili = new HashSet<string>();

            //mapiranje soketa na trkacki broj (da znamo koga uklanjamo)
            Dictionary<Socket, string> brojPoSoketu = new Dictionary<Socket, string>();

            //dodatne info o automobilu
            Dictionary<string, string> markaPoBroju = new Dictionary<string, string>();
            Dictionary<string, IPEndPoint> endPointPoBroju = new Dictionary<string, IPEndPoint>();

            int sledeciBroj = 1;

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, serverPort));
            serverSocket.Listen(10);
            serverSocket.Blocking = false;

            Console.WriteLine("Direkcija trke je pokrenuta.");
            Console.WriteLine($"IP: {IPAddress.Any}");
            Console.WriteLine($"Port: {serverPort}");
            Console.WriteLine("Čeka se povezivanje automobila...");

            List<Socket> clientSockets = new List<Socket>();

            while (true)
            {
                //Socket.Select menja liste -> inicijalizacija u svakoj iteraciji
                List<Socket> checkRead = new List<Socket>();
                List<Socket> checkError = new List<Socket>();

                checkRead.Add(serverSocket);
                checkError.Add(serverSocket);

                for(int i = 0; i < clientSockets.Count; i++)
                {
                    checkRead.Add(clientSockets[i]);
                    checkError.Add(clientSockets[i]);
                }

                Socket.Select(checkRead, null, checkError, 1000*1000);  //1s

                //greske
                if(checkError.Count > 0)
                {
                    foreach (Socket s in checkError)
                    {
                        if(s == serverSocket)
                        {
                            Console.WriteLine("Server socket je prijavio grešku. Gasimo server.");
                            return;
                        }
                        UkloniKlijenta(clientSockets, brojPoSoketu, aktivniAutomobili, markaPoBroju, endPointPoBroju, resultByLap, s);
                    }
                }
                if(checkRead.Count == 0)
                {
                    continue;
                }

                //NOVI KLIJENTI
                if (checkRead.Contains(serverSocket))
                {
                    while (true)
                    {
                        try
                        {
                            Socket client = serverSocket.Accept();
                            client.Blocking = false;
                            clientSockets.Add(client);

                            IPEndPoint remote = client.RemoteEndPoint as IPEndPoint;
                            Console.WriteLine($"Automobil povezan: {remote.Address}:{remote.Port}");

                        }
                        catch (SocketException)
                        {
                            //Nema vise pending accept poziva (non-blocking)
                            break;
                        }
                    }
                    checkRead.Remove(serverSocket);
                }
                // poruke od postojecih klijenata
                foreach (Socket client in checkRead)
                {
                    try
                    {
                        byte[] buffer = new byte[1024];
                        int bytesReceived = client.Receive(buffer);

                        if (bytesReceived == 0)
                        {
                            Console.WriteLine("Klijent je zavrsio sa radom (disconnect).");
                            UkloniKlijenta(clientSockets, brojPoSoketu, aktivniAutomobili, markaPoBroju, endPointPoBroju, resultByLap, client);
                            continue;
                        }
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesReceived).Trim();

                        if (message.StartsWith("Izlazi sa staze:"))
                        {
                            string[] partsKraj = message.Split(';');
                            if (partsKraj.Length == 2)
                            {
                                Console.WriteLine($"Automobil {partsKraj[1]} je završio trku.");
                            }
                            UkloniKlijenta(clientSockets, brojPoSoketu, aktivniAutomobili, markaPoBroju, endPointPoBroju, resultByLap, client);
                            continue;
                        }

                        string[] parts = message.Split(';');
                        if (parts.Length != 2)
                        {
                            Console.WriteLine("Neispravan format poruke: " + message);
                            continue;
                        }
                        // prijava;0 => dodela broja
                        if (parts[0] == "prijava")
                        {
                            string broj = sledeciBroj.ToString();
                            sledeciBroj++;

                            brojPoSoketu[client] = broj;
                            aktivniAutomobili.Add(broj);


                            IPEndPoint remote = client.RemoteEndPoint as IPEndPoint;
                            if (remote != null)
                            {
                                endPointPoBroju[broj] = remote;
                            }


                            client.Send(Encoding.UTF8.GetBytes(broj));
                            Console.WriteLine("Dodeljen trkački broj: " + broj);
                            Console.WriteLine("Aktivni automobili: " + string.Join(", ", aktivniAutomobili));

                            continue;
                        }
                        // key;lapTime
                        string key = parts[0];
                        double lapTime;

                        if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lapTime))
                        {
                            Console.WriteLine("Neispravno vreme kruga: '" + parts[1] + "'");
                            continue;
                        }

                        if (!resultByLap.ContainsKey(key))
                            resultByLap[key] = new List<double>();

                        resultByLap[key].Add(lapTime);

                        Console.WriteLine($"Primljeni podaci -> {key}, vreme kruga: {lapTime}s");

                        // key format: "broj-marka"
                        string[] keyParts = key.Split('-');
                        if (keyParts.Length >= 2)
                        {
                            string broj = keyParts[0];
                            string marka = keyParts[1];
                            markaPoBroju[broj] = marka;
                        }
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Klijent je zavrsio sa radom (socket exception).");
                        UkloniKlijenta(clientSockets, brojPoSoketu, aktivniAutomobili, markaPoBroju, endPointPoBroju, resultByLap, client);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Greška: " + ex.Message);
                        UkloniKlijenta(clientSockets, brojPoSoketu, aktivniAutomobili, markaPoBroju, endPointPoBroju, resultByLap, client);
                    }
                }
            }
        }
        private static void UkloniKlijenta(List<Socket> clientSockets, Dictionary<Socket, string> brojPoSoketu, HashSet<string> aktivniAutomobili,
            Dictionary<string, string> markaPoBroju, Dictionary<string, IPEndPoint> endPointPoBroju, Dictionary<string, List<double>> resultByLap, Socket client)
        {
            try
            {
                string broj;
                if (brojPoSoketu.TryGetValue(client, out broj))
                {
                    brojPoSoketu.Remove(client);
                    aktivniAutomobili.Remove(broj);

                    if (aktivniAutomobili.Count == 0)
                    {
                        IspisiRezultate(resultByLap);
                    }


                    markaPoBroju.Remove(broj);
                    endPointPoBroju.Remove(broj);
                }

                clientSockets.Remove(client);
                client.Close();
            }
            catch { }
        }

        private static void IspisiRezultate(Dictionary<string, List<double>> resultByLap)
        {
            Console.WriteLine();
            Console.WriteLine("===== REZULTATI TRKE =====");

            foreach (var entry in resultByLap)
            {
                string autoKey = entry.Key;          // npr. "1-Mercedes"
                List<double> vremena = entry.Value;

                if (vremena.Count == 0)
                    continue;

                double najbrziKrug = vremena.Min();

                Console.WriteLine($"Automobil {autoKey}:");

                for (int i = 0; i < vremena.Count; i++)
                {
                    Console.WriteLine(
                        $"Krug {i + 1}: {vremena[i]:F2}s"
                    );
                }

                int bestLap = entry.Value.IndexOf(najbrziKrug) + 1;

                Console.WriteLine($"Najbrži krug: {bestLap} ({najbrziKrug:F2}s)" );
                Console.WriteLine();
            }

            Console.WriteLine("===== KRAJ REZULTATA =====");
        }

    }
}