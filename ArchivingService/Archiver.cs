using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
namespace ParticleAcceleratorMonitoring
{
    public class Archiver : SensorListener
    {
        private string SavePath = "";

        // Thread-safe queues used for saving data to files
        private List<QueueProcessor<SensorData>> QueueProcessors = new List<QueueProcessor<SensorData>>();
        private ThreadSafeList<object> _locks = new ThreadSafeList<object>();

        private string SaveDirectory;

        public Archiver(int id, ILogger<Archiver> logger) : base(id, logger)
        {
            // Load configuration values from appsettings.json
            SaveDirectory = Program.Configuration?["AppSettings:ArchiverSettings:SaveDirectory"] ?? throw new ArgumentNullException(nameof(Program.Configuration));

            // Create save path to : Documents\[SaveDirectory]\DD_MM_YYYY_H--M--S--MS
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            SavePath = Path.Combine(documentsPath, SaveDirectory);
            string date = string.Format("{0}_{1}_{2}_{3}-{4}-{5}-{6}",
                               DateTime.Now.Day,
                               DateTime.Now.Month,
                               DateTime.Now.Year,
                               DateTime.Now.Hour,
                               DateTime.Now.Minute,
                               DateTime.Now.Second,
                               DateTime.Now.Millisecond);

            SavePath = Path.Combine(SavePath, date);
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }

            // Setup queue processors for each sensor
            for (int i = 0; i < SENSOR_COUNT; i++)
            {
                object _lock = new object();
                _locks.Add(_lock);

                // Arguments given: 1. callback function, 2. lock
                QueueProcessors.Add(new QueueProcessor<SensorData>(AppendToFile, _lock));
            }
            logger.LogInformation("Archiver initialized.");
        }

        // Processes the data after the base class finishes its work
        protected override void ChildRecieveData(object sender, SimpleTCP.Message e, Dictionary<string, string> dataDict)
        {
            if (dataDict.ContainsKey("readings"))
            {
                int id = int.Parse(dataDict["id"]);
                double readings = double.Parse(dataDict["readings"]);
                long tics = long.Parse(dataDict["tics"]);
                string fileName = $"Sensor{id}data.txt";

                SensorData sensorData = new SensorData(readings, tics, fileName);
                QueueProcessors[id].AddToQueue(sensorData);
            }
        }

        // Appends data to a file
        internal void AppendToFile(SensorData data)
        {
            string fileName = data.filename;
            string message = data.ToString();
            string filePath = Path.Combine(SavePath, fileName);

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, true)) // append = true
                {
                    writer.WriteLine(message.TrimEnd());
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error appending to file: {ex.Message}");
            }
        }

        // Waits for all queue processors to finish processing
        public async Task WaitForQueuesToFinishAsync()
        {
            var tasks = new List<Task>();

            foreach (var queueProcessor in QueueProcessors)
            {
                tasks.Add(queueProcessor.WaitForCompletionAsync());
            }

            await Task.WhenAll(tasks);
            logger.LogInformation("All queues have finished processing.");
        }

        // Inner class representing sensor data
        internal class SensorData : IComparable<SensorData>
        {
            public double readings;
            public long tics;
            public string filename;

            public SensorData(double readings, long tics, string filename)
            {
                this.readings = readings;
                this.tics = tics;
                this.filename = filename;
            }

            public int CompareTo(SensorData? other)
            {
                if (other == null)
                {
                    return 1;
                }

                return this.tics.CompareTo(other.tics); // Sort by timestamp (ascending)
            }

            public override string ToString()
            {
                return $"Timestamp: {tics} Readings: {readings}";
            }
        }
    }
}
