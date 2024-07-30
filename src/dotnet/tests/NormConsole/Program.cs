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
        public Program()
        {
            _normSession = _normInstance.CreateSession("224.1.2.3", 6565, NormNode.NORM_NODE_ANY);
            _normSession.StartSender(1024 * 1024, 1400, 64, 16);
            _normSession.StartReceiver(1024 * 1024);
            _eventsThread = new(Events);
            _eventsThread.Start();
        }

        static void Main() => new Program().Run();

        void Run()
        {
            _normSession.SetLoopback(true);
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