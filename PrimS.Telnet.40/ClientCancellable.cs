namespace PrimS.Telnet
{
  using System;
  using System.Threading;
  using LiteGuard;

  // Referencing https://support.microsoft.com/kb/231866?wa=wsignin1.0 and http://www.codeproject.com/Articles/19071/Quick-tool-A-minimalistic-Telnet-library got me started

  /// <summary>
  /// Basic Telnet client.
  /// </summary>
  public partial class Client
  {
    private readonly TimeSpan timeout;
    private readonly ConnectionMode connectionMode;
    
    /// <summary>
    /// Initialises a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="hostname">The hostname.</param>
    /// <param name="port">The port.</param>
    /// <param name="token">The cancellation token.</param>
    public Client(string hostname, int port, CancellationToken token)
      : this(new TcpByteStream(hostname, port), token)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="byteStream">The stream served by the host connected to.</param>
    /// <param name="token">The cancellation token.</param>
    public Client(IByteStream byteStream, CancellationToken token)
      : this(byteStream, token, new TimeSpan(0, 0, 30), ConnectionMode.OnInitialise)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="byteStream">The stream served by the host connected to.</param>
    /// <param name="token">The cancellation token.</param>
    /// <param name="timeout">The timeout to wait for initial successful connection to <cref>byteStream</cref>.</param>
    /// <param name="connectionMode">Mode for creation of the connection.</param>
    public Client(IByteStream byteStream, CancellationToken token, TimeSpan timeout, ConnectionMode connectionMode)
      : base(byteStream, token)
    {
      Guard.AgainstNullArgument("byteStream", byteStream);
      this.timeout = timeout;
      this.connectionMode = connectionMode;
      if (connectionMode == ConnectionMode.OnInitialise)
      {
        this.Connect();
      }
    }

    /// <summary>
    /// Connects this instance.
    /// </summary>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="System.InvalidOperationException">Unable to connect to the host.</exception>
    public void Connect()
    {
      DateTime timeoutEnd = DateTime.Now.Add(this.timeout);
      var are = new AutoResetEvent(false);
      this.ByteStream.Connect();
      while (!this.ByteStream.IsConnected && timeoutEnd > DateTime.Now)
      {
        are.WaitOne(1);
      }

      if (!this.ByteStream.IsConnected)
      {
        throw new InvalidOperationException("Unable to connect to the host.");
      }
    }

    /// <summary>
    /// Reads from the stream.
    /// </summary>
    /// <param name="timeout">The timeout.</param>
    /// <returns>Any text read from the stream.</returns>
    public string Read(TimeSpan timeout)
    {
      if (this.connectionMode == ConnectionMode.OnDemand && !this.ByteStream.IsConnected)
      {
        this.Connect();
      }

      ByteStreamHandler handler = new ByteStreamHandler(this.ByteStream, this.InternalCancellation);
      return handler.Read(timeout);
    }
  }
}
