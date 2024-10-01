using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleTCP;
using System.Runtime.InteropServices;



namespace ParticleAcceleratorMonitoring
{
    public partial class Client : SensorListener
    {
        // Close animation paramaters
        private readonly double FADE_SPEED;
        private readonly int TIMER_INTERVAL;

        private readonly string ASK_TO_CONTINUE_STRING;
        private readonly string ASK_TO_CONTINUE_CAPTION;

        private List<Label> sensorOutputLabels = new List<Label>();
        private List<Label> sensorTypeLabels = new List<Label>();
        private List<PictureBox> warningSigns = new List<PictureBox>();

        private SimpleTcpClient monitoringServiceClient;
        private System.Windows.Forms.Timer closeTimer;

        // Used for rounded window corners
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();


        // WM_NCLBUTTONDOWN is a Windows Message that corresponds to the event of a left mouse button click
        // HTCAPTION is a hit-test value that tells Windows which part of the window the WM_NCLBUTTONDOWN message applies to.
        // Used in window dragging
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;

        public Client(ILogger<Client> logger, int id = int.MinValue) : base(id, logger)
        {
            // Load configurations from appsettings.json
            FADE_SPEED = double.Parse(Program.Configuration["AppSettings:FADE_SPEED"] ?? "0.1");
            TIMER_INTERVAL = int.Parse(Program.Configuration["AppSettings:TIMER_INTERVAL"] ?? "1000");
            ASK_TO_CONTINUE_STRING = Program.Configuration["AppSettings:ASK_TO_CONTINUE_STRING"] ?? "Are you sure you want to continue?";
            ASK_TO_CONTINUE_CAPTION = Program.Configuration["AppSettings:ASK_TO_CONTINUE_CAPTION"] ?? "Confirmation";
            InitializeComponent();
           

            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 25, 25));
            monitoringServiceClient = StartTCPClient(PORT_START);

            AttachMouseDownEvent(this);
            this.MouseDown += Form_MouseDown;

            closeTimer = new System.Windows.Forms.Timer
            {
                Interval = TIMER_INTERVAL
            };
            closeTimer.Tick += CloseTimer_Tick;
        }


        // Receives data from the server after the base class has processed it
        protected override void ChildRecieveData(object sender, SimpleTCP.Message e, Dictionary<string, string> dataDict)
        {
            try
            {
                int id = int.Parse(dataDict["id"]);

                // Updates readings on screen
                if (dataDict.ContainsKey("readings"))
                {
                    double readings = double.Parse(dataDict["readings"]);
                    string units = dataDict["units"];
                    string type = dataDict["type"];
                    sensorTypeLabels[id].Text = type;
                    sensorOutputLabels[id].Text = $"{readings} {units}";
                }

                // Updates state of alarm and warnings
                if (dataDict.ContainsKey("changesMade"))
                {
                    int changesMade = int.Parse(dataDict["changesMade"]);
                    int alarmState = int.Parse(dataDict["alarmState"]);

                    AlarmImage.Visible = alarmState == 1;
                    warningSigns[id].Visible = changesMade == 1;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Client: Error processing data: {ex.Message}");
            }
        }


        // Disconnects the client from the monitoring service
        public void DisconnectFromAlarm()
        {
            try
            {
                monitoringServiceClient.Disconnect();
            }
            catch (Exception ex)
            {
                logger.LogError($"Client: Error disconnecting from alarm: {ex.Message}");
            }
        }

        // ---------------------------------------------------------------------------------------------------------
        // Startup-related events

        private void Client_Shown(object sender, EventArgs e)
        {
            try
            {
                StartListening();
            }
            catch (Exception ex)
            {
                logger.LogError($"Client: Error starting client: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void Client_Load(object sender, EventArgs e)
        {
            this.MaximizeBox = false;

            // Initialize label and warning sign lists
            sensorOutputLabels.Add(Sensor0Label);
            sensorOutputLabels.Add(Sensor1Label);
            sensorOutputLabels.Add(Sensor2Label);
            sensorOutputLabels.Add(Sensor3Label);

            sensorTypeLabels.Add(SensorTypeLabel0);
            sensorTypeLabels.Add(SensorTypeLabel1);
            sensorTypeLabels.Add(SensorTypeLabel2);
            sensorTypeLabels.Add(SensorTypeLabel3);

            warningSigns.Add(SensorWarning0);
            warningSigns.Add(SensorWarning1);
            warningSigns.Add(SensorWarning2);
            warningSigns.Add(SensorWarning3);

            SwitchToMonitoring_Click(sender, e);
            logger.LogInformation("Client started.");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Panel management
        private void ShowPanel(Panel panelToShow)
        {
            MonitoringPannel.Visible = false;
            ControlPannel.Visible = false;
            panelToShow.Visible = true;
        }

        // Handles window dragging
        private void Form_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        // Recursively attaches MouseDown event to controls for dragging
        private void AttachMouseDownEvent(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (!(control is System.Windows.Forms.Button)) // Exclude buttons
                    control.MouseDown += Form_MouseDown;
                if (control.HasChildren)
                    AttachMouseDownEvent(control);
            }
        }


        // ---------------------------------------------------------------------------------------------------------
        // Form closing and fade-out animation

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            if (this.Opacity > 0)
            {
                this.Opacity -= FADE_SPEED;
            }
            else
            {
                closeTimer.Stop();
                this.Close();
            }
        }

        private void CloseButton_MouseEnter(object sender, EventArgs e)
        {
            CloseButton.BackColor = Color.FromArgb(140, 47, 32);
        }

        private void CloseButton_MouseLeave(object sender, EventArgs e)
        {
            CloseButton.BackColor = Color.FromArgb(46, 51, 73);
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            closeTimer.Start();
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (isConnected)
                {
                    StopListening();
                    monitoringServiceClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Client: Error closing form: {ex.Message}");

            }
        }

        // ---------------------------------------------------------------------------------------------------------
        // Menu-related events

        private void SwitchToMonitoring_Click(object sender, EventArgs e)
        {
            panlNav.Height = SwitchToMonitoring.Height;
            panlNav.Top = SwitchToMonitoring.Top;
            SwitchToMonitoring.BackColor = Color.FromArgb(46, 51, 73);
            ShowPanel(MonitoringPannel);
        }

        private void SwitchToControl_Click(object sender, EventArgs e)
        {
            panlNav.Height = SwitchToControl.Height;
            panlNav.Top = SwitchToControl.Top;
            SwitchToControl.BackColor = Color.FromArgb(46, 51, 73);
            ShowPanel(ControlPannel);
        }

        private void SwitchToMonitoring_Leave(object sender, EventArgs e)
        {
            SwitchToMonitoring.BackColor = Color.FromArgb(24, 30, 54);
        }

        private void SwitchToControl_Leave(object sender, EventArgs e)
        {
            SwitchToControl.BackColor = Color.FromArgb(24, 30, 54);
        }

        // ---------------------------------------------------------------------------------------------------------
        // User manages sensor state changes

        private object CreateStatusData(string sensorType, SensorState state)
        {
            return new
            {
                id = id,
                state = state,
                type = sensorType
            };
        }

        private bool ConfirmAction(string message, string caption)
        {
            var result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return result == DialogResult.Yes;
        }

        private void ControlSensorState(string sensorType, SensorState state)
        {
            var msgObject = CreateStatusData(sensorType, state);
            SendToAllSensors(msgObject);
        }


        // Notifies the alarm server of a sensor state change
        private void NotifyAlarmServer(SensorState sensorState, string SensorType)
        {
            monitoringServiceClient.WriteLine(JsonConvert.SerializeObject(new
            {
                id = id,
                state = sensorState,
                type = SensorType
            }));
        }

        private void btTemperatureStart_Click(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                ControlSensorState("Temperature Sensor", SensorState.MEASURING);
                NotifyAlarmServer(SensorState.MEASURING, "Temperature Sensor");
            });
        }
        private void btTemperatureStop_Click(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                ControlSensorState("Temperature Sensor", SensorState.IDLE);
                NotifyAlarmServer(SensorState.IDLE, "Temperature Sensor");
            });
        }
        private void btTemperatureShutdown_Click(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                if (ConfirmAction(ASK_TO_CONTINUE_STRING, ASK_TO_CONTINUE_CAPTION))
                {
                    ControlSensorState("Temperature Sensor", SensorState.OFF);
                    NotifyAlarmServer(SensorState.OFF, "Temperature Sensor");
                }
            });
        }
        private void btPressureStart_Click(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                ControlSensorState("Pressure Sensor", SensorState.MEASURING);
                NotifyAlarmServer(SensorState.MEASURING, "Pressure Sensor");
            });
        }
        private void btPressureStop_Click(object sender, EventArgs e) 
        { 
            Invoke((MethodInvoker)delegate
            {
                ControlSensorState("Pressure Sensor", SensorState.IDLE);
                NotifyAlarmServer(SensorState.IDLE, "Pressure Sensor");
            });
        } 

        private void btPressureShutdown_Click(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                if (ConfirmAction(ASK_TO_CONTINUE_STRING, ASK_TO_CONTINUE_CAPTION))
                {
                    ControlSensorState("Pressure Sensor", SensorState.OFF);
                    NotifyAlarmServer(SensorState.OFF, "Pressure Sensor");
                }
            });
        }
        private void btHumidityStart_Click(object sender, EventArgs e) 
        { 
            Invoke((MethodInvoker)delegate
            {
                ControlSensorState("Humidity Sensor", SensorState.MEASURING);
                NotifyAlarmServer(SensorState.MEASURING, "Humidity Sensor");
            });
        }
        private void btHumidityStop_Click(object sender, EventArgs e) 
        {
            Invoke((MethodInvoker)delegate
            {
                ControlSensorState("Humidity Sensor", SensorState.IDLE);
                NotifyAlarmServer(SensorState.IDLE, "Humidity Sensor");
            });
        }
        private void btHumidityShutdown_Click(object sender, EventArgs e) 
        { 
            Invoke((MethodInvoker)delegate
            {
                if (ConfirmAction(ASK_TO_CONTINUE_STRING, ASK_TO_CONTINUE_CAPTION))
                {
                    ControlSensorState("Humidity Sensor", SensorState.OFF);
                    NotifyAlarmServer(SensorState.OFF, "Humidity Sensor");
                }
            });
        }
        private void btRadiationStart_Click(object sender, EventArgs e) 
        {
            Invoke((MethodInvoker)delegate
            {
                ControlSensorState("Radiation Sensor", SensorState.MEASURING);
                NotifyAlarmServer(SensorState.MEASURING, "Radiation Sensor");
            });
        }
        private void btRadiationStop_Click(object sender, EventArgs e) 
        {
            Invoke((MethodInvoker)delegate
            {
                ControlSensorState("Radiation Sensor", SensorState.IDLE);
                NotifyAlarmServer(SensorState.IDLE, "Radiation Sensor");
            });
        }
        private void btRadiationShutdown_Click(object sender, EventArgs e) 
        { 
            Invoke((MethodInvoker)delegate
            {
                if (ConfirmAction(ASK_TO_CONTINUE_STRING, ASK_TO_CONTINUE_CAPTION))
                {
                    ControlSensorState("Radiation Sensor", SensorState.OFF);
                    NotifyAlarmServer(SensorState.OFF, "Radiation Sensor");
                }
            });
        }
    }
}
