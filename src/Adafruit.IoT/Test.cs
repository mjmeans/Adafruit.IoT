using Adafruit.IoT.Devices;
using Adafruit.IoT.Motors;
using System;
using System.Threading.Tasks;
using Windows.Devices.Pwm;

namespace Adafruit.IoT
{
    internal class Examples
    {
        public async void Example1()
        {
            MotorHat2348 mh = null;
            PwmStepperMotor stepper = null;
            PwmPin pwm = null;

            if (mh == null)
            {
                // Create a driver object for the HAT at address 0x60
                mh = new MotorHat2348(0x60);
                // Create a stepper motor object at the specified ports and steps per rev
                stepper = mh.CreateStepperMotor(1, 2, 200);
                // Create a PwmPin object at one of the auxiliary PWMs on the HAT
                pwm = mh.CreatePwm(1);
            }

            // step 200 full steps in the forward direction using half stepping (so 400 steps total) at 30 rpm
            stepper.SetSpeed(30);
            await stepper.StepAsync(200, Direction.Forward, SteppingStyle.Half);

            // Activate the pin and set it to 50% duty cycle
            pwm.Start();
            pwm.SetActiveDutyCyclePercentage(0.5);

            // for demonstration purposes we will wait 10 seconds to observe the PWM and motor operation.
            await Task.Delay(10000);

            // Stop the auxiliary PWM pin
            pwm.Stop();

            // Dispose of the MotorHat and free all its resources
            mh.Dispose();
        }
    }
}
