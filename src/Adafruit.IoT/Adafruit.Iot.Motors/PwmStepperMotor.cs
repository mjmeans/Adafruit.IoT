using Adafruit.IoT.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Adafruit.IoT.Motors
{
    /// <summary>
    /// Represents a stepper motor connected to a PWM controller.
    /// </summary>
    /// <remarks>
    /// This class supports <see cref="SteppingStyle"/> settings
    /// <see cref="SteppingStyle.Full"/>, <see cref="SteppingStyle.Half"/>,
    /// <see cref="SteppingStyle.Wave"/> and <see cref="SteppingStyle.Microstep8"/>.
    /// </remarks>
    public sealed class PwmStepperMotor : IMotor, IDisposable
    {
        private byte _MICROSTEPS = 8;
        private double[] _MICROSTEP_CURVE = new double[] 
        {
            0,
            0.195090322,
            0.382683432,
            0.555570233,
            0.707106781,
            0.831469612,
            0.923879533,
            0.98078528,
            1
            // 0, 50, 98, 142, 180, 212, 236, 250, 255
        };

        // MICROSTEPS = 16
        // a sinusoidal curve NOT LINEAR!
        // MICROSTEP_CURVE = [0, 25, 50, 74, 98, 120, 141, 162, 180, 197, 212, 225, 236, 244, 250, 253, 255]
        
        private double _powerLevel = 1;           // Set to 1.0 to drive the stepper motor at full power/turque

        private Windows.Devices.Pwm.PwmPin _PWMA;
        private Windows.Devices.Pwm.PwmPin _PWMB;
        private Windows.Devices.Pwm.PwmPin _AIN1;
        private Windows.Devices.Pwm.PwmPin _AIN2;
        private Windows.Devices.Pwm.PwmPin _BIN1;
        private Windows.Devices.Pwm.PwmPin _BIN2;

        private Windows.Devices.Pwm.PwmController _controller;
        private int _steps_per_rev;
        private double _sec_per_step;
        private int _currentstep;
        private int[] _currentcoils = { 0, 0, 0, 0 };
        private Windows.Devices.Pwm.PwmPin[] _coilpins;

<<<<<<< HEAD
=======
        SteppingStyle _stepStyle;
        private MotorState _stepAsyncState;

>>>>>>> refs/remotes/origin/development
        /// <summary>
        /// Initializes a new <see cref="PwmStepperMotor"/> instance.
        /// </summary>
        /// <param name="controller">The <see cref="Windows.Devices.Pwm.PwmController"/> to use.</param>
        /// <param name="driverA">The motor driver channel (1 through 4) for coil A.</param>
        /// <param name="driverB">The motor driver channel (1 through 4) for coil B.</param>
        /// <param name="steps">The number of full steps per revolution that this motor has.</param>
        internal PwmStepperMotor(Windows.Devices.Pwm.PwmController controller, byte driverA, byte driverB, byte steps)
        {
            if ((driverA < 1) || (driverA > 4))
                throw new MotorHatException("Stepper motor driverA must be between 1 and 4.");
            if ((driverB < 1) || (driverB > 4))
                throw new MotorHatException("Stepper motor driverB must be between 1 and 4.");
            if (driverA == driverB)
                throw new MotorHatException("Stepper motor driverA and driverB must be different.");

            this._controller = controller;
            this._steps_per_rev = steps;
            this._sec_per_step = 0.1;
            this._currentstep = 0;

            this._MICROSTEPS = 8;
            var m = new List<double>();
            for (int i = 0; i <= _MICROSTEPS; i++)
                m.Add(Math.Sin(Math.PI/180*90*i/this._MICROSTEPS));
            this._MICROSTEP_CURVE = m.ToArray();

            int pwmapin, ain1pin, ain2pin;
            int pwmbpin, bin1pin, bin2pin;

            switch (driverA)
            {
                case 1:
                    pwmapin = 8;
                    ain2pin = 9;
                    ain1pin = 10;
                    break;
                case 2:
                    pwmapin = 13;
                    ain2pin = 12;
                    ain1pin = 11;
                    break;
                case 3:
                    pwmapin = 2;
                    ain2pin = 3;
                    ain1pin = 4;
                    break;
                case 4:
                    pwmapin = 7;
                    ain2pin = 6;
                    ain1pin = 5;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            switch (driverB)
            {
                case 1:
                    pwmbpin = 8;
                    bin2pin = 9;
                    bin1pin = 10;
                    break;
                case 2:
                    pwmbpin = 13;
                    bin2pin = 12;
                    bin1pin = 11;
                    break;
                case 3:
                    pwmbpin = 2;
                    bin2pin = 3;
                    bin1pin = 4;
                    break;
                case 4:
                    pwmbpin = 7;
                    bin2pin = 6;
                    bin1pin = 5;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            this._PWMA = this._controller.OpenPin(pwmapin);
            this._AIN2 = this._controller.OpenPin(ain2pin);
            this._AIN1 = this._controller.OpenPin(ain1pin);
            this._PWMB = this._controller.OpenPin(pwmbpin);
            this._BIN2 = this._controller.OpenPin(bin2pin);
            this._BIN1 = this._controller.OpenPin(bin1pin);
            this._PWMA.Start();
            this._PWMB.Start();
            this._AIN1.Start();
            this._AIN2.Start();
            this._BIN1.Start();
            this._BIN2.Start();
            _coilpins = new Windows.Devices.Pwm.PwmPin[] 
            {
                this._AIN2,
                this._BIN1,
                this._AIN1,
                this._BIN2
            };
        }

        /// <summary>
        /// Set the desired motor's speed in revolutions per minute.
        /// </summary>
        /// <param name="rpm">The revolutions per minute.</param>
        /// <remarks>
        /// This setting applies to multiple motor steps processed by the <see cref="StepAsync"/> method.
        /// </remarks>
        public void SetSpeed(double rpm)
        {
            this._sec_per_step = 60.0 / rpm / _steps_per_rev;
        }

        /// <summary>
        /// Steps the motor one step in the direction and style specified.
        /// </summary>
        /// <param name="direction">A <see cref="Direction"/>.</param>
        /// <param name="stepStyle">A <see cref="SteppingStyle"/>.</param>
        /// <returns>The current step number.</returns>
        /// <remarks>
        /// .
        /// </remarks>
        public int OneStep(Direction direction, SteppingStyle stepStyle)
        {
            double pwm_a, pwm_b;
            int[] coils;

            pwm_a = pwm_b = 1;

            // first determine what sort of stepping procedure we're up to
            if (stepStyle == SteppingStyle.Full)
            {
                if (((this._currentstep / (this._MICROSTEPS / 2)) % 2) == 0)
                {
                    // we're at an even step, weird
                    if (direction == Direction.Forward)
                        this._currentstep += this._MICROSTEPS / 2;
                    else
                        this._currentstep -= this._MICROSTEPS / 2;
                }
                else
                {
                    // go to next odd step
                    if (direction == Direction.Forward)
                        this._currentstep += this._MICROSTEPS;
                    else
                        this._currentstep -= this._MICROSTEPS;
                }
            }
            if (stepStyle == SteppingStyle.Wave)
            {
                if (((this._currentstep / (this._MICROSTEPS / 2)) % 2) == 1)
                {
                    // we're at an odd step, weird
                    if (direction == Direction.Forward)
                        this._currentstep += this._MICROSTEPS / 2;
                    else
                        this._currentstep -= this._MICROSTEPS / 2;
                }
                else
                {
                    // go to next even step
                    if (direction == Direction.Forward)
                        this._currentstep += this._MICROSTEPS;
                    else
                        this._currentstep -= this._MICROSTEPS;
                }
            }
            else if (stepStyle == SteppingStyle.Half)
            {
                if (direction == Direction.Forward)
                    this._currentstep += this._MICROSTEPS / 2;
                else
                    this._currentstep -= this._MICROSTEPS / 2;
            }
            else if (stepStyle == SteppingStyle.Microstep8)
            {
                if (direction == Direction.Forward)
                    this._currentstep += 1;
                else
                    this._currentstep -= 1;

                // go to next 'step' and wrap around
                this._currentstep += this._MICROSTEPS * 4;
                this._currentstep %= this._MICROSTEPS * 4;

                pwm_a = pwm_b = 0;
                if ((this._currentstep >= 0) && (this._currentstep < this._MICROSTEPS))
                {
                    pwm_a = this._MICROSTEP_CURVE[this._MICROSTEPS - this._currentstep];
                    pwm_b = this._MICROSTEP_CURVE[this._currentstep];
                }
                else if ((this._currentstep >= this._MICROSTEPS) && (this._currentstep < this._MICROSTEPS * 2))
                {
                    pwm_a = this._MICROSTEP_CURVE[this._currentstep - this._MICROSTEPS];
                    pwm_b = this._MICROSTEP_CURVE[this._MICROSTEPS * 2 - this._currentstep];
                }
                else if ((this._currentstep >= this._MICROSTEPS * 2) && (this._currentstep < this._MICROSTEPS * 3))
                {
                    pwm_a = this._MICROSTEP_CURVE[this._MICROSTEPS * 3 - this._currentstep];
                    pwm_b = this._MICROSTEP_CURVE[this._currentstep - this._MICROSTEPS * 2];
                }
                else if ((this._currentstep >= this._MICROSTEPS * 3) && (this._currentstep < this._MICROSTEPS * 4))
                {
                    pwm_a = this._MICROSTEP_CURVE[this._currentstep - this._MICROSTEPS * 3];
                    pwm_b = this._MICROSTEP_CURVE[this._MICROSTEPS * 4 - this._currentstep];
                }
            }

            // go to next 'step' and wrap around
            this._currentstep += this._MICROSTEPS * 4;
            this._currentstep %= this._MICROSTEPS * 4;

            // only really used for micro stepping, otherwise always on!
            this._PWMA.SetActiveDutyCyclePercentage(pwm_a * this.PowerFactor);
            this._PWMB.SetActiveDutyCyclePercentage(pwm_b * this.PowerFactor);

            // set up coil energizing!
            coils = new int[] { 0, 0, 0, 0 };

            if (stepStyle == SteppingStyle.Microstep8)
            {
                if ((this._currentstep >= 0) && (this._currentstep < this._MICROSTEPS))
                    coils = new int[] { 1, 1, 0, 0 };
                else if ((this._currentstep >= this._MICROSTEPS) && (this._currentstep < this._MICROSTEPS * 2))
                    coils = new int[] { 0, 1, 1, 0 };
                else if ((this._currentstep >= this._MICROSTEPS * 2) && (this._currentstep < this._MICROSTEPS * 3))
                    coils = new int[] { 0, 0, 1, 1 };
                else if ((this._currentstep >= this._MICROSTEPS * 3) && (this._currentstep < this._MICROSTEPS * 4))
                    coils = new int[] { 1, 0, 0, 1 };
            }
            else
            {
                int[][] step2coils = new int[][] {
                    new int[] { 1, 0, 0, 0 },
                    new int[] { 1, 1, 0, 0 },
                    new int[] { 0, 1, 0, 0 },
                    new int[] { 0, 1, 1, 0 },
                    new int[] { 0, 0, 1, 0 },
                    new int[] { 0, 0, 1, 1 },
                    new int[] { 0, 0, 0, 1 },
                    new int[] { 1, 0, 0, 1 }
                };
                coils = step2coils[this._currentstep / (this._MICROSTEPS / 2)];
            }

            // Turn on any coils that are off and need to be turned on
            for (int i = 0; i < 4; i++)
            {
                if ((coils[i] == 1) && (_currentcoils[i] == 0))
                {
                    _coilpins[i].SetActiveDutyCyclePercentage(1.0);
                }
            }

            // Turn off any coils that are on and need to be turned off
            for (int i = 0; i < 4; i++)
            {
                if ((coils[i] == 0) && (_currentcoils[i] == 1))
                {
                    _coilpins[i].SetActiveDutyCyclePercentage(0);
                }
            }

            //Debug.WriteLine("coils({0}, {1}, {2}, {3}) pwm({4}, {5})", coils[0], coils[1], coils[2], coils[3], pwm_a, pwm_b);
            _currentcoils = coils;
            return this._currentstep;
        }

        /// <summary>
        /// Steps a specified number of steps, direction and stepping style.
        /// </summary>
        /// <param name="steps">The number of steps to process.</param>
        /// <param name="direction">A <see cref="Direction"/>.</param>
        /// <param name="stepStyle">A <see cref="SteppingStyle"/>.</param>
        /// <remarks>
        /// .
        /// </remarks>
        public IAsyncAction StepAsync(int steps, Direction direction, SteppingStyle stepStyle)
        {
<<<<<<< HEAD
            return StepAsyncInternal(steps, direction, stepStyle).AsAsyncAction();
        }

=======
            if (_stepAsyncState == MotorState.Run)
                throw new InvalidOperationException("Stepper motor is already running.");

            cts = new CancellationTokenSource();
            _stepStyle = stepStyle;
            WorkItemHandler loop = new WorkItemHandler((IAsyncAction) =>
                motorThread(steps, direction, stepStyle, cts.Token)
            );
            _runTask = ThreadPool.RunAsync(loop, WorkItemPriority.High);
            return _runTask;
        }

        CancellationTokenSource cts;

>>>>>>> refs/remotes/origin/development
        /// <summary>
        /// Async method to step the motor multiple steps.
        /// </summary>
        /// <param name="steps">The number of steps to step the motor.</param>
        /// <param name="direction">A <see cref="Direction"/>.</param>
        /// <param name="stepStyle">A <see cref="SteppingStyle"/>.</param>
        /// <param name="ct">A <see cref="CancellationToken"/>.</param>
        /// <returns>The current step number.</returns>
        /// <remarks>
        /// The speed at which the steps will occur will be at the best effort to achieve the desired <see cref="SetSpeed(double)"/> speed.
        /// </remarks>
        internal void motorThread(int steps, Direction direction, SteppingStyle stepStyle, CancellationToken ct)
        {
<<<<<<< HEAD
=======
            _stepAsyncState = MotorState.Run;
>>>>>>> refs/remotes/origin/development
            double s_per_s;
            int lateststep = 0;

            var watch = System.Diagnostics.Stopwatch.StartNew();

            s_per_s = this._sec_per_step;
            if (stepStyle == SteppingStyle.Half)
            {
                s_per_s /= 2.0;
                steps *= 2;
            }
            if (stepStyle == SteppingStyle.Microstep8)
            {
                s_per_s /= this._MICROSTEPS;
                steps *= this._MICROSTEPS;
            }
            long ticksperstep = (long)(s_per_s * Stopwatch.Frequency);
            long nexttime = 0;
            for (int s=0; s<steps; s++)
            {
                lateststep = this.OneStep(direction, stepStyle);
                nexttime += ticksperstep;
                await Task.Run(() =>
                {
                    while (watch.ElapsedTicks < ticksperstep)
                    { }
                });
            }
            if (stepStyle == SteppingStyle.Microstep8)
            {
                // Always end in full step
                while ((lateststep != 0) && (lateststep != this._MICROSTEPS))
                {
                    if (ct.IsCancellationRequested) break;
                    lateststep = this.OneStep(direction, stepStyle);
                    nexttime += ticksperstep;
                    while (watch.ElapsedTicks < nexttime)
                    {
<<<<<<< HEAD
                        while (watch.ElapsedTicks < ticksperstep)
                        { }
                    });
                }
            }
=======
                        if (ct.IsCancellationRequested) break;
                    }
                }
            }
            catch (TaskCanceledException)
            { }
            finally
            {
                if (stepStyle == SteppingStyle.Microstep8)
                {
                    // Always end in full step
                    while ((lateststep != 0) && (lateststep != this._MICROSTEPS))
                    {
                        lateststep = this.OneStep(direction, stepStyle);
                        nexttime += ticksperstep;
                        while (watch.ElapsedTicks < nexttime) { }
                    }
                }
            }
            _stepAsyncState = MotorState.Brake;
        }

        /// <summary>
        /// The current <see cref="MotorState"/>.
        /// </summary>
        public MotorState StepAsyncState
        {
            get
            {
                return _stepAsyncState;
            }
        }

        /// <summary>
        /// Sets the stepping style for this stepper motor.
        /// </summary>
        /// <param name="stepStyle">A <see cref="SteppingStyle"/>.</param>
        public void SetStepStyle(SteppingStyle stepStyle)
        {
            _stepStyle = stepStyle;
        }

        IAsyncAction _runTask;

        /// <inheritdoc/>
        public void Run(Direction direction)
        {
            Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(cancelRun);

            // Do not await, this should continue running
            Task.Run(() => StepAsync(-1, direction, _stepStyle))
                .ContinueWith(t => { throw t.Exception; }, 
                TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Stop the stepper motor but keeps the stepper drivers energized.
        /// </summary>
        public void Brake()
        {
            Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(cancelRun);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(cancelRun);
            for (int i = 0; i < 4; i++)
            {
                _coilpins[i].SetActiveDutyCyclePercentage(0.0);
            }
            _stepAsyncState = MotorState.Stop;
        }

        private async Task cancelRun()
        {
            if (cts != null)
            {
                cts.Cancel();
                await Task.Run(() =>
                {
                    while (_runTask.Status == AsyncStatus.Started) { };
                });
                cts = null;
            }
>>>>>>> refs/remotes/origin/development
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Gets or sets the power level of this stepper motor.
        /// </summary>
        /// <value>
        /// A power scaling factor from 0.0 to 1.0.
        /// </value>
        /// <remarks>
        /// A changing in power factor will be applied to subsequent motor steps.
        /// </remarks>
        public double PowerFactor
        {
            get
            {
                return _powerLevel;
            }

            set
            {
                _powerLevel = value;
            }
        }

        /// <inheritdoc/>
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose the opened pins so that they are released
                    if (this._PWMA != null)
                    {
                        if (this._PWMA.IsStarted) this._PWMA.Stop();
                        this._PWMA.Dispose();
                        this._PWMA = null;
                    }
                    if (this._PWMB != null)
                    {
                        if (this._PWMB.IsStarted) this._PWMB.Stop();
                        this._PWMB.Dispose();
                        this._PWMB = null;
                    }
                    if (this._AIN1 != null)
                    {
                        if (this._AIN1.IsStarted) this._AIN1.Stop();
                        this._AIN1.Dispose();
                        this._AIN1 = null;
                    }
                    if (this._AIN2 != null)
                    {
                        if (this._AIN2.IsStarted) this._AIN2.Stop();
                        this._AIN2.Dispose();
                        this._AIN2 = null;
                    }
                    if (this._BIN1 != null)
                    {
                        if (this._BIN1.IsStarted) this._BIN1.Stop();
                        this._BIN1.Dispose();
                        this._BIN1 = null;
                    }
                    if (this._BIN2 != null)
                    {
                        if (this._BIN2.IsStarted) this._BIN2.Stop();
                        this._BIN2.Dispose();
                        this._BIN2 = null;
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

