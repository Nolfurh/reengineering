using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer;

/// <summary>
/// This program was designed for test purposes only
/// Not for a review
/// </summary>
[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(string[] args)
    {
        EchoServer server = new EchoServer(5000);

        server.Start();

        string host = "127.0.0.1"; // Target IP
        int port = 60000;          // Target Port
        int intervalMilliseconds = 5000; // Send every 3 seconds

        using (var sender = new UdpTimedSender(host, port))
        {
            Console.WriteLine("Press any key to stop sending...");
            sender.StartSending(intervalMilliseconds);

            Console.WriteLine("Press 'q' to quit...");
            while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
            {
                // Just wait until 'q' is pressed
            }

            sender.StopSending();
            server.Stop();
            Console.WriteLine("Sender stopped.");
        }
    }
}