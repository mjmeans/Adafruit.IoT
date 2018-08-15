using Adafruit.IoT.Devices;
using Adafruit.IoT.Motors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MotorHat2348Sample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private class StepperMotorProperties
        {
            public string Manufacturer;
            public string Model;
            public float Step_Angle_Degrees;
            public float Rated_Voltage;
            public float Current_AmpsPerPhase;
            public float Resistance_OhmsPerPhase;
            public float Inductance_mH;
            public string Holding_Torque_kgcm;
            public float Gear_Ratio;
        }

        StepperMotorProperties[] stepperMotorProperties = new StepperMotorProperties[]
        {
            new StepperMotorProperties() // Adafruit https://www.adafruit.com/product/324
            {   Manufacturer = "Bohong Stepping Motor", Model = "42HB34F08AB-06",
                Step_Angle_Degrees = 1.8f, Gear_Ratio = 1, Holding_Torque_kgcm = "1.6 kg-cm",
                Rated_Voltage = 12f, Current_AmpsPerPhase = 0.35f,
                Resistance_OhmsPerPhase = 34f, Inductance_mH = 33f },
            new StepperMotorProperties() // http://voltom.ru/info/42HB34F08AB-06.pdf
            {   Manufacturer = "Bohong Stepping Motor", Model = "42HB34F08AB-06",
                Step_Angle_Degrees = 1.8f, Gear_Ratio = 1, Holding_Torque_kgcm = "2.4 kg-cm",
                Rated_Voltage = 4.96f, Current_AmpsPerPhase = 0.8f,
                Resistance_OhmsPerPhase = 6.2f, Inductance_mH = 10f },
            new StepperMotorProperties()
            {   Manufacturer = "Stepper Online", Model = "17HS13-0404S-PG27",
                Step_Angle_Degrees = 1.8f, Gear_Ratio = 26f + 103f / 121f, Holding_Torque_kgcm = "30.6 kg-cm",
                Rated_Voltage = 12f, Current_AmpsPerPhase = 0.4f,
                Resistance_OhmsPerPhase = 30f, Inductance_mH = 37f }
        };

        double steps_per_rev;

        public MainPage()
        {
            this.InitializeComponent();
            mh = new MotorHat2348(0x60);

            var mystepper = stepperMotorProperties[0];
            steps_per_rev = (int)(360f / mystepper.Step_Angle_Degrees * mystepper.Gear_Ratio);
            stepper = mh.CreateStepperMotor(1, 2, steps_per_rev);
            dcmotor = mh.CreateDCMotor(3);
        }

        MotorHat2348 mh;
        PwmStepperMotor stepper;
        PwmDCMotor dcmotor;

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            await Task.CompletedTask;
            button.IsEnabled = false;

            stepper.PowerFactor = 1;

            var watch = new System.Diagnostics.Stopwatch();
            textBlock.Text = "";

            stepper.SetStepStyle(SteppingStyle.Full);
            watch.Restart();
            for (int i = 0; i < steps_per_rev; i++)
            {
                stepper.OneStep(Direction.Forward);
            }
            watch.Stop();
            stepper.Stop();
            textBlock.Text += "\n" + string.Format("Full step speed = {0:F2} rpm", 60 / watch.Elapsed.TotalSeconds);

            stepper.SetStepStyle(SteppingStyle.Half);
            watch.Restart();
            for (int i = 0; i < steps_per_rev * 2; i++)
            {
                stepper.OneStep(Direction.Forward);
            }
            watch.Stop();
            stepper.Stop();
            textBlock.Text += "\n" + string.Format("Half step speed = {0:F2} rpm", 60 / watch.Elapsed.TotalSeconds);

            stepper.SetStepStyle(SteppingStyle.FullWave);
            watch.Restart();
            for (int i = 0; i < steps_per_rev; i++)
            {
                stepper.OneStep(Direction.Forward);
            }
            watch.Stop();
            stepper.Stop();
            textBlock.Text += "\n" + string.Format("FullWave speed = {0:F2} rpm", 60 / watch.Elapsed.TotalSeconds);

            stepper.SetStepStyle(SteppingStyle.HalfWave);
            watch.Restart();
            for (int i = 0; i < steps_per_rev * 2; i++)
            {
                stepper.OneStep(Direction.Forward);
            }
            watch.Stop();
            stepper.Stop();
            textBlock.Text += "\n" + string.Format("HalfWave speed = {0:F2} rpm", 60 / watch.Elapsed.TotalSeconds);

            stepper.SetStepStyle(SteppingStyle.Microstep4);
            watch.Restart();
            for (int i = 0; i < steps_per_rev * 4; i++)
            {
                stepper.OneStep(Direction.Forward);
            }
            watch.Stop();
            stepper.Stop();
            textBlock.Text += "\n" + string.Format("Microstep4 speed = {0:F2} rpm", 60 / watch.Elapsed.TotalSeconds);

            stepper.SetStepStyle(SteppingStyle.Microstep8);
            watch.Restart();
            for (int i = 0; i < steps_per_rev * 8; i++)
            {
                stepper.OneStep(Direction.Forward);
            }
            watch.Stop();
            stepper.Stop();
            textBlock.Text += "\n" + string.Format("Microstep8 speed = {0:F2} rpm", 60 / watch.Elapsed.TotalSeconds);

            stepper.SetStepStyle(SteppingStyle.Microstep16);
            watch.Restart();
            for (int i = 0; i < steps_per_rev * 16; i++)
            {
                stepper.OneStep(Direction.Forward);
            }
            watch.Stop();
            stepper.Stop();
            textBlock.Text += "\n" + string.Format("Microstep16 speed = {0:F2} rpm", 60 / watch.Elapsed.TotalSeconds);

            stepper.SetStepStyle(SteppingStyle.Microstep32);
            watch.Restart();
            for (int i = 0; i < steps_per_rev * 32; i++)
            {
                stepper.OneStep(Direction.Forward);
            }
            watch.Stop();
            stepper.Stop();
            textBlock.Text += "\n" + string.Format("Microstep32 speed = {0:F2} rpm", 60 / watch.Elapsed.TotalSeconds);

            stepper.SetStepStyle(SteppingStyle.Microstep16);
            stepper.SetSpeed(1);
            watch.Restart();
            await stepper.StepAsync(steps_per_rev, Direction.Forward);
            watch.Stop();
            stepper.Stop();
            textBlock.Text += "\n" + string.Format("Microstep16 @ 1 RPM actual speed = {0:F2} rpm", 60 / watch.Elapsed.TotalSeconds);

            button.IsEnabled = true;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (mh != null)
            {
                mh.Dispose();
                mh = null;
            }
            Application.Current.Exit();
        }

        private bool dcmotorrunning;
        private bool dcmotorbackward;

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            if (dcmotorrunning)
            {
                dcmotor.Stop();
                dcmotorrunning = false;
            }
            else
            {
                dcmotor.SetSpeed(slider.Value / 100.0);
                switch (dcmotorbackward)
                {
                    case false:
                        dcmotor.Run(Direction.Forward);
                        break;
                    case true:
                        dcmotor.Run(Direction.Backward);
                        break;
                }
                dcmotorbackward = !dcmotorbackward;
                dcmotorrunning = true;
            }
        }


        //private async void button_Click(object sender, RoutedEventArgs e)
        //{
        //    button.IsEnabled = false;
        //    textBlock.Text = "";
        //    var tc = new ThreadClass();
        //    tc.StepsPerRun = 2700;      // The external I/O requires this many steps to ompelte a run
        //    tc.Rpm = 20;                // We want the speed to be 20 runs per minute            
        //    var watch = System.Diagnostics.Stopwatch.StartNew();
        //    await tc.StepAsync(2700);   // We want 2700 steps to occur (one complete run)
        //    watch.Stop();
        //    // The total elapsed seconds 5 seconds, or 20 runs per minute.
        //    textBlock.Text = string.Format("Run speed per minute = {0:F2}", 60 / watch.Elapsed.TotalSeconds);
        //    button.IsEnabled = true;
        //}

        //public class ThreadClass
        //{
        //    // The purpose of this class is to call Step() a specific number of times at as close as possible to a precise interval.
        //    // If any interval is missed, for example due to Windows being busy, it will be called again immediately to catch up.
        //    //
        //    // Step() takes 1 milliseconds to complete, so it can run 60,000 times per minute.
        //    // StepsPerRun is the number of steps it takes to complete a Run of work on the outside I/O.
        //    //.
        //    // Rpm is the number of runs per minute speed that we want it to pace itself at.
        //    //
        //    // The calling program will set Steps Per Run to 2700.
        //    // It takes 2700 steps to complete a Run and a Step() can be done up to 60,000 times per minute.
        //    // So that means we should get a maximum speed of 22.22 runs per minute.

        //    CancellationTokenSource _cts;
        //    IAsyncAction _runTask;
        //    long _ticks_per_step;
        //    int _currentstep;

        //    public int StepsPerRun;
        //    public int Rpm;

        //    public IAsyncAction StepAsync(int steps)
        //    {
        //        _ticks_per_step = (long)(60.0 / Rpm / StepsPerRun * Stopwatch.Frequency);
        //        _cts = new CancellationTokenSource();
        //        WorkItemHandler loop = new WorkItemHandler((IAsyncAction) =>
        //            myThread(steps, _cts.Token)
        //        );
        //        _runTask = ThreadPool.RunAsync(loop, WorkItemPriority.High);
        //        return _runTask;
        //    }

        //    internal void myThread(int steps, CancellationToken ct)
        //    {
        //        int st = steps;
        //        long nexttime = 0;
        //        int laststep = 0;
        //        int s = 0;
        //        // Do something that takes anywhere from 2 to 10 ms beginning at precisely separated intervals
        //        var watch = System.Diagnostics.Stopwatch.StartNew();
        //        while ((s < st) || (steps == -1))
        //        {
        //            laststep = Step();
        //            if (st != -1) s++;
        //            nexttime += _ticks_per_step;
        //            while (watch.ElapsedTicks < nexttime)
        //            {
        //                if (ct.IsCancellationRequested) break;
        //            }
        //        }
        //    }

        //    internal int Step()
        //    {
        //        Task.Delay(1).Wait();
        //        _currentstep += 1;
        //        return _currentstep;
        //    }
        //}
    }
}
