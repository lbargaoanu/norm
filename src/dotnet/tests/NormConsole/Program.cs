using Microsoft.Extensions.Configuration;
using Mil.Navy.Nrl.Norm;
using Mil.Navy.Nrl.Norm.Buffers;
using Mil.Navy.Nrl.Norm.Enums;
namespace NormConsole
{
    internal class Program
    {
        readonly NormInstance _normInstance = new();
        readonly NormSession _normSession;
        readonly ByteBuffer _dataBuffer = ByteBuffer.AllocateDirect(10*1024);
        readonly Mutex _mutex;
        public Program()
        {
            var configuration = new ConfigurationBuilder().AddXmlFile("settings.xml").Build();
            var robustFactor = int.Parse(configuration["robust-factor"]);
            var roundTripTime = double.Parse(configuration["round-trip-time"]);
            _mutex = new(default, "NormConsole", out var first);
            _normSession = ConfigureNorm();
            new Thread(Events).Start();
            NormSession ConfigureNorm()
            {
                var nodeId = first ? NormNode.NORM_NODE_ANY : Environment.ProcessId;
                var normSession = _normInstance.CreateSession(address: "224.1.2.3", port: 6565, nodeId);
                normSession.SetTxRobustFactor(robustFactor);
                normSession.SetDefaultRxRobustFactor(robustFactor);
                normSession.GrttEstimate = roundTripTime;
                if(roundTripTime != 0)
                {
                    normSession.SetGrttProbingMode(NormProbingMode.NORM_PROBE_NONE);
                }
                normSession.SetRxPortReuse(true);
                normSession.SetLoopback(true);
                normSession.StartSender(bufferSpace: 1024 * 1024, segmentSize: 1400, blockSize: 64, numParity: 16);
                normSession.StartReceiver(bufferSpace: 1024 * 1024);
                return normSession;
            }
        }
        static void Main() => new Program().Run();
        void Run()
        {
            while(true)
            {
                _normSession.DataEnqueue(_dataBuffer, 0, (int)_dataBuffer.ByteLength);
                Console.WriteLine($"{DateTime.Now.TimeOfDay} Press ENTER to send data");
                Console.ReadLine();
            }
        }
        void Events()
        {
            while(true)
            {
                var normEvent = _normInstance.GetNextEvent();
                if(normEvent?.Type == NormEventType.NORM_RX_OBJECT_COMPLETED)
                {
                    Console.WriteLine($"{DateTime.Now.TimeOfDay} Received data from {normEvent.Node.Address}");
                }
            }
        }
    }
}