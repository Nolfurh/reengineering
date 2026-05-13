using System;
using System.Net;
using System.Net.Sockets;

namespace EchoServer;

public class EchoServer : IDisposable
{
    private readonly int _port;
    private TcpListener? _listener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _acceptLoopTask;
    private bool _disposed = false;


    public EchoServer(int port)
    {
        _port = port;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Start()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start(); 
        Console.WriteLine($"Server started on port {_port}.");

        _acceptLoopTask = Task.Run(() => AcceptClientsAsync());
    }

    private async Task AcceptClientsAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected.");

                _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
            }
            catch (ObjectDisposedException)
            {
                // Listener has been closed
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted || ex.SocketErrorCode == SocketError.Interrupted)
            { 
                break;
            }
        }
        
        Console.WriteLine("Server shutdown.");
    }

    private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (NetworkStream stream = client.GetStream())
        {
            try
            {
                byte[] buffer = new byte[8192];
                int bytesRead;

                while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer.AsMemory(), token)) > 0)
                {
                    // Echo back the received message
                    await stream.WriteAsync(buffer.AsMemory(), token);
                    Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
    }

    public void Stop()
    {
        if (_disposed)
            return;

        _cancellationTokenSource.Cancel();
        _listener?.Stop();

        Console.WriteLine("Server stopped.");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
