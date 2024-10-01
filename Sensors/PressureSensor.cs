
using Microsoft.Extensions.Logging;

namespace ParticleAcceleratorMonitoring
{
    public class PressureSensor : Sensor
    {
        public PressureSensor() : base(){}
        public PressureSensor(string ip, int port, int id, ILogger<Sensor> logger) : base(ip, port, id, logger){}

        public override double MAX_SAFE_VALUE => 0.0000001; // causes beam degredation

        public override double MIN_SAFE_VALUE => 0.0; // perfect vaccum

        public override string UNITS => "Pa";

        public override double MAX_TOLERANCE => 0.1;

        public override double MIN_TOLERANCE => 0.0;

        public override string SENSOR_TYPE => "Pressure Sensor";

        public override double ReadValue()
        {
            return RandomReadValue();
        }
    }
}
