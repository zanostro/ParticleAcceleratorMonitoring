using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Core;
using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;


namespace ParticleAcceleratorMonitoring
{
    public class SensorListener : Form
    {

        protected readonly ILogger<SensorListener> logger;

        protected int id;

        protected readonly string ip;
        protected int PORT_START;
        protected readonly int RESERVERD_PORT_COUNT;    // Ports reserved for alarming clients
        protected readonly int SENSOR_COUNT;
        protected readonly int DELIMITER;
        protected volatile bool isConnected = true;

        // Thread lock for broadcasting
        public readonly object _broadcastLock = new object();

        // ID allocation scheme
        protected readonly int CLIENT_ID_START;
        protected readonly int ARCHIVING_SERVICE_ID;
        protected readonly int MONITORING_SERVICE_ID;

        protected List<SimpleTcpClient> SensorClients = new List<SimpleTcpClient>();

        public SensorListener(int id, ILogger<SensorListener> logger)
        {
            this.id = id;
            this.logger = logger;
            // Load values from configuration
            ip = Program.Configuration["AppSettings:SERVER_IP"] ?? "127.0.0.1";
            PORT_START = int.Parse(Program.Configuration["AppSettings:PORT_START"] ?? "9000");
            RESERVERD_PORT_COUNT = int.Parse(Program.Configuration["AppSettings:RESERVERD_PORT_COUNT"] ?? "2");
            SENSOR_COUNT = int.Parse(Program.Configuration["AppSettings:SENSOR_COUNT"] ?? "4");
            DELIMITER = int.Parse(Program.Configuration["AppSettings:DELIMITER"] ?? "" + 0x13);
            CLIENT_ID_START = int.Parse(Program.Configuration["AppSettings:CLIENT_ID_START"] ?? "-3");
            ARCHIVING_SERVICE_ID = int.Parse(Program.Configuration["AppSettings:ARCHIVING_SERVICE_ID"]);
            MONITORING_SERVICE_ID = int.Parse(Program.Configuration["AppSettings:MONITORING_SERVICE_ID"]);
        }

        public SensorListener() { }

       


        // ----------------------------------------------------------------------------------------------------------------------------------------------------
        // Sends data to all connected sensors

        protected void SendToSensor(SimpleTcpClient sensor, object data)
        {
            try
            {
                string jsonString = JsonConvert.SerializeObject(data);
                sensor.WriteLine(jsonString);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error sending data to sensor: {ex.Message}");
            }

        }


        protected void SendToAllSensors(object data)
        {
            foreach (SimpleTcpClient tcpClient in SensorClients)
            {
                SendToSensor(tcpClient, data);
            }  
        }

        // Updates sensor status by sending the new state to the sensor
        protected void UpdateSensorStatus(SimpleTcpClient TCPClient, SensorState state)
        { 
            var data = new
            {
                id = this.id,
                state = state
            };
            SendToAllSensors(data);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //
        // Connection-related methods

        // Initializes and creates a new TCP client
        protected SimpleTcpClient StartTCPClient(int port)
        {
            try
            {
                SimpleTcpClient TCPClient = new SimpleTcpClient
                {
                    StringEncoder = Encoding.UTF8,
                    Delimiter = (byte) DELIMITER
                };
                TCPClient.DataReceived += BaseRecieveData;
                TCPClient.Connect(ip, port);

                return TCPClient;
            }
            catch (SocketException ex)
            {
                logger.LogError($"Error connecting to TCP client on port {port}: {ex.Message}");
                throw;
            }
        }

        // Connects to all sensors
        public void StartListening()
        {
            for (int i = 0; i < SENSOR_COUNT; i++)
            {
                int port = PORT_START + RESERVERD_PORT_COUNT + i;
                SimpleTcpClient TCPClient = StartTCPClient(port);
                SensorClients.Add(TCPClient);
            }
        }

        // Disconnects from all sensors
        public void StopListening()
        {
            isConnected = false;
            foreach (SimpleTcpClient SensorClient in SensorClients)
            {
                SensorClient.DataReceived -= BaseRecieveData;
                SensorClient.Disconnect();
            }
            SensorClients.Clear();
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        //
        // Message handling section

        // Reformats incoming message data by splitting concatenated JSON strings
        // If the sensor sends a ping reply message simultaneously with data, the two JSON objects may get concatenated.
        // To handle this, we split the combined string on the '}' character to separate and parse the individual JSON objects.
        // Limitation: Nested JSON objects cannot be handled by this approach, but since nested structures are not used here, this is acceptable.

        protected Dictionary<string, string> ReformatIncomingMsgData(string msg)
        {
            List<Dictionary<string, string>> jsons = new List<Dictionary<string, string>>();
            string[] split = msg.Split('}'); // Split two JSON objects concatenated by '}' - limitation: cannot handle nested JSON objects

            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].Length == 0) split[i] += "{"; // Convert empty string to valid JSON
                split[i] += "}"; // Re-add missing closing bracket

                try
                {
                    jsons.Add(JsonConvert.DeserializeObject<Dictionary<string, string>>(split[i]));
                }
                catch (JsonException ex)
                {
                    logger.LogError($"Error deserializing JSON: {ex.Message}");
                }
            }

            // Combines all JSON objects into a single dictionary
            Dictionary<string, string> dataDict = new Dictionary<string, string>();
            foreach (Dictionary<string, string> dict in jsons)
            {
                foreach (var kvp in dict)
                {
                    if (!dataDict.ContainsKey(kvp.Key))
                        dataDict.Add(kvp.Key, kvp.Value);
                }
            }

            return dataDict;
        }

       

        // Processes received data from sensors
        protected void ProcessData(object sender, SimpleTCP.Message e)
        {
            try
            {
                if (!isConnected || IsDisposed) return;

                string msg = e.MessageString;
                Dictionary<string, string> dataDict = ReformatIncomingMsgData(msg);

                if (dataDict.ContainsKey("ping"))
                {
                    var data = new
                    {
                        id = this.id,
                        pong = DateTime.Now.Ticks
                    };
                    string jsonString = JsonConvert.SerializeObject(data);
                    e.Reply(jsonString);
                }

                if (dataDict.ContainsKey("state"))
                {

                    string stateString = dataDict["state"];
                    if (!Enum.TryParse(stateString, out SensorState state))
                        throw new Exception("Cannot parse stateString: " + stateString + "to enum State");
                   
                    int sensorId = int.Parse(dataDict["id"]);
                }

                ChildRecieveData(sender, e, dataDict); // Passes control to child classes
            }
            catch (Exception ex)
            {
                logger.LogError("Error trying to process data: " + ex.ToString());
            }
        }

        // Intended to be overridden by child classes
        protected virtual void ChildRecieveData(object sender, SimpleTCP.Message e, Dictionary<string, string> dataDict) { }

        // Handles base data reception, verifying if the parent process is still running
        protected void BaseRecieveData(object sender, SimpleTCP.Message e)
        {
            if (!isConnected || IsDisposed) return;

            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => ProcessData(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                    // Handle parent process being disposed
                    return;
                }
            }
            else
            {
                ProcessData(sender, e);
            }
        }   
    }
}
