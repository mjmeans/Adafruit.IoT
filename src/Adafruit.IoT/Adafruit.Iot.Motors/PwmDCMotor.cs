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
        private Windows.Devices.Pwm.PwmController _controller;
        private Windows.Devices.Pwm.PwmPin _PWMpin;
        private Windows.Devices.Pwm.PwmPin _IN1pin;
        private Windows.Devices.Pwm.PwmPin _IN2pin;
        private byte _motorNum;
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

            // The PCA9685 PWM controller is used to control the inputs of two TB6612FNG dual motor drivers, "IC1" and "IC3".
            // Each TB6612FNG has two motor drivers. So we have motor driver circuits of IC1a, IC1b, IC3a and IC3b.
            // These correspond to motor hat screw terminals M1, M2, M3 and M4.
            // Each driver circuit ("IC1a", etc.) has one PWM pin and two IN pins.
            // The PWM pin expects a PWM input signal. The two IN pins expect a logic 0 or 1 input signal.
            // The variables pwm, in1 and in2 variables identify which PCA9685 PWM output pins will be used to drive this PwmDCMotor.
            // The pwm variable identifies which PCA9685 output pin is used to drive the xPWM input on the TB6612FNG.
            // And the in1 and in2 variables are used to specify which PCA9685 output pins are used to drive the xIN1 and xIN2 input pins of the TB6612FNG.
            byte pwm, in1, in2 = 0;

            if (driver == 1)
            {
                pwm = 8;
                in2 = 9;
                in1 = 10;
            }
            else if (driver == 2)
            {
                pwm = 13;
                in2 = 12;
                in1 = 11;
            }
            else if (driver == 3)
            {
                pwm = 2;
                in2 = 3;
                in1 = 4;
            }
            else if (driver == 4)
            {
                pwm = 7;
                in2 = 6;
                in1 = 5;
            }
            else
                throw new MotorHatException("MotorHat Motor must be between 1 and 4 inclusive");

            this._PWMpin = this._controller.OpenPin(pwm);
            this._PWMpin.Start();

            this._IN1pin = this._controller.OpenPin(in1);
            this._IN1pin.SetActiveDutyCyclePercentage(1);
            this._IN1pin.Polarity = Windows.Devices.Pwm.PwmPulsePolarity.ActiveLow;
            this._IN1pin.Start();

            this._IN2pin = this._controller.OpenPin(in2);
            this._IN2pin.SetActiveDutyCyclePercentage(1);
            this._IN2pin.Polarity = Windows.Devices.Pwm.PwmPulsePolarity.ActiveLow;
            this._IN2pin.Start();
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
                    this._IN2pin.Polarity = Windows.Devices.Pwm.PwmPulsePolarity.ActiveLow;
                    this._IN1pin.Polarity = Windows.Devices.Pwm.PwmPulsePolarity.ActiveHigh;
                    break;
                case Direction.Backward:
                    this._PWMpin.SetActiveDutyCyclePercentage(_speed);
                    this._IN1pin.Polarity = Windows.Devices.Pwm.PwmPulsePolarity.ActiveLow;
                    this._IN2pin.Polarity = Windows.Devices.Pwm.PwmPulsePolarity.ActiveHigh;
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

        /// <inheritdoc/>
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
