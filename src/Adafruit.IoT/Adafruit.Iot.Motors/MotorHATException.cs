using System;

namespace Adafruit.IoT.Devices
{
    internal class MotorHatException : Exception
    {
        public MotorHatException()
        {
        }

        public MotorHatException(string message) : base(message)
        {
        }

        public MotorHatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}