using System.Net.Sockets;
using System.Net;
using HL7Processor.Core.Communication.MLLP;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HL7Processor.Tests.Communication;

public class MLLPServerTests
{
    private readonly MLLPServer _server;
    private readonly int _port;

    public MLLPServerTests()
    {
        _port = 5500 + Random.Shared.Next(1000);
        _server = new MLLPServer(new NullLogger<MLLPServer>(), new MLLPServerOptions { IPAddress = IPAddress.Loopback, Port = _port });
    }

    [Fact]
    public async Task Server_Should_Echo_Ack()
    {
        string? receivedMessage = null;
        MLLPAcknowledgment? ackFromEvent = null;

        _server.MessageReceived += (_, e) => receivedMessage = e.Message;
        _server.AcknowledgmentSent += (_, e) => ackFromEvent = e.Acknowledgment;

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var serverTask = _server.StartAsync(cts.Token);

        // Give server time to start
        await Task.Delay(200);

        var message = "MSH|^~\\&|SND|SND|RCV|RCV|20230101120000||ADT^A01|MSG0001|P|2.5\rPID|1||12345||DOE^JOHN\r";
        var mllpBytes = MLLPProtocol.WrapMessage(message);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, _port);
        var stream = client.GetStream();
        await stream.WriteAsync(mllpBytes);

        // Read ACK
        var buffer = new byte[4096];
        var read = await stream.ReadAsync(buffer);
        Assert.True(read > 0);
        var ack = MLLPProtocol.UnwrapMessage(buffer[..read]);

        Assert.Contains("ACK", ack);
        Assert.Equal(message, receivedMessage);
        Assert.NotNull(ackFromEvent);

        await _server.StopAsync();
        await serverTask;
    }
} 