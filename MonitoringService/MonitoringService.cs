using Newtonsoft.Json;
using SimpleTCP;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System;
using Microsoft.Extensions.Logging;

namespace ParticleAcceleratorMonitoring
{
    public partial class MonitoringService : SensorListener
    {
        private readonly int CLIENT_COUNT;
        private List<Sensor> Sensors = new List<Sensor>();
        private List<Client> Clients = new List<Client>();
        private Archiver ArchivingService;
        private SimpleTcpServer AlarmServer;

        // Logger instances
        private readonly ILogger<Client> clientLogger;
        private readonly ILogger<Archiver> archiverLogger;
        private readonly ILogger<Sensor> sensorLogger;

        // Tracks sensors that exceed safe readings
        ThreadSafeSensorValueTracker SensorTracker;


        public MonitoringService
            (
            ILogger<MonitoringService> logger, 
            ILogger<Client> clientlogger,
            ILogger<Archiver> archiverLogger,
            ILogger<Sensor> sensorLogger
            )
            : base(int.Parse(Program.Configuration["AppSettings:MONITORING_SERVICE_ID"]), logger)
        {
            InitializeComponent();

            this.clientLogger = clientlogger;
            this.archiverLogger = archiverLogger;
            this.sensorLogger = sensorLogger;

            // Load CLIENT_COUNT from configuration
            CLIENT_COUNT = int.Parse(Program.Configuration["AppSettings:CLIENT_COUNT"]);
            SensorTracker = new ThreadSafeSensorValueTracker(SENSOR_COUNT);

            AlarmServer = new SimpleTcpServer
            {
                Delimiter = (byte)DELIMITER,
                StringEncoder = Encoding.UTF8
            };
            AlarmServer.DelimiterDataReceived += AlarmServer_DataReceived;
            this.sensorLogger = sensorLogger;
        }


        // Emulates a console output
        private void PrintToConsole(string msg)
        {
            if (ConsoleTextbox.InvokeRequired)
            {
                ConsoleTextbox.Invoke(new Action(() => PrintToConsole(msg)));
            }
            else
            {
                ConsoleTextbox.AppendText("  " + msg + Environment.NewLine);
            }
        }

        // Processes data received from sensors after base class processing
        protected override void ChildRecieveData(object sender, SimpleTCP.Message e, Dictionary<string, string> dataDict)
        {
            try
            {
                if (dataDict.ContainsKey("readings"))
                {
                    int id = int.Parse(dataDict["id"]);
                    double readings = double.Parse(dataDict["readings"]);
                    double maxValue = Sensors[id].MAX_SAFE_VALUE;
                    double minValue = Sensors[id].MIN_SAFE_VALUE;

                    ProcessAlarm(id, readings, minValue, maxValue);
                }

                if (dataDict.ContainsKey("pong"))
                {
                    int id = int.Parse(dataDict["id"]);
                    long ticks = long.Parse(dataDict["pong"]);
                    PrintToConsole($"Ping reply from: {id} at: {ticks}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing sensor data: {ex.Message}");
            }
        }


        // Broadcasts changes in alarm state to all clients
        private void BroadcastAlarmChange(int id, int changesMade, int alarmState)
        {
            var data = new
            {
                id,
                changesMade,
                alarmState
            };
            string jsonString = JsonConvert.SerializeObject(data);
            lock (_broadcastLock)
                AlarmServer.Broadcast(jsonString);
        }


        // Checks if sensor readings exceed safe values and broadcasts any changes
        private void ProcessAlarm(int index, double value, double minValue, double maxValue)
        {
            int[] changes = SensorTracker.ExceedsMaxReadings(index, value, minValue, maxValue);
            int changesMade = changes[0];
            int alarmState = changes[2];

            // If there is a change, inform all clients
            if (changesMade != 0)
            {
                BroadcastAlarmChange(index, changesMade, alarmState);
            }
        }

        // Handles data received by the alarm server - which sensors to exclude from alarm tracking or which sensors to include
        private void AlarmServer_DataReceived(object sender, SimpleTCP.Message e)
        {
            try
            {
                // Parse data
                string msg = e.MessageString;
                Dictionary<string, string>? dataDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(msg);
                if(dataDict == null)
                    throw new Exception("Error parsing JSON message.");
                string stateString = dataDict["state"];
                string type = dataDict["type"];


                if (!Enum.TryParse(stateString, out SensorState state))
                    throw new Exception("Cannot parse stateString: " + stateString + "to enum State");

                // Creates a list of indexes which to exclude or include in the alarm tracking
                List<int> list = new List<int>();

                foreach (Sensor sensor in Sensors)
                {
                    if (sensor.SENSOR_TYPE.Equals(type))
                        list.Add(sensor.id);
                }

                // tracks if the alarm should be triggered
                bool alarmState = false;

                // Exclue sensors
                if (state == SensorState.OFF || state == SensorState.IDLE)
                    alarmState = SensorTracker.ExcludeSensors(list);

                // Include sensors
                if (state == SensorState.ON || state == SensorState.MEASURING)
                    alarmState = SensorTracker.IncludeSensors(list);

                // Broadcasts the new alarm state
                foreach (int index in list)
                {
                    BroadcastAlarmChange(index, 0, alarmState ? 1 : 0);
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing alarm server data: {ex.Message}");
            }

        }



        // Starts all sensors
        private void StartSensors()
        {
            PrintToConsole("Starting sensor servers...");
            logger.LogInformation("Starting sensor servers...");
            int index = 0;

            
            Sensors.Add(new TemperatureSensor(ip, PORT_START + RESERVERD_PORT_COUNT + index, index++, sensorLogger));
            Sensors.Add(new PressureSensor(ip, PORT_START + RESERVERD_PORT_COUNT + index, index++, sensorLogger));
            Sensors.Add(new HumiditySensor(ip, PORT_START + RESERVERD_PORT_COUNT + index, index++, sensorLogger));
            Sensors.Add(new RadiationSensor(ip, PORT_START + RESERVERD_PORT_COUNT + index, index++, sensorLogger));

            foreach (Sensor sensor in Sensors)
            {
                try
                {
                    sensor.Start();
                }
                catch (Exception ex)
                { 
                    logger.LogError($"Error starting sensor server: {ex.Message}");
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                }     
            }
            PrintToConsole("All servers started!");
            logger.LogInformation("All servers started!");
        }
        

        // Starts all clients
        private void StartClients()
        {
               for (int i = 0; i < CLIENT_COUNT; i++)
               {
                   int id = CLIENT_ID_START - i;
                   Client client = new Client(clientLogger, id);
                   client.Show();
                   Clients.Add(client);
               }
        }

        // Sends a ping to all connected services
        private void PingAll_Click(object sender, EventArgs e)
        {
            long time = DateTime.Now.Ticks;
            var data = new
            {
                id = base.id,
                ping = time
            };

            string jsonString = JsonConvert.SerializeObject(data);

            try
            {
                foreach (SimpleTcpClient TCPClient in SensorClients)
                {
                    TCPClient.WriteLine(jsonString);
                }

                lock (_broadcastLock)
                    AlarmServer.Broadcast(jsonString);
            }
            catch (Exception ex)
            {
                PrintToConsole($"Error sending ping: {ex.Message}");
                logger.LogError($"Error sending ping: {ex.Message}");
            }
        }

        // Called before the window is rendered
        private void MonitoringService_Load(object sender, EventArgs e)
        {
            this.MaximizeBox = false;
            ArchivingService = new Archiver(ARCHIVING_SERVICE_ID, archiverLogger);
        }

        // Called after the window is first rendered
        private void MonitoringService_Shown(object sender, EventArgs e)
        {
            PrintToConsole(""); // Adds an empty line for visibility

            try
            {
                AlarmServer.Start(PORT_START);
                StartSensors();
                ArchivingService.StartListening();
                StartListening();
                StartClients();

                // Signals all sensors to start measuring
                foreach (SimpleTcpClient TCPClient in SensorClients)
                {
                    UpdateSensorStatus(TCPClient, SensorState.MEASURING);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error starting services: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }

        }

        // Called before the window is closed
        private async void MonitoringService_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Stops all TCP clients
                foreach (SimpleTcpClient TCPClient in SensorClients)
                {
                    UpdateSensorStatus(TCPClient, SensorState.OFF);
                }
                StopListening();

                // Disconnects clients
                foreach (Client client in Clients)
                {
                    client.DisconnectFromAlarm();
                    client.StopListening();
                }

                ArchivingService.StopListening();
                AlarmServer.Stop();

                foreach (Sensor sensor in Sensors)
                {
                    sensor.Stop();
                }

                await ArchivingService.WaitForQueuesToFinishAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error during shutdown: {ex.Message}");
            }
        }
    }
}
