using Adafruit.IoT.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
    /// <see cref="SteppingStyle.FullWave"/>, <see cref="SteppingStyle.HalfWave"/>,
    /// <see cref="SteppingStyle.Microstep4"/>, <see cref="SteppingStyle.Microstep8"/>,
    /// <see cref="SteppingStyle.Microstep16"/> and <see cref="SteppingStyle.Microstep32"/>.
    /// </remarks>
    public sealed class PwmStepperMotor : IMotor, IDisposable
    {
        private MotorCoil[] _stepTable;
        private SteppingStyle _stepStyle;
        private MotorState _stepAsyncState;
        private double _last_pwm_a, _last_pwm_b;
        private int _last_sign_a, _last_sign_b;
        private IAsyncAction _runTask;

        private class MotorCoil
        {
            public double Ia, Ib;
            public MotorCoil(double Ia, double Ib)
            {
                this.Ia = Ia;
                this.Ib = Ib;
            }
        }

        // Various step tables are documetned here http://www.lamja.com/?p=140
        private MotorCoil[] BuildStepTable(SteppingStyle stepStyle)
        {
            int steps = 0;
            double offset = 0;
            List<MotorCoil> stepTable = new List<MotorCoil>();
            Debug.WriteLine(stepStyle);

            switch (stepStyle)
            {
                case SteppingStyle.Full:
                    steps = 1;
                    stepTable.Add(new MotorCoil( 1,  1));
                    stepTable.Add(new MotorCoil( 1, -1));
                    stepTable.Add(new MotorCoil(-1, -1));
                    stepTable.Add(new MotorCoil(-1,  1));
                    break;
                case SteppingStyle.Half:
                    steps = 1;
                    stepTable.Add(new MotorCoil( 0,  1));
                    stepTable.Add(new MotorCoil( 1,  1));
                    stepTable.Add(new MotorCoil( 1,  0));
                    stepTable.Add(new MotorCoil( 1, -1));
                    stepTable.Add(new MotorCoil( 0, -1));
                    stepTable.Add(new MotorCoil(-1, -1));
                    stepTable.Add(new MotorCoil(-1,  0));
                    stepTable.Add(new MotorCoil(-1,  1));
                    break;
                case SteppingStyle.FullWave:
                    steps = 1;
                    break;
                case SteppingStyle.HalfWave:
                    steps = 2;
                    break;
                case SteppingStyle.Microstep4:
                    steps = 4;
                    break;
                case SteppingStyle.Microstep8:
                    steps = 8;
                    break;
                case SteppingStyle.Microstep16:
                    steps = 16;
                    break;
                case SteppingStyle.Microstep32:
                    steps = 32;
                    break;
            }

            if (stepTable.Count == 0)
            {
                // Calculate microsteps
                for (double i = 0; i < steps * 4; i++)
                {
                    var deg = (i / steps);
                    var phase_a = Math.Sin((Math.PI / 2.0 * (deg + offset)));
                    var phase_b = Math.Cos((Math.PI / 2.0 * (deg + offset)));
                    stepTable.Add(new MotorCoil(phase_a, phase_b));
                }
            }
            for (int i = 0; i < steps * 4; i++)
            {
                Debug.WriteLine(string.Format("{0} {1:0.00} {2:0.00}", i, stepTable[i].Ia, stepTable[i].Ib));
            }

            return stepTable.ToArray();
        }

        private double _powerLevel = 1;           // Set to 1.0 to drive the stepper motor at full power/torque
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

        private Windows.Devices.Pwm.PwmPin _PWMA;
        private Windows.Devices.Pwm.PwmPin _PWMB;
        private Windows.Devices.Pwm.PwmPin _AIN1;
        private Windows.Devices.Pwm.PwmPin _AIN2;
        private Windows.Devices.Pwm.PwmPin _BIN1;
        private Windows.Devices.Pwm.PwmPin _BIN2;

        private Windows.Devices.Pwm.PwmController _controller;
        private double _steps_per_rev;
        private double _sec_per_step;
        private long _ticks_per_step;
        private int _currentstep;
        private int[] _currentcoils = { 0, 0, 0, 0 };
        private Windows.Devices.Pwm.PwmPin[] _coilpins;

        /// <summary>
        /// Initializes a new <see cref="PwmStepperMotor"/> instance.
        /// </summary>
        /// <param name="controller">The <see cref="Windows.Devices.Pwm.PwmController"/> to use.</param>
        /// <param name="driverA">The motor driver channel (1 through 4) for coil A.</param>
        /// <param name="driverB">The motor driver channel (1 through 4) for coil B.</param>
        /// <param name="stepsPerRev">The number of full steps per revolution that this motor has.</param>
        internal PwmStepperMotor(Windows.Devices.Pwm.PwmController controller, byte driverA, byte driverB, double stepsPerRev)
        {
            if ((driverA < 1) || (driverA > 4))
                throw new MotorHatException("Stepper motor driverA must be between 1 and 4.");
            if ((driverB < 1) || (driverB > 4))
                throw new MotorHatException("Stepper motor driverB must be between 1 and 4.");
            if (driverA == driverB)
                throw new MotorHatException("Stepper motor driverA and driverB must be different.");

            this._controller = controller;
            this._steps_per_rev = stepsPerRev;

            // Default values
            this._sec_per_step = 0.1;
            this._currentstep = 0;

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
            this._AIN1 = this._controller.OpenPin(ain1pin);
            this._AIN2 = this._controller.OpenPin(ain2pin);
            this._PWMB = this._controller.OpenPin(pwmbpin);
            this._BIN1 = this._controller.OpenPin(bin1pin);
            this._BIN2 = this._controller.OpenPin(bin2pin);
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
            this._ticks_per_step = (long)(this._sec_per_step * Stopwatch.Frequency);
            Debug.WriteLine("StopWatch is " + (Stopwatch.IsHighResolution ? "" : "not ") + "high resolution");
            Debug.WriteLine("\tStopWatch frequency is {0} ticks per second", Stopwatch.Frequency);
            Debug.WriteLine("\t60 sec/min * {0} RPM / {1} steps/rev = {2} sec/step", rpm, _steps_per_rev, _sec_per_step);
            Debug.WriteLine("\t{0} sec/step * {1} ticks/sec = {2} ticks/step", _sec_per_step, Stopwatch.Frequency, _ticks_per_step);
        }

        /// <summary>
        /// Steps the motor one step in the direction and style specified.
        /// </summary>
        /// <param name="direction">A <see cref="Direction"/>.</param>
        /// <returns>The current step number.</returns>
        /// <remarks>
        /// .
        /// </remarks>
        public int OneStep(Direction direction)
        {
            double pwm_a, pwm_b;
            int sign_a, sign_b;

            pwm_a = pwm_b = 1;

            this._currentstep += 1;
            this._currentstep %= _stepTable.Length;

            pwm_a = Math.Abs(_stepTable[this._currentstep].Ia);
            pwm_b = Math.Abs(_stepTable[this._currentstep].Ib);
            sign_a = Math.Sign(_stepTable[this._currentstep].Ia);
            sign_b = Math.Sign(_stepTable[this._currentstep].Ib);

            // only change the pwm duty cycle if this step is a different duty cycle
            if (pwm_a != _last_pwm_a)
            {
                this._PWMA.SetActiveDutyCyclePercentage(Math.Abs(pwm_a * this.PowerFactor));
            }
            if (pwm_b != _last_pwm_b)
            {
                this._PWMB.SetActiveDutyCyclePercentage(Math.Abs(pwm_b * this.PowerFactor));
            }

            if (sign_b != _last_sign_b)
            {
                // Turn off first
                if (this._BIN2.GetActiveDutyCyclePercentage() != 0.0f)
                    this._BIN2.SetActiveDutyCyclePercentage(0);
                if (this._BIN1.GetActiveDutyCyclePercentage() != 0.0f)
                    this._BIN1.SetActiveDutyCyclePercentage(0);
                // turn on
                if (sign_b > 0)
                {
                    if (this._BIN1.GetActiveDutyCyclePercentage() != 1.0f)
                        this._BIN1.SetActiveDutyCyclePercentage(1.0);
                }
                else
                {
                    if (this._BIN2.GetActiveDutyCyclePercentage() != 1.0f)
                        this._BIN2.SetActiveDutyCyclePercentage(1.0);
                }
            }
            if (sign_a != _last_sign_a)
            {
                // Turn off first
                if (this._AIN1.GetActiveDutyCyclePercentage() != 0.0f)
                    this._AIN1.SetActiveDutyCyclePercentage(0);
                if (this._AIN2.GetActiveDutyCyclePercentage() != 0.0f)
                    this._AIN2.SetActiveDutyCyclePercentage(0);
                // turn on
                if (sign_a > 0)
                {
                    if (this._AIN1.GetActiveDutyCyclePercentage() != 1.0f)
                        this._AIN1.SetActiveDutyCyclePercentage(1.0);
                }
                else
                {
                    if (this._AIN2.GetActiveDutyCyclePercentage() != 1.0f)
                        this._AIN2.SetActiveDutyCyclePercentage(1.0);
                }
            }
            _last_pwm_a = pwm_a;
            _last_pwm_b = pwm_b;
            _last_sign_a = sign_a;
            _last_sign_b = sign_b;

            return this._currentstep;
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
        [Obsolete("Use SetStepStyle(SteppingStyle); OneStep(Direction); instead.")]
        public int OneStep(Direction direction, SteppingStyle stepStyle)
        {
            SetStepStyle(stepStyle);
            return OneStep(direction);
        }

        /// <summary>
        /// Steps a specified number of steps, direction and stepping style.
        /// </summary>
        /// <param name="steps">The number of steps to process.</param>
        /// <param name="direction">A <see cref="Direction"/>.</param>
        /// <remarks>
        /// StepAsync cannot be called if the motor is already running.
        /// </remarks>
        public IAsyncAction StepAsync(double steps, Direction direction)
        {
            if (_stepAsyncState == MotorState.Run)
                throw new InvalidOperationException("Stepper motor is already running.");

            cts = new CancellationTokenSource();
            //Task t = new Task(() => { }, cts.Token, TaskCreationOptions.None);            
            WorkItemHandler loop = new WorkItemHandler((IAsyncAction) =>
                motorThread(steps, direction, cts.Token)
            );
            _runTask = ThreadPool.RunAsync(loop, WorkItemPriority.High);
            return _runTask;
        }

        CancellationTokenSource cts;

        /// <summary>
        /// Async method to step the motor multiple steps.
        /// </summary>
        /// <param name="steps">The number of steps to step the motor.</param>
        /// <param name="direction">A <see cref="Direction"/>.</param>
        /// <param name="ct">A <see cref="CancellationToken"/>.</param>
        /// <returns>The current step number.</returns>
        /// <remarks>
        /// The speed at which the steps will occur will be at the best effort to achieve the desired <see cref="SetSpeed(double)"/> speed.
        /// </remarks>
        internal void motorThread(double steps, Direction direction, CancellationToken ct)
        {
            _stepAsyncState = MotorState.Run;
            long ticks_per_step = this._ticks_per_step;
            double _steps = steps;
            int lateststep = 0;

            var watch = new System.Diagnostics.Stopwatch();

            switch (_stepStyle)
            {
                case SteppingStyle.Full:
                case SteppingStyle.FullWave:
                    break;
                case SteppingStyle.Half:
                case SteppingStyle.HalfWave:
                    ticks_per_step /= 2;
                    _steps *= 2;
                    break;
                case SteppingStyle.Microstep4:
                    ticks_per_step /= 4;
                    _steps *= 4;
                    break;
                case SteppingStyle.Microstep8:
                    ticks_per_step /= 8;
                    _steps *= 8;
                    break;
                case SteppingStyle.Microstep16:
                    ticks_per_step /= 16;
                    _steps *= 16;
                    break;
                case SteppingStyle.Microstep32:
                    ticks_per_step /= 32;
                    _steps *= 32;
                    break;
            }
            long nexttime = 0;
            int s = 0;
            try
            {
                watch.Start();
                while ((s < _steps) || (steps == -1))
                {
                    lateststep = this.OneStep(direction);
                    if (ct.IsCancellationRequested) break;
                    if (_steps != -1) s++;
                    nexttime += ticks_per_step;
                    while (watch.ElapsedTicks < nexttime)
                    { }
                }
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
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
            this._stepStyle = stepStyle;
            this._stepTable = BuildStepTable(stepStyle);
        }

        /// <inheritdoc/>
        public void Run(Direction direction)
        {
            Microsoft.IoT.DeviceHelpers.TaskExtensions.UISafeWait(cancelRun);

            // Do not await, this should continue running
            Task.Run(() => StepAsync(-1, direction))
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
                    DisposePin(ref this._PWMA);
                    DisposePin(ref this._PWMB);
                    DisposePin(ref this._AIN1);
                    DisposePin(ref this._AIN2);
                    DisposePin(ref this._BIN1);
                    DisposePin(ref this._BIN2);
                }
                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }

        private void DisposePin(ref Windows.Devices.Pwm.PwmPin p)
        {
            if (p != null)
            {
                if (p.IsStarted) p.Stop();
                p.Dispose();
                p = null;
            }
        }
        #endregion
    }
}

