using Adafruit.IoT.Devices;
using System;

namespace Adafruit.IoT.Motors
{
    /// <summary>
    /// Represents a DC Motor connected to a PWM controller.
    /// </summary>
    /// <remarks>
    /// This class implements the <see cref="SetSpeed(double)"/> method to set the PWM duty cycle range of 0.0 to 1.0 which adjusts the power and therefore the motor speed rather than an RPM speed.
    /// </remarks>
    public sealed class PwmDCMotor : IMotor, IDisposable
    {
        private Windows.Devices.Pwm.PwmPin _IN1pin;
        private Windows.Devices.Pwm.PwmPin _IN2pin;
        private Windows.Devices.Pwm.PwmController _controller;
        private byte _motorNum;
        private Windows.Devices.Pwm.PwmPin _PWMpin;
        private double _speed;

        /// <summary>
        /// Initializes a new <see cref="PwmDCMotor"/> instance.
        /// </summary>
        /// <param name="controller">The <see cref="Windows.Devices.Pwm.PwmController"/> to use.</param>
        /// <param name="driver">The motor driver channel (1 through 4) for coil A.</param>
        internal PwmDCMotor(Windows.Devices.Pwm.PwmController controller, byte driver)
        {
            this._controller = controller;
            this._motorNum = driver;
            byte pwm, in1, in2 = 0;

            if (driver == 0)
            {
                pwm = 8;
                in2 = 9;
                in1 = 10;
            }
            else if (driver == 1)
            {
                pwm = 13;
                in2 = 12;
                in1 = 11;
            }
            else if (driver == 2)
            {
                pwm = 2;
                in2 = 3;
                in1 = 4;
            }
            else if (driver == 3)
            {
                pwm = 7;
                in2 = 6;
                in1 = 5;
            }
            else
                throw new MotorHatException("MotorHat Motor must be between 1 and 4 inclusive");

            this._PWMpin = this._controller.OpenPin(pwm);
            this._IN1pin = this._controller.OpenPin(in1);
            this._IN2pin = this._controller.OpenPin(in2);
        }

        /// <summary>
        /// Runs the motor in the specified direction.
        /// </summary>
        /// <param name="direction">A <see cref="Direction"/>.</param>
        /// <remarks>
        /// This method uses the previously value set using <see cref="SetSpeed(double)"/> to modulate the PWM power going to the motor.
        /// In order to change the speed of a running motor you must call this method again after calling <see cref="SetSpeed(double)"/>.
        /// </remarks>
        public void Run(Direction direction)
        {
            if (this._controller == null)
                return;

            switch (direction)
            {
                case Direction.Forward:
                    this._PWMpin.SetActiveDutyCyclePercentage(_speed);
                    this._IN2pin.Stop();
                    this._IN1pin.Start();
                    break;
                case Direction.Backward:
                    this._PWMpin.SetActiveDutyCyclePercentage(_speed);
                    this._IN1pin.Stop();
                    this._IN2pin.Start();
                    break;
                default:
                    throw new ArgumentException("direction");
            }
        }

        /// <summary>
        /// Set the desired motor's speed in revolutions per minute.
        /// </summary>
        /// <param name="rpm">The duty cycle of the PWM controller in the range of 0.0 to 1.0.</param>
        /// <remarks>
        /// <note type="note">
        /// Although this method has a parameter called rpm, this parameter actually sets the duty cycle of the PWM controller that powers the motor.
        /// </note>
        /// <remarks>
        /// This method sets the value used to modulate the PWM power going to the motor.
        /// In order to change the speed of a running motor you must call this method and then call <see cref="Run(Direction)"/> again.
        /// </remarks>
        /// </remarks>
        public void SetSpeed(double rpm)
        {
            if (rpm < 0)
                rpm = 0;
            if (rpm > 1)
                rpm = 1;
            _speed = rpm;
        }

        public void Stop()
        {
            this._PWMpin.SetActiveDutyCyclePercentage(0);
            this._IN1pin.Stop();
            this._IN2pin.Stop();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <inheritdoc/>
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose the opened pins so that they are released
                    if (this._PWMpin != null) { this._PWMpin.Dispose(); this._PWMpin = null; }
                    if (this._IN1pin != null) { this._IN1pin.Dispose(); this._IN1pin = null; }
                    if (this._IN2pin != null) { this._IN2pin.Dispose(); this._IN2pin = null; }
                }
                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
