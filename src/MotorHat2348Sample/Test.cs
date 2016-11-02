using Adafruit.IoT.Devices;
using Adafruit.IoT.Motors;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Pwm;

namespace Adafruit.IoT
{
    internal class Examples
    {
        public async void StepperMotorExample()
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

            // Stops the stepper motor driver but keeps it energized (to hold position like a brake).
            stepper.Brake();

            // for demonstration purposes we will wait 2 seconds to observe the PWM and motor operation.
            await Task.Delay(2000);

            // Stops the stepper motor driver and de-energizes it.
            stepper.Stop();

            // Dispose of the MotorHat and free all its resources
            mh.Dispose();
        }

        public async void DCMotorExample()
        {
            var mh = new MotorHat2348(0x60);
            var motor = mh.CreateDCMotor(3);

            int incrementDelay = 50; // milliseconds
            double speedIncrement = 0.01;

            while (true)
            {
                motor.Run(Direction.Forward);

                Debug.WriteLine("Forward - Speed Up!");
                for (double i = 0; i < 1; i += speedIncrement)
                {
                    motor.SetSpeed(i);
                    await Task.Delay(incrementDelay);
                }
                Debug.WriteLine("Forward - Slow Down!");
                for (double i = 1; i > 0; i -= speedIncrement)
                {
                    motor.SetSpeed(i);
                    await Task.Delay(incrementDelay);
                }

                motor.Run(Direction.Backward);

                Debug.WriteLine("Backward - Speed Up!");
                for (double i = 0; i < 1; i += speedIncrement)
                {
                    motor.SetSpeed(i);
                    await Task.Delay(incrementDelay);
                }
                Debug.WriteLine("Backward - Slow Down!");
                for (double i = 1; i > 0; i -= speedIncrement)
                {
                    motor.SetSpeed(i);
                    await Task.Delay(incrementDelay);
                }
                motor.Stop();
                Debug.WriteLine("repeat!");
            }
        }
    }
}
