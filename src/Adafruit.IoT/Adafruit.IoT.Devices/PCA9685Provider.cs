using Microsoft.IoT.DeviceHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Devices.Pwm;
using Windows.Devices.Pwm.Provider;

namespace Adafruit.IoT.Devices.Pwm
{
    /// <summary>
    /// Driver for the PCA9685 used on the <see href="http://www.adafruit.com/products/815">
    /// Adafruit 16-Channel 12-bit PWM/Servo Driver</see> and others.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Create an instance of this class and pass it to the
    /// <see cref="PwmController.GetControllersAsync">GetControllersAsync</see> 
    /// method of the <see cref="PwmController"/> class. Then use the returned 
    /// controller collection to set the PWM parameters.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// // Create a provider at the default I2C1 address 0x40.
    /// var pca9685 = new PCA9685();
    /// 
    /// // Get any controllers that can use this provider.
    /// var controllers = PwmController.GetControllersAsync(pca9685);
    /// 
    /// // Get the first controller.
    /// var controller = controllers[0];
    /// 
    /// // Open the PWM pin number 0. Pins cannot be opened for shared access.
    /// using (var pin = controller.OpenPin(0))
    /// {
    ///     // Start the PWM
    ///     pin.Start();
    ///     
    ///     // Set it's duty cycle
    ///     pin.SetActiveDutyCyclePercentage(0.5);
    ///     
    ///     // ... do some timing to allow it to stabilize or whatever
    /// 
    ///     // Stop the pin
    ///     pin.Stop();
    ///     
    /// }   // end using will release the pin, turn it off, and make it available by another operation
    /// </code>
    /// </para>
    /// <para>
    /// This class is adapted from the original C++ ms-iot sample 
    /// <see href="https://github.com/ms-iot/BusProviders/tree/develop/PWM/PwmPCA9685">here</see>.
    /// And SimulatedProvider code <see href="https://github.com/ms-iot/BusProviders/blob/develop/SimulatedProvider/SimulatedProvider/PwmControllerProvider.cs">here</see>/>
    /// </para>
    /// </remarks>
    internal sealed class PCA9685Provider : IPwmProvider, IDisposable
    {
        #region Constants
        private const string I2C_DEFAULT_CONTROLLER_NAME = "I2C1";
        private const int I2C_PRIMARY_ADDRESS = 0x40;
        #endregion // Constants

        #region Public Properties
        /// <summary>
        /// Gets or sets the I2C address of the controller.
        /// </summary>
        /// <value>
        /// The I2C address of the controller. The default is 0x40.
        /// </value>
        [DefaultValue(I2C_PRIMARY_ADDRESS)]
        public int Address
        {
            get
            {
                return address;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                address = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the I2C controller to use.
        /// </summary>
        /// <value>
        /// The name of the I2C controller to use. The default is "I2C1".
        /// </value>
        [DefaultValue(I2C_DEFAULT_CONTROLLER_NAME)]
        public string ControllerName
        {
            get
            {
                return controllerName;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                controllerName = value;
            }
        }
        #endregion // Public Properties

        #region Member Variables
        private int address = I2C_PRIMARY_ADDRESS;
        private string controllerName = I2C_DEFAULT_CONTROLLER_NAME;
        private List<PCA9685Controller> controllers;
        private bool isInitialized;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Create a new <see cref="PCA9685Provider"/> instance for the default I2C controller (I2C1) at the default I2C address (0x40).
        /// </summary>
        public PCA9685Provider()
        { }

        /// <summary>
        /// Create a new <see cref="PCA9685Provider"/> instance for the default I2C controller (I2C1) at the specified I2C address.
        /// </summary>
        /// <param name="address">The I2C address.</param>
        public PCA9685Provider(int address)
        {
            this.address = address;
        }

        /// <summary>
        /// Create a new <see cref="PCA9685Provider"/> instance for the specified I2C controller and I2C address.
        /// </summary>
        /// <param name="controllerName">The I2C controller name.</param>
        /// <param name="address">The I2C address.</param>
        public PCA9685Provider(string controllerName, int address)
        {
            this.address = address;
            this.controllerName = controllerName;
        }
        #endregion // Constructors

        #region IPwmProvider Interface
        /// <inheritdoc/>
        public IReadOnlyList<IPwmControllerProvider> GetControllers()
        {
            if (controllers == null)
            {
                // Validate
                if (string.IsNullOrWhiteSpace(controllerName)) { throw new MissingIoException(nameof(ControllerName)); }

                controllers = new List<PCA9685Controller>();
                controllers.Add(new PCA9685Controller(this));
            }
            isInitialized = true;
            return controllers;
        }
        #endregion // IPwmProvider Interface

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (controllers != null)
                    {
                        for (int i = controllers.Count - 1; i>0; i--)
                        {
                            var controller = controllers[i];
                            controller.Dispose();
                            controllers.RemoveAt(i);
                        }
                        controllers = null;
                    }
                }
                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion // IDisposable Support

    }

    /// <summary>
    /// Internal driver for the PCA9685 used on the <see href="http://www.adafruit.com/products/815">
    /// Adafruit 16-Channel 12-bit PWM/Servo Driver</see> and others.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is not designed to be access by user code. It is accessed
    /// by the <see cref="PwmController"/> and <see cref="PwmPin"/> classes.
    /// </para>
    /// <para>
    /// This class is adapted from the original C++ ms-iot sample 
    /// <see href="https://github.com/ms-iot/BusProviders/tree/develop/PWM/PwmPCA9685">here</see>.
    /// And SimulatedProvider code <see href="https://github.com/ms-iot/BusProviders/blob/develop/SimulatedProvider/SimulatedProvider/PwmControllerProvider.cs">here</see>/>
    /// </para>
    /// </remarks>
    internal sealed class PCA9685Controller : IPwmControllerProvider, IDisposable
    {
        #region Constants
        private const double CLOCK_FREQUENCY = 25000000;
        private const byte DEFAULT_PRESCALE = 0x1E;
        private const int I2C_RESET_ADDRESS = 0x0;
        private const int I2C_ALL_CALL_ADDRESS = 0xE0;      // Default ALLCALL address for all PCA9685 on I2C bus
        private const int MAX_FREQUENCY = 1526;
        private const int MIN_FREQUENCY = 24;
        private const int PIN_COUNT = 16;                   // 16 LED outputs
        private const ushort PULSE_RESOLUTION = 4096;       // 12-bit resolution
        static private readonly byte[] RESET_COMMAND = new byte[] { 0x06 };

        static private readonly PinControlRegister[] PwmPinRegs = new PinControlRegister[16]
        {
            new PinControlRegister(Registers.PIN0_ON_L, Registers.PIN0_ON_H, Registers.PIN0_OFF_L, Registers.PIN0_OFF_H ),
            new PinControlRegister(Registers.PIN1_ON_L, Registers.PIN1_ON_H , Registers.PIN1_OFF_L , Registers.PIN1_OFF_H ),
            new PinControlRegister(Registers.PIN2_ON_L, Registers.PIN2_ON_H , Registers.PIN2_OFF_L , Registers.PIN2_OFF_H ),
            new PinControlRegister(Registers.PIN3_ON_L, Registers.PIN3_ON_H , Registers.PIN3_OFF_L , Registers.PIN3_OFF_H ),
            new PinControlRegister(Registers.PIN4_ON_L, Registers.PIN4_ON_H , Registers.PIN4_OFF_L , Registers.PIN4_OFF_H ),
            new PinControlRegister(Registers.PIN5_ON_L, Registers.PIN5_ON_H , Registers.PIN5_OFF_L , Registers.PIN5_OFF_H ),
            new PinControlRegister(Registers.PIN6_ON_L, Registers.PIN6_ON_H, Registers.PIN6_OFF_L, Registers.PIN6_OFF_H ),
            new PinControlRegister(Registers.PIN7_ON_L, Registers.PIN7_ON_H , Registers.PIN7_OFF_L , Registers.PIN7_OFF_H ),
            new PinControlRegister(Registers.PIN8_ON_L, Registers.PIN8_ON_H , Registers.PIN8_OFF_L , Registers.PIN8_OFF_H ),
            new PinControlRegister(Registers.PIN9_ON_L, Registers.PIN9_ON_H , Registers.PIN9_OFF_L , Registers.PIN9_OFF_H ),
            new PinControlRegister(Registers.PIN10_ON_L, Registers.PIN10_ON_H , Registers.PIN10_OFF_L , Registers.PIN10_OFF_H ),
            new PinControlRegister(Registers.PIN11_ON_L, Registers.PIN11_ON_H , Registers.PIN11_OFF_L , Registers.PIN11_OFF_H ),
            new PinControlRegister(Registers.PIN12_ON_L, Registers.PIN12_ON_H , Registers.PIN12_OFF_L , Registers.PIN12_OFF_H ),
            new PinControlRegister(Registers.PIN13_ON_L, Registers.PIN13_ON_H , Registers.PIN13_OFF_L , Registers.PIN13_OFF_H ),
            new PinControlRegister(Registers.PIN14_ON_L, Registers.PIN14_ON_H , Registers.PIN14_OFF_L , Registers.PIN14_OFF_H ),
            new PinControlRegister(Registers.PIN15_ON_L, Registers.PIN15_ON_H , Registers.PIN15_OFF_L , Registers.PIN15_OFF_H ),

        };
        #endregion // Constants

        #region Nested Types
        /// <summary>
        /// PCA9685 register addresses
        /// </summary>
        private enum Registers : byte
        {
            MODE1 = 0x00,
            MODE2 = 0x01,
            SUBADR1 = 0x02,
            SUBADR2 = 0x03,
            SUBADR3 = 0x04,
            ALLCALLADR = 0x05,
            PIN0_ON_L = 0x06,
            PIN0_ON_H = 0x07,
            PIN0_OFF_L = 0x08,
            PIN0_OFF_H = 0x09,
            PIN1_ON_L = 0x0A,
            PIN1_ON_H = 0x0B,
            PIN1_OFF_L = 0x0C,
            PIN1_OFF_H = 0x0D,
            PIN2_ON_L = 0x0E,
            PIN2_ON_H = 0x0F,
            PIN2_OFF_L = 0x10,
            PIN2_OFF_H = 0x11,
            PIN3_ON_L = 0x12,
            PIN3_ON_H = 0x13,
            PIN3_OFF_L = 0x14,
            PIN3_OFF_H = 0x15,
            PIN4_ON_L = 0x16,
            PIN4_ON_H = 0x17,
            PIN4_OFF_L = 0x18,
            PIN4_OFF_H = 0x19,
            PIN5_ON_L = 0x1A,
            PIN5_ON_H = 0x1B,
            PIN5_OFF_L = 0x1C,
            PIN5_OFF_H = 0x1D,
            PIN6_ON_L = 0x1E,
            PIN6_ON_H = 0x1F,
            PIN6_OFF_L = 0x20,
            PIN6_OFF_H = 0x21,
            PIN7_ON_L = 0x22,
            PIN7_ON_H = 0x23,
            PIN7_OFF_L = 0x24,
            PIN7_OFF_H = 0x25,
            PIN8_ON_L = 0x26,
            PIN8_ON_H = 0x27,
            PIN8_OFF_L = 0x28,
            PIN8_OFF_H = 0x29,
            PIN9_ON_L = 0x2A,
            PIN9_ON_H = 0x2B,
            PIN9_OFF_L = 0x2C,
            PIN9_OFF_H = 0x2D,
            PIN10_ON_L = 0x2E,
            PIN10_ON_H = 0x2F,
            PIN10_OFF_L = 0x30,
            PIN10_OFF_H = 0x31,
            PIN11_ON_L = 0x32,
            PIN11_ON_H = 0x33,
            PIN11_OFF_L = 0x34,
            PIN11_OFF_H = 0x35,
            PIN12_ON_L = 0x36,
            PIN12_ON_H = 0x37,
            PIN12_OFF_L = 0x38,
            PIN12_OFF_H = 0x39,
            PIN13_ON_L = 0x3A,
            PIN13_ON_H = 0x3B,
            PIN13_OFF_L = 0x3C,
            PIN13_OFF_H = 0x3D,
            PIN14_ON_L = 0x3E,
            PIN14_ON_H = 0x3F,
            PIN14_OFF_L = 0x40,
            PIN14_OFF_H = 0x41,
            PIN15_ON_L = 0x42,
            PIN15_ON_H = 0x43,
            PIN15_OFF_L = 0x44,
            PIN15_OFF_H = 0x45,
            ALL_LED_ON_L = 0xFA,
            ALL_LED_ON_H = 0xFB,
            ALL_LED_OFF_L = 0xFC,
            ALL_LED_OFF_H = 0xFD,
            PRESCALE = 0xFE,
        };

        /// <summary>
        /// PCA9685 mode 1 register flags
        /// </summary>
        [Flags]
        private enum Mode1Flags : byte
        {                           // * denoted power-on state
            RESTART = 0x80,         // Restart: 0*: disabled, 1:enabled
            EXTCLK = 0x40,          // clock source: 0*: internal, 1: external
            AI = 0x20,              // register auto-increment: *0: disabled, 1: enabled
            SLEEP = 0x10,           // mode: 0: normal, *1: low-power
            SUB1 = 0x08,            // sub-address 1: *0: disabled, 1: enabled
            SUB2 = 0x04,            // sub-address 2: *0: disabled, 1: enabled
            SUB3 = 0x02,            // sub-address 3: *0: disabled, 1: enabled
            ALLCALL = 0x01,         // all call address: 0: disabled, *1: enabled
        }                           // power-on state of this register is 0x11 (00010001)

        /// <summary>
        /// PCA9685 mode 2 register flags
        /// </summary>
        [Flags]
        private enum Mode2Flags : byte
        {                           // * denoted power-on state
            RESERVED1 = 0x80,       // Reserved *0
            RESERVED2 = 0x40,       // Reserved *0
            RESERVED3 = 0x20,       // Reserved *0
            INVRT = 0x10,           // When output drivers enabled, *0: not inverted, 1: inverted
            OCH = 0x08,             // Outputs change on: *0: STOP command, 1: ACK command
            OUTDRV = 0x04,          // Output type: 0: open drain, *1: totem pole
            // *00: When output drivers disabled, LEDn=0
            // 01: When output drivers disabled, LEDn=1 when OUTDRV=1, otherwise high-impedance
            // 1x: When output drivers disabled, LEDn=high-impedance
            OUTNE1 = 0x02,
            OUTNE0 = 0x01,
        }                           // power-on state of this register is 0x04 (00000100)

        private struct PinControlRegister
        {
            public PinControlRegister(Registers onLow, Registers onHigh, Registers offLow, Registers offHigh)
            {
                OnLow = (byte)onLow;
                OnHigh = (byte)onHigh;
                OffLow = (byte)offLow;
                OffHigh = (byte)offHigh;
            }
            public byte OnLow;
            public byte OnHigh;
            public byte OffLow;
            public byte OffHigh;
        };
        #endregion // Nested Types

        #region Constructors
        public PCA9685Controller(PCA9685Provider pCA9685)
        {
            this.pCA9685 = pCA9685;
            this.controllerName = pCA9685.ControllerName;
            this.address = pCA9685.Address;
        }
        #endregion // Constructors

        #region Member Variables
        private double actualFrequency;
        private int address;
        private string controllerName;
        private bool isInitialized;
        private byte preScale = DEFAULT_PRESCALE;
        private bool[] pinAccess = new bool[PIN_COUNT];
        private I2cDevice primaryDevice;
        private I2cDevice resetDevice;
        private PCA9685Provider pCA9685;
        #endregion // Member Variables

        #region Internal Methods
        private async Task EnsureInitializedAsync()
        {
            // If already initialized, done
            if (isInitialized) { return; }

            // Get a query for I2C
            var aqs = I2cDevice.GetDeviceSelector(controllerName);

            // Find the first I2C device
            var di = (await DeviceInformation.FindAllAsync(aqs)).FirstOrDefault();

            // Make sure we found an I2C device
            if (di == null) { throw new DeviceNotFoundException(controllerName); }

            // Connection settings for primary device
            var primarySettings = new I2cConnectionSettings(this.address);
            primarySettings.BusSpeed = I2cBusSpeed.FastMode;
            primarySettings.SharingMode = I2cSharingMode.Exclusive;

            // Get the primary device
            primaryDevice = await I2cDevice.FromIdAsync(di.Id, primarySettings);
            if (primaryDevice == null) { throw new DeviceNotFoundException("PCA9685 primary device"); }

            // Connection settings for reset device
            var resetSettings = new I2cConnectionSettings(this.address);
            resetSettings.SlaveAddress = I2C_RESET_ADDRESS;

            // Get the reset device
            resetDevice = await I2cDevice.FromIdAsync(di.Id, resetSettings);
            if (resetDevice == null) { throw new DeviceNotFoundException("PCA9685 reset device"); }

            // Initialize the controller
            await InitializeControllerAsync();

            // Done initializing
            isInitialized = true;
        }

        private async Task InitializeControllerAsync()
        {
            if (primaryDevice == null) return;

            ResetController();

            var writeBuf = new byte[2];

            await SleepControllerAsync();

            // Set PRE_SCALE to default
            writeBuf[0] = (byte)Registers.PRESCALE;
            writeBuf[1] = DEFAULT_PRESCALE;
            primaryDevice.Write(writeBuf);

            // Set ActualFrequency to default(200Hz)
            actualFrequency = Math.Round((CLOCK_FREQUENCY) / (double)((preScale + 1) * PULSE_RESOLUTION));

            // Turn them all on - Why are we turning them all on. Seems like we should be turning them off.
            writeBuf[0] = (byte)Registers.ALL_LED_OFF_H;
            writeBuf[1] = 0;
            primaryDevice.Write(writeBuf);
            writeBuf[0] = (byte)Registers.ALL_LED_ON_H;
            writeBuf[1] = (1 << 4);             // 0x10 = LED FULL ON
            primaryDevice.Write(writeBuf);

            await RestartControllerAsync((byte)(Mode1Flags.RESTART | Mode1Flags.AI | Mode1Flags.ALLCALL));
        }

        private void ResetController()
        {
            resetDevice.Write(RESET_COMMAND);
        }

        private async Task RestartControllerAsync(byte mode1)
        {
            var writeBuf = new byte[2];

            writeBuf[0] = (byte)Registers.MODE1;
            writeBuf[1] = mode1;
            primaryDevice.Write(writeBuf);

            // Wait for more than 500us to stabilize.  	
            await Task.Delay(1);
        }

        private async Task<byte> SleepControllerAsync()
        {
            var writeBuf = new byte[2];
            var mode = new byte[1];
            var modeAddr = new byte[1];

            // Read MODE1 register  	
            modeAddr[0] = (byte)Registers.MODE1;
            primaryDevice.WriteRead(modeAddr, mode);

            // Disable Oscillator  	
            writeBuf[0] = (byte)Registers.MODE1;
            writeBuf[1] = (byte)(mode[0] | (byte)Mode1Flags.SLEEP);
            primaryDevice.Write(writeBuf);

            // Wait for more than 500us to stabilize.  	
            await Task.Delay(1);

            return mode[0];
        }
        #endregion // Internal Methods

        #region IPwmControllerProvider Interface
        void IPwmControllerProvider.AcquirePin(int pin)
        {
            if ((pin < 0) || (pin >= PIN_COUNT)) throw new ArgumentOutOfRangeException("pin");

            // Make sure we're initialized
            Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(EnsureInitializedAsync);

            //Debug.WriteLine("PwmControllerProviderPCA9685: Acquiring pin {0}", pin);

            lock (pinAccess)
            {
                if (pinAccess[pin]) { throw new UnauthorizedAccessException(); }
                pinAccess[pin] = true;
            }
        }

        void IPwmControllerProvider.DisablePin(int pin)
        {
            if ((pin < 0) || (pin >= PIN_COUNT)) throw new ArgumentOutOfRangeException("pin");

            // Make sure we're initialized
            Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(EnsureInitializedAsync);

            //Debug.WriteLine("PwmControllerProviderPCA9685: Disabling pin {0}", pin);

            if (!pinAccess[pin])
                throw new InvalidOperationException("Pin is not acquired");

            // Since we are using the totem-pole mode, we just need to  	
            // make sure that the pin is fully OFF.
            var buffer = new byte[2];
            buffer[0] = PwmPinRegs[pin].OffHigh;
            buffer[1] = (byte)(0x1 << 4);
            primaryDevice.Write(buffer);
        }

        void IPwmControllerProvider.EnablePin(int pin)
        {
            if ((pin < 0) || (pin >= PIN_COUNT)) throw new ArgumentOutOfRangeException("pin");

            // Make sure we're initialized
            Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(EnsureInitializedAsync);

            //Debug.WriteLine("PwmControllerProviderPCA9685: Enabling pin {0}", pin);

            if (!pinAccess[pin])
                throw new InvalidOperationException("Pin is not acquired");

            //  	 
            // Since we are using the totem-pole mode, we just need to  	 
            // make sure that the pin is not fully OFF(bit 4 of LEDn_OFF_H should be zero).  	 
            // We set the OFF and ON counter to zero so that the pin is held Low.  	 
            // Subsequent calls to SetPulseParameters should set the pulse width.  	//   
            var buffer = new byte[5];
            buffer[0] = PwmPinRegs[pin].OffHigh;
            buffer[1] = buffer[2] = buffer[3] = buffer[4] = 0x0;
            primaryDevice.Write(buffer);
        }

        void IPwmControllerProvider.ReleasePin(int pin)
        {
            if ((pin < 0) || (pin >= PIN_COUNT)) throw new ArgumentOutOfRangeException("pin");

            //Debug.WriteLine("PwmControllerProviderPCA9685: Releasing pin {0}", pin);

            lock (pinAccess)
            {
                if (!pinAccess[pin])
                    throw new InvalidOperationException("Pin is not acquired");
                pinAccess[pin] = false;
            }
        }

        double IPwmControllerProvider.SetDesiredFrequency(double frequency)
        {
            if (frequency < MIN_FREQUENCY) frequency = MIN_FREQUENCY;
            if (frequency > MAX_FREQUENCY) frequency = MAX_FREQUENCY;

            // Make sure we're initialized
            Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(EnsureInitializedAsync);

            preScale = (byte)(Math.Round((CLOCK_FREQUENCY / (frequency * PULSE_RESOLUTION))) - 1);
            actualFrequency = CLOCK_FREQUENCY / (double)((preScale + 1) * PULSE_RESOLUTION);

            byte mode1 = Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(SleepControllerAsync);

            var buffer = new byte[2];
            // Set PRE_SCALE  	
            buffer[0] = (byte)Registers.PRESCALE;
            buffer[1] = preScale;
            primaryDevice.Write(buffer);

            // Restart  	
            Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(RestartControllerAsync, mode1);

            return actualFrequency;
        }

        /// <inheritdoc/>
        void IPwmControllerProvider.SetPulseParameters(int pin, double dutyCycle, bool invertPolarity)
        {
            if ((pin < 0) || (pin >= PIN_COUNT)) throw new ArgumentOutOfRangeException("pin");
            if ((dutyCycle < 0) || (dutyCycle > 1)) throw new ArgumentOutOfRangeException("dutyCycle");

            //Debug.WriteLine("PwmControllerProviderPCA9685: Setting {0} pin duty cycle to {1}", pin, dutyCycle);

            // Make sure we're initialized
            Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(EnsureInitializedAsync);

            if (!pinAccess[pin])
                throw new InvalidOperationException("Pin is not acquired");

            var buffer = new byte[5];
            ushort onRatio = (ushort)Math.Round(dutyCycle * (PULSE_RESOLUTION - 1));

            // Set the initial Address. AI flag is ON and hence  	
            // address will auto-increment after each byte.
            buffer[0] = PwmPinRegs[pin].OnLow;

            if (invertPolarity)
            {
                onRatio = (ushort)(PULSE_RESOLUTION - onRatio);
                buffer[1] = (byte)(onRatio & 0xFF);
                buffer[2] = (byte)((onRatio & 0x0F00) >> 8);
                buffer[3] = 0;
                buffer[4] = 0;
            }
            else
            {
                buffer[1] = 0;
                buffer[2] = 0;
                buffer[3] = (byte)(onRatio & 0xFF);
                buffer[4] = (byte)((onRatio & 0x0F00) >> 8);
            }
            primaryDevice.Write(buffer);
        }

        double IPwmControllerProvider.ActualFrequency
        {
            get
            {
                return actualFrequency;
            }
        }

        double IPwmControllerProvider.MaxFrequency
        {
            get
            {
                return MAX_FREQUENCY;
            }
        }

        double IPwmControllerProvider.MinFrequency
        {
            get
            {
                return MIN_FREQUENCY;
            }
        }

        int IPwmControllerProvider.PinCount
        {
            get
            {
                return PIN_COUNT;
            }
        }
        #endregion // IPwmControllerProvider Interface

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (resetDevice != null)
                    {
                        resetDevice.Dispose();
                        resetDevice = null;
                    }
                    if (primaryDevice != null)
                    {
                        primaryDevice.Dispose();
                        primaryDevice = null;
                    }
                    pinAccess = null;
                }
                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion // IDisposable Support

    }
}
