using Newtonsoft.Json;
using SimpleTCP;
using System;
using System.Threading;

namespace ParticleAcceleratorMonitoring
{
    internal class BroadcastThread
    {
        private SimpleTcpServer server;
        private int sleepInterval;
        private Sensor sensor;
        private QueueProcessor<string> messageQueueProcessor; 
        private readonly object _queueLock = new object(); 

        public BroadcastThread(SimpleTcpServer server, int time, Sensor sensor)
        {
            this.server = server;
            this.sleepInterval = time;
            this.sensor = sensor;

            messageQueueProcessor = new QueueProcessor<string>(server.Broadcast, _queueLock);
        }

        public void MainLoop()
        {
            while (!sensor.stopBroadcastThread)  // Check stop flag in every loop
            {
                Thread.Sleep(sleepInterval);

                // checks for sensor state and takes appropriate action
                bool loop = true;
                while (loop)
                {
                    SensorState state = sensor.GetState();
                    switch (state)
                    {
                        case SensorState.OFF:
                            return;
                        case SensorState.MEASURING:
                            loop = false;
                            break;
                        default:
                            Thread.Sleep(sleepInterval);
                            break;
                    }
                }
                
                long tics = DateTime.Now.Ticks;
                int id = sensor.id;
                string units = sensor.UNITS;
                double readings = sensor.ReadValue();

                // prepare data for broadcasting
                var data = new
                {
                    id = id,
                    type = sensor.SENSOR_TYPE,
                    readings = readings,
                    units = units,
                    tics = tics
                };
                    
                string jsonString = JsonConvert.SerializeObject(data);

                messageQueueProcessor.AddToQueue(jsonString);
                
            }
        }
    }
}
