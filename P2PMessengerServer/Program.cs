using System;
using System.Collections.Generic;
using System.Linq;
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

        public List<TcpClient> Clients { get; private set; }

        public Servidor(IPAddress ip, int porta)
        {
            Ip = ip;
            Porta = porta;

            Rodando = false;
            Clients = new List<TcpClient>();
        }
        
        public async Task Iniciar()
        {
            if (!Rodando)
            {
                Rodando = true;
                _cancellationTokenSource = new CancellationTokenSource();

                TcpListener server = new TcpListener(Ip, Porta);

                server.Start();

                Console.WriteLine("Servidor rodando.");
                Console.WriteLine($"{Ip}:{Porta}");

                while (!_cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = server.AcceptTcpClient();

                    if (client.Connected)
                    {
                        Clients.Add(client);
                        BroadCast(client);
                    }
                }
            }
        }

        private async Task BroadCast(TcpClient client)
        {
            while (!_cancellationToken.IsCancellationRequested && client.Connected)
            {
                try
                {
                    NetworkStream ns = client.GetStream();
                        
                    byte[] msg = new byte[1024];
                    await ns.ReadAsync(msg, 0, msg.Length);
                    foreach (var outroCliente in Clients.Where(c => !c.Equals(client) && c.Connected))
                    {
                        await outroCliente.GetStream().WriteAsync(msg, 0, msg.Length);
                    }
                        
                }
                catch (Exception) { }
            }

            Clients.Remove(client);
        }
    }
}
