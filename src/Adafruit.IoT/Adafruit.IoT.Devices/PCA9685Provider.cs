using Microsoft.IoT.DeviceHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private List<PCA9685ControllerProvider> controllers;
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

                controllers = new List<PCA9685ControllerProvider>();
                controllers.Add(new PCA9685ControllerProvider(this));
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

}
