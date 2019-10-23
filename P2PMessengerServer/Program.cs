using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace P2PMessengerServer
{
    class Program
    {
        private static string IpServidor;
        private static int PortaServidor;

        public static Servidor Servidor { get; private set; }

        static void Main(string[] args)
        {
            Console.WriteLine("P2PMessengerServer");

            string porta;
            do
            {
                Console.WriteLine("Informe o IP do servidor.");

                IpServidor = Console.ReadLine();

                if (!ValidaIp(IpServidor))
                {
                    Console.Clear();
                    Console.WriteLine("P2PMessengerServer");
                }

            } while (!ValidaIp(IpServidor));

            do
            {
                Console.WriteLine("Informe a porta do servidor.");

                porta = Console.ReadLine();

                if (!int.TryParse(porta, out PortaServidor))
                {
                    Console.Clear();
                    Console.WriteLine("P2PMessengerServer");
                }

            } while (!int.TryParse(porta, out PortaServidor));

            Console.Clear();
            Console.WriteLine("P2PMessengerServer");

            Servidor = new Servidor(IPAddress.Parse(IpServidor), PortaServidor);

            Servidor.Iniciar().Wait();
        }

        private static bool ValidaIp(string ip)
        {
            var regex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");

            return regex.IsMatch(ip);
        }
    }

    public class Servidor
    {
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken => _cancellationTokenSource.Token;

        public bool Rodando { get; private set; }

        public IPAddress Ip { get; private set; }
        public int Porta { get; private set; }

        public Servidor(IPAddress ip, int porta)
        {
            Ip = ip;
            Porta = porta;

            Rodando = false;
        }
        
        public async Task Iniciar()
        {
            if (!Rodando)
            {
                Rodando = true;
                _cancellationTokenSource = new CancellationTokenSource();

                TcpListener server = new TcpListener(Ip, Porta);

                server.Start(); // this will start the server

                Console.WriteLine("Servidor rodando.");

                while (!_cancellationToken.IsCancellationRequested) //we wait for a connection
                {
                    TcpClient client = server.AcceptTcpClient(); //if a connection exists, the server will accept it

                    NetworkStream ns = client.GetStream(); //networkstream is used to send/receive messages

                    if (client.Connected) //while the client is connected, we look for incoming messages
                    {
                        try
                        {
                            byte[] msg = new byte[1024]; //the messages arrive as byte array
                            await ns.ReadAsync(msg, 0, msg.Length); //the same networkstream reads the message sent by the client
                            await ns.WriteAsync(msg, 0, msg.Length);
                        }
                        catch (Exception) { }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(0.5), _cancellationToken);
                }
            }
        }
    }
}
