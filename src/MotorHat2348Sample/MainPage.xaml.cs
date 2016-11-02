using Adafruit.IoT.Devices;
using Adafruit.IoT.Motors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        public MainPage()
        {
            this.InitializeComponent();
            mh = new MotorHat2348(0x60);
            stepper = mh.CreateStepperMotor(1, 2, 200);
            stepper.SetSpeed(60);
            stepper.PowerFactor = 0.25;
            dcmotor = mh.CreateDCMotor(3);
        }

        MotorHat2348 mh;
        PwmStepperMotor stepper;
        PwmDCMotor dcmotor;

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = false;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await stepper.StepAsync(200, Direction.Forward, SteppingStyle.Microstep8);
            watch.Stop();
            System.Diagnostics.Debug.WriteLine("Duration in seconds = {0}", watch.Elapsed.TotalSeconds);
            textBlock.Text = string.Format("Actual speed = {0:F1} rpm", 60 / watch.Elapsed.TotalSeconds);
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
    }
}
