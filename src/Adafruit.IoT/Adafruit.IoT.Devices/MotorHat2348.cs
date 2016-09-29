using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Adafruit.IoT.Devices.Pwm;
using Adafruit.IoT.Motors;

namespace Adafruit.IoT.Devices
{
    /// <summary>
    /// Driver class for the Adafruit DC &amp; Stepper Motor HAT for Raspberry Pi.
    /// </summary>
    /// <remarks>
    /// <para>See the Adafruit DC &amp; Stepper Motor HAT for Raspberry Pi – Mini Kit
    /// <see href="https://www.adafruit.com/products/2348">here</see>
    /// </para><para>To use this class:
    /// <code>
    ///using Adafruit.IoT.Devices;
    ///using Adafruit.IoT.Motors;
    ///using System;
    ///using System.Threading.Tasks;
    ///using Windows.Devices.Pwm;
    ///
    ///namespace Adafruit.IoT
    ///{
    ///    internal class Examples
    ///    {
    ///        public async void Example1()
    ///        {
    ///            MotorHat2348 mh = null;
    ///            PwmStepperMotor stepper = null;
    ///            PwmPin pwm = null;
    ///
    ///            if (mh == null)
    ///            {
    ///                // Create a driver object for the HAT at address 0x60
    ///                mh = new MotorHat2348(0x60);
    ///                // Create a stepper motor object at the specified ports and steps per rev
    ///                stepper = mh.CreateStepperMotor(1, 2, 200);
    ///                // Create a PwmPin object at one of the auxiliary PWMs on the HAT
    ///                pwm = mh.CreatePwm(1);
    ///            }
    ///
    ///            // step 200 full steps in the forward direction using half stepping (so 400 steps total) at 30 rpm
    ///            stepper.SetSpeed(30);
    ///            await stepper.StepAsync(200, Direction.Forward, SteppingStyle.Half);
    ///
    ///            // Activate the pin and set it to 50% duty cycle
    ///            pwm.SetActiveDutyCyclePercentage(0.5);
    ///            pwm.Start();
    ///
    ///            // for demonstration purposes we will wait 10 seconds to observe the PWM and motor operation.
    ///            await Task.Delay(10000);
    ///
    ///            // Stop the auxiliary PWM pin
    ///            pwm.Stop();
    ///
    ///            // Dispose of the MotorHat and free all its resources
    ///            mh.Dispose();
    ///        }
    ///    }
    /// }
    /// </code></para>
    /// </remarks>
    public sealed class MotorHat2348 : IDisposable
    {
        private byte _i2caddr;
        private double _frequency;
        internal Windows.Devices.Pwm.PwmController _pwm;
        private List<IMotor> motors = new List<IMotor>();
        private List<Windows.Devices.Pwm.PwmPin> pins = new List<Windows.Devices.Pwm.PwmPin>();
        private bool[] motorChannelsUsed = new bool[4]; // There are a total of 4 motor channels
        private bool[] pinChannelsUsed = new bool[4]; // There are a total of 4 additional PWM pins
        private bool isInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="MotorHat2348"/> class with the specified I2C address and PWM frequency.
        /// </summary>
        /// <param name="i2cAddrress">The I2C address of the MotorHat's PWM controller.</param>
        /// <param name="frequency">The frequency in Hz to set the PWM controller.</param>
        /// <remarks>
        /// The default i2c address is 0x60, but the HAT can be configured in hardware to any address from 0x60 to 0x7f.
        /// The PWM hardware used by this HAT is a PCA9685. It has a total possible frequency range of 24 to 1526 Hz.
        /// Setting the frequency above or below this range will cause PWM hardware to be set at its maximum or minimum setting.
        /// </remarks>
        public MotorHat2348(byte i2cAddrress, double frequency)
        {
            this.motors = new List<IMotor>();
            this._i2caddr = i2cAddrress;        // default I2C address of the HAT
            this._frequency = frequency;        // default @1600Hz PWM freq
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MotorHat2348"/> class with the specified I2C address and default PWM frequency.
        /// </summary>
        /// <param name="i2cAddrress">The I2C address of the MotorHat's PWM controller.</param>
        /// <remarks>
        /// The <see cref="MotorHat2348"/> will be created with the default frequency of 1600 Hz.
        /// </remarks>
        public MotorHat2348(byte i2cAddrress) : this(i2cAddrress, 1600)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MotorHat2348"/> class with the default I2C address and PWM frequency.
        /// </summary>
        /// <remarks>
        /// The <see cref="MotorHat2348"/> will be created with the default I2C address of 0x60 and PWM frequency of 1600 Hz.
        /// </remarks>
        public MotorHat2348() : this(0x60, 1600)
        {
        }

        private void EnsureInitialized()
        {
            Task.Run(async () =>
            {
                if (isInitialized) return;
                var provider = new PCA9685Provider(this._i2caddr);
                var controllers = await Windows.Devices.Pwm.PwmController.GetControllersAsync(provider);
                this._pwm = controllers[0];
                if (this._frequency > this._pwm.MaxFrequency) this._frequency = this._pwm.MaxFrequency;
                this._pwm.SetDesiredFrequency(this._frequency);
                isInitialized = true;
            }).Wait();
        }

        /// <summary>
        /// Creates a <see cref="PwmDCMotor"/>  object for the specified channel and adds it to the list of Motors.
        /// </summary>
        /// <param name="driverChannel">A motor driver channel from 1 to 4.</param>
        /// <returns>The created DCMotor object.</returns>
        /// <remarks>
        /// The driverChannel parameter refers to the motor driver channels M1, M2, M3 or M4.
        /// </remarks>
        public PwmDCMotor CreateDCMotor(byte driverChannel)
        {
            PwmDCMotor value;
            var actualDriverChannel = driverChannel - 1;
            if ((driverChannel < 1) || (driverChannel > 4))
                throw new InvalidOperationException("CreateDCMotor parameter 'driverChannel' must be between 1 and 4.");
            if (motorChannelsUsed[actualDriverChannel] == true)
                throw new MotorHatException(string.Format("Channel {0} aleady in assigned.", driverChannel));
            EnsureInitialized();

            value = new PwmDCMotor(this._pwm, (byte)actualDriverChannel);
            motorChannelsUsed[actualDriverChannel] = true;
            motors.Add(value);

            return value;
        }

        /// <summary>
        /// Creates a <see cref="PwmStepperMotor"/> object for the specified channels and adds it to the list of Motors.
        /// </summary>
        /// <param name="driverChannelA">A motor driver channel from 1 to 4.</param>
        /// <param name="driverChannelB">A motor driver channel from 1 to 4.</param>
        /// <param name="steps">The number of full steps per revolution that this motor has.</param>
        /// <returns>The created <see cref="PwmStepperMotor"/> object.</returns>
        /// <remarks>
        /// The driverChannelA and driverChannelB parameters must be different and must be channels not already assigned to other 
        /// <see cref="PwmDCMotor"/> or <see cref="PwmStepperMotor"/> objects for this <see cref="MotorHat2348"/>.
        /// </remarks>
        public PwmStepperMotor CreateStepperMotor(byte driverChannelA, byte driverChannelB, byte steps)
        {
            PwmStepperMotor value;

            if ((driverChannelA < 1) || (driverChannelA > 4))
                throw new ArgumentOutOfRangeException("driverChannelA");
            if ((driverChannelB < 1) || (driverChannelB > 4))
                throw new ArgumentOutOfRangeException("driverChannelB");
            if (driverChannelA == driverChannelB)
                throw new ArgumentOutOfRangeException("driverChannelB", "Parameters driverChannelA and driverChannelB must be different.");
            if (motorChannelsUsed[driverChannelA - 1] == true)
                throw new MotorHatException(string.Format("Channel {0} is already assigned.", driverChannelA));
            if (motorChannelsUsed[driverChannelB - 1] == true)
                throw new MotorHatException(string.Format("Channel {0} is already assigned.", driverChannelB));
            EnsureInitialized();

            value = new PwmStepperMotor(this._pwm, driverChannelA, driverChannelB, steps);
            motorChannelsUsed[driverChannelA - 1] = true;
            motorChannelsUsed[driverChannelB - 1] = true;
            motors.Add(value);

            return value;
        }

        /// <summary>
        /// Creates a <see cref="Windows.Devices.Pwm.PwmPin"/> for the specified channel.
        /// </summary>
        /// <param name="channel">The PWM channel number.</param>
        /// <returns>The created <see cref="Windows.Devices.Pwm.PwmPin"/> for the specified channel.</returns>
        /// <remarks>Channel numbers 1 through 4 correspond to the auxiliary PCA9685 channels 0, 1, 14 and 15.</remarks>
        public Windows.Devices.Pwm.PwmPin CreatePwm(byte channel)
        {
            Windows.Devices.Pwm.PwmPin retval;
            int pwapin;

            if ((channel < 1) || (channel > 4))
                throw new ArgumentOutOfRangeException("channel");
            EnsureInitialized();

            switch (channel)
            {
                case 1:
                    pwapin = 0;
                    break;
                case 2:
                    pwapin = 1;
                    break;
                case 3:
                    pwapin = 14;
                    break;
                case 4:
                    pwapin = 15;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("channel");
            }
            pinChannelsUsed[channel - 1] = true;
            retval = this._pwm.OpenPin(pwapin);
            pins.Add(retval);
            return retval;
        }

        /// <summary>
        /// Gets all motors created to this <see cref="MotorHat2348"/>.
        /// </summary>
        /// <value>
        /// A list of <see cref="IMotor"/> objects.
        /// </value>
        /// <remarks>
        /// The method returns a list of values that represent the <see cref="IMotor"/> objects created on this <see cref="MotorHat2348"/>.
        /// </remarks>
        public IReadOnlyList<IMotor> Motors
        {
            get
            {
                EnsureInitialized();
                return this.motors;
            }
        }

        /// <summary>
        /// Gets all auxiliary PWM pins created to this <see cref="MotorHat2348"/>.
        /// </summary>
        /// <value>
        /// A list of <see cref="PwmPins"/> objects.
        /// </value>
        /// <remarks>
        /// The method returns a list of values that represent the <see cref="PwmPins"/> objects created on this <see cref="MotorHat2348"/>.
        /// </remarks>
        public IReadOnlyList<Windows.Devices.Pwm.PwmPin> PwmPins
        {
            get
            {
                EnsureInitialized();
                return this.pins;
            }
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
                    for (int i = motors.Count - 1; i >= 0; i--)
                    {
                        var motor = motors[i] as IDisposable;
                        if (motor != null) { motor.Dispose(); }
                        motors.RemoveAt(i);
                    }
                    for (int i = pins.Count - 1; i >= 0; i--)
                    {
                        var pin = pins[i] as IDisposable;
                        if (pin != null) { pin.Dispose(); }
                        pins.RemoveAt(i);
                    }
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
