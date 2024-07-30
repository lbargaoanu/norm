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
        readonly Thread _eventsThread;
        readonly ByteBuffer _dataBuffer = ByteBuffer.AllocateDirect(10*1024);
        readonly bool _sender;
        readonly Mutex _mutex;
        public Program()
        {
            var configuration = new ConfigurationBuilder().AddXmlFile("settings.xml").Build();
            _sender = configuration["sender"] != null;
            _mutex = new(true, "NormConsole", out var first);
            _normSession = _normInstance.CreateSession("224.1.2.3", 6565, first ? NormNode.NORM_NODE_ANY : Environment.TickCount64);
            _normSession.SetRxPortReuse(true);
            _normSession.SetLoopback(false);
            if(_sender)
            {
                _normSession.StartSender(1024 * 1024, 1400, 64, 16);
            }
            _normSession.StartReceiver(1024 * 1024);
            _eventsThread = new(Events);
            _eventsThread.Start();
        }

        static void Main() => new Program().Run();

        void Run()
        {
            if(!_sender)
            {
                Console.WriteLine("Waiting for data");
                return;
            }
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
                    Console.WriteLine("Received data");
                }
            }
        }
    }
}