namespace Adafruit.IoT.Motors
{
    /// <summary>
    /// Defines common methods used by all motors. 
    /// </summary>
    /// <remarks>
    /// The purpose of this interface is to share generic motor control methods between type and strongly-type motor control types.
    /// </remarks>
    public interface IMotor
    {
        /// <summary>
        /// Sets the speed of the motor.
        /// </summary>
        /// <param name="rpm">The desired revolutions per minute speed of the motor.</param>
        /// <remarks>
        /// The desired speed of the motor is used to calculate the delays between steps for a stepper motor or the duty cycle of the power supplied to a DC motor.
        /// </remarks>
        void SetSpeed(double rpm);

        /// <summary>
        /// Run the motor continuously in the specified direction
        /// </summary>
        /// <param name="direction">A <see cref="Direction"/>.</param>
        void Run(Direction direction);

        /// <summary>
        /// Stop the motor and cut its power. Inertia may permit the motor to continue rotating.
        /// </summary>
        void Stop();

        /// <summary>
        /// Stop the motor and apply a brake.
        /// </summary>
        void Brake();
    }
}
