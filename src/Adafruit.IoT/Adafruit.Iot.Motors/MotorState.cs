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
        /// The motor is stopped and a brake effect is applied.
        /// </summary>
        /// <remarks>
        /// <para>For DC motors the brake effect is a short-brake that causes the brake motor wires to be shorted together, thus causing a braking effect.</para>
        /// <para>For stepper motors, the motor is stopped but the coils are left energized.</para>
        /// </remarks>
        Brake = 2
    }
}
