using Mil.Navy.Nrl.Norm;
using Mil.Navy.Nrl.Norm.Enums;

namespace NormConsole
{
    internal class Program
    {
        readonly NormInstance _normInstance = new();
        readonly NormSession _normSession;
        readonly Thread _eventsThread;

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