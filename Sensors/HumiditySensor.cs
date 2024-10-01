
using Microsoft.Extensions.Logging;

namespace ParticleAcceleratorMonitoring
{
    public class HumiditySensor : Sensor
    {
        public HumiditySensor() : base() {}
        public HumiditySensor(string ip, int port, int id, ILogger<Sensor> logger) : base(ip, port, id, logger){}

        public override double MAX_SAFE_VALUE => 0.1;

        public override double MIN_SAFE_VALUE => 0.0;   // perfect vacuum

        public override string UNITS => "RH";

        public override double MAX_TOLERANCE => 0.1;

        public override double MIN_TOLERANCE => 0.0;

        public override string SENSOR_TYPE => "Humidity Sensor";

        public override double ReadValue()
        {
            return RandomReadValue();
        }
    }
}
