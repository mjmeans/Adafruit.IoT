using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adafruit.IoT.Motors
{
    /// <summary>
    /// The valid states of a stepper motor.
    /// </summary>
    public enum MotorState : int
    {
        /// <summary>
        /// The motor is not running or energized.
        /// </summary>
        Stop = 0,
        /// <summary>
        /// The motor is running.
        /// </summary>
        Run = 1,
        /// <summary>
        /// The motor is stopped but energized, holding position like a brake
        /// </summary>
        /// <remarks>
        /// This state is only valid for stepper motors.
        /// </remarks>
        Brake = 2
    }
}
