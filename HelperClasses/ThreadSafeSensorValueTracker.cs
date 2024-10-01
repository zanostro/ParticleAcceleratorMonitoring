
using System.Collections.Generic;

namespace ParticleAcceleratorMonitoring
{

    // thread safe mechanism that tracks which sensors fall outside their allowed intervals
    internal class ThreadSafeSensorValueTracker
    {
        // prevents multiple threads to access variables at the same time
        private readonly object _lockObject = new object();

        // tracks which sensors fall outside their allowed intervals (true - outside, false inside the interval)
        private bool[] _StateTracker;  
        private bool[] _ExcludedSensors;
        private int _SensorsOutsideIntervalCounter;       

        public ThreadSafeSensorValueTracker(int arraySize)
        {
            _StateTracker = new bool[arraySize];
            _ExcludedSensors = new bool[arraySize];
            _SensorsOutsideIntervalCounter = 0;                     
        }



        // exclude sensors from monitoring: removes them from state tracker and return new alarm state
        public bool ExcludeSensors(List<int> indexes)
        {
            lock (_lockObject)
            {
                foreach (int index in indexes) {
                    _ExcludedSensors[index] = true;
                    if (_StateTracker[index]) 
                    {
                        _SensorsOutsideIntervalCounter--;
                        _StateTracker[index] = false;
                    }
                }
            }
            return _SensorsOutsideIntervalCounter > 1;
        }

        // include sensors in monitoring: adds them to state tracker and return new alarm state
        public bool IncludeSensors(List<int> indexes)
        {
            lock (_lockObject)
            {
                foreach (int index in indexes)
                {
                    _ExcludedSensors[index] = false;
                }
            }
            return _SensorsOutsideIntervalCounter > 1;
        }



        // [0] - signals which changes have been made to the sensor :
        //         -1 sensor no longer exceeds safe readings
        //          0 no change
        //          1 sensor starts exceeding safe readins
        //
        // [1] - signal to flip alarm state (0 no change, 1 change)
        // [2] - alarm state (0 no alarm, 1 alarm)
        // the mechanism is designed so that the lock is accessed only once for each received message.
        public int[] ExceedsMaxReadings(int index, double value, double minValue, double maxValue)
        {  
            // default is no change
            int change = 0;
            int alarmChange = 0;
            int alarmState = 0;

            // if sensor is excluded from monitoring, return no change
            if (_ExcludedSensors[index]) return new int[] { 0, 0, 0}; 

            if (_SensorsOutsideIntervalCounter > 1) alarmState = 1;
            int prevAlarmState = alarmState;


            lock (_lockObject)
            {
                bool exceeds = value > maxValue;
                bool falls_short = value < minValue;

                // outside alowed intervals
                if ((exceeds | falls_short) && _StateTracker[index] == false)
                {
                    _StateTracker[index] = true;
                    _SensorsOutsideIntervalCounter++;

                    //faulty sensor or alarm
                    change = 1;
                }

                // inside allowed intervals
                else if(!exceeds && !falls_short && _StateTracker[index])
                {
                    _StateTracker[index] = false;
                    _SensorsOutsideIntervalCounter--;
                    change = -1;
                }
            }

            if (_SensorsOutsideIntervalCounter > 1) alarmState = 1;

            // alarm state has been flipped
            if(alarmState != prevAlarmState) alarmChange = 1;

            return new int[] {change, alarmChange, alarmState};
        }
    }
}
