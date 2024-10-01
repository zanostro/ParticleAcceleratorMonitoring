

using Microsoft.Extensions.Logging;

namespace ParticleAcceleratorMonitoring
{
    public class RadiationSensor : Sensor
    {
        public RadiationSensor() : base() { }
        public RadiationSensor(string ip, int port, int id, ILogger<Sensor> logger) : base(ip, port, id, logger){}

        public override double MAX_SAFE_VALUE => 1000000.0;

        public override double MIN_SAFE_VALUE => 0.0; 

        public override string UNITS => "µSv/h";

        public override double MAX_TOLERANCE => 0.1;

        public override double MIN_TOLERANCE => 0.0; // background radiation levels away from high-energy interactions can be very low, similar to ambient environmental radiation

        public override string SENSOR_TYPE => "Radiation Sensor";

        public override double ReadValue()
        {
            return RandomReadValue();
        }
    }
}
