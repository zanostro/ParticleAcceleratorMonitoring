

using Microsoft.Extensions.Logging;

namespace ParticleAcceleratorMonitoring
{
    public class TemperatureSensor : Sensor
    {
        public TemperatureSensor() : base() { }
        public TemperatureSensor(string ip, int port, int id, ILogger<Sensor> logger) : base(ip, port, id, logger) { }

        public override double MAX_SAFE_VALUE => 4.2;   // helium boiling point
        public override double MIN_SAFE_VALUE => 1.9;   // cryogenic target temperature
        public override string UNITS => "K";

        public override double MAX_TOLERANCE => 0.1;
        public override double MIN_TOLERANCE => 0.1;

        public override string SENSOR_TYPE => "Temperature Sensor";

        public override double ReadValue()
        {
           return RandomReadValue();
        }
    }
}
