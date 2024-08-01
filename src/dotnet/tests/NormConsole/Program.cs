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
            _mutex = new(default, "NormConsole", out var first);
            var nodeId = first ? NormNode.NORM_NODE_ANY : Environment.ProcessId;
            _normSession = _normInstance.CreateSession(address: "224.1.2.3", port: 6565, nodeId);
            var robustFactor = int.Parse(configuration["robust-factor"]!);
            _normSession.SetTxRobustFactor(robustFactor);
            _normSession.SetDefaultRxRobustFactor(robustFactor);
            if((_normSession.GrttEstimate = double.Parse(configuration["round-trip-time"]!)) != 0)
            {
                _normSession.SetGrttProbingMode(NormProbingMode.NORM_PROBE_NONE);
            }
            _normSession.SetRxPortReuse(true);
            _normSession.SetLoopback(true);
            _normSession.StartSender(bufferSpace: 1024 * 1024, segmentSize: 1400, blockSize: 64, numParity: 16);
            _normSession.StartReceiver(bufferSpace: 1024 * 1024);
            new Thread(Events).Start();
        }
        static void Main() => new Program().Run();
        void Run()
        {
            while(true)
            {
                _normSession.DataEnqueue(_dataBuffer, 0, (int)_dataBuffer.ByteLength);
                Console.WriteLine("Press ENTER to send data");
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
                    Console.WriteLine($"{DateTime.Now.TimeOfDay} Received data");
                }
            }
        }
    }
}