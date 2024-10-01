using Newtonsoft.Json;
using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ParticleAcceleratorMonitoring
{
    public abstract class Sensor
    {
        private const int UPDATE_INTERVAL = 500; // Polling interval in milliseconds
        private const int DELIMITER = 0x13;   // Delimiter for incoming TCP messages

        private string ipString;
        private int port;
        private SimpleTcpServer server;
        private Thread broadcastThread;
        private BroadcastThread broadcastThreadObject;

        Random random = new Random();
        public readonly ILogger<Sensor> logger;

        public int id;

        // Sensor value ranges based on LHC data
        public double MIN_RANDOM_VALUE;
        public double MAX_RANDOM_VALUE;
        public abstract double MAX_TOLERANCE { get; }
        public abstract double MIN_TOLERANCE { get; }
        public abstract double MAX_SAFE_VALUE { get; }
        public abstract double MIN_SAFE_VALUE { get; }
        public abstract string UNITS { get; }
        public abstract string SENSOR_TYPE { get; }

        public volatile bool stopBroadcastThread = false; // Controls the broadcast thread's execution
        public readonly object _broadcastLock = new object(); // Lock for thread-safe broadcasting

        protected SensorState _state = SensorState.OFF;

        public readonly object _stateLock = new object(); // Lock for thread-safe state changes
        
        public abstract double ReadValue();

        // Generates a random sensor reading for simulation purposes
        protected double RandomReadValue()
        {
            return random.NextDouble() * (MAX_RANDOM_VALUE - MIN_RANDOM_VALUE) + MIN_RANDOM_VALUE;
        }

        
        public Sensor()
        {
            MIN_RANDOM_VALUE = MIN_SAFE_VALUE - MIN_TOLERANCE * MIN_SAFE_VALUE;
            MAX_RANDOM_VALUE = MAX_SAFE_VALUE + MAX_TOLERANCE * MAX_SAFE_VALUE;
        }

        
        public Sensor(string ip, int port, int id, ILogger<Sensor> logger) : this() 
        {
            this.ipString = ip;
            this.port = port;
            this.id = id;
            this.logger = logger;

            // Set up TCP server and message handling
            server = new SimpleTcpServer
            {
                Delimiter = DELIMITER,
                StringEncoder = Encoding.UTF8
            };
            server.DelimiterDataReceived += Server_DataReceived;

            _state = SensorState.ON;

        }

        
        public SensorState GetState()
        {
            lock (_stateLock) return _state;
        }

        
        public void SetState(SensorState newState)
        {
            lock (_stateLock) _state = newState;
        }

        // Starts the sensor server and begins broadcasting data periodically
        public void Start()
        {
            
            // Exception handled in MonitoringService.cs
            server.Start(port);

            logger.LogInformation("Sensor server started successfully.");

            // Start the broadcast thread for periodic data transmission
            broadcastThreadObject = new BroadcastThread(this.server, UPDATE_INTERVAL, this);
            broadcastThread = new Thread(broadcastThreadObject.MainLoop);
            broadcastThread.Start();
            
        }

        // Stops the sensor server and the broadcast thread
        public void Stop()
        {
            stopBroadcastThread = true;
            SetState(SensorState.OFF);

            try
            {
                // Wait for the broadcast thread to complete
                if (broadcastThread != null && broadcastThread.IsAlive)
                {
                    broadcastThread.Join();
                }

                server.Stop();
                logger.LogInformation("Sensor stopped.");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error stopping sensor server: {ex.Message}");
            }
        }

        // Handles state change events and replies with an acknowledgment
        private void ParseStateChangeEvent(SimpleTCP.Message e, Dictionary<string, string> dataDict)
        {
            try
            {

                string stateString = dataDict["state"];
                if (!Enum.TryParse(stateString, out SensorState state))
                    throw new Exception("Cannot parse stateString: " + stateString + "to enum State");
                    
                SetState(state);

                // Send a response acknowledging the state change
                var response = new
                {
                    id = id,
                    type = SENSOR_TYPE,
                    state = state
                };
                string jsonString = JsonConvert.SerializeObject(response);
                lock (_broadcastLock)
                    e.Reply(jsonString);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing state change event: {ex.Message}");
            }
        }

        // Handles incoming data from the TCP server
        private void Server_DataReceived(object sender, SimpleTCP.Message e)
        {

            try
            {
                string msg = e.MessageString;
                Dictionary<string, string> dataDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(msg);

                // Respond to 'ping' messages with current sensor info
                if (dataDict.ContainsKey("ping"))
                {
                    var data = new
                    {
                        id = this.id,
                        type = SENSOR_TYPE,
                        pong = DateTime.Now.Ticks
                    };
                    string jsonString = JsonConvert.SerializeObject(data);
                    lock (_broadcastLock)
                        e.Reply(jsonString);
                }

                // Process state change requests
                if (dataDict.ContainsKey("state"))
                {

                    // In some cases, the action requires a specific type to execute. If the type is not provided, it is considered unnecessary.
                    // However, if the type is provided, it is validated to ensure it matches the expected type before proceeding.
                    if (dataDict.ContainsKey("type"))
                    {
                        if(dataDict["type"].Equals(SENSOR_TYPE))
                            ParseStateChangeEvent(e, dataDict);
                    }
                    else
                    {
                        // General state change processing
                        ParseStateChangeEvent(e, dataDict);
                    }
                }
            }
            catch (JsonException ex)
            {
                logger.LogError($"Error parsing JSON message: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error handling received data: {ex.Message}");
            }
        }
    }
}
