namespace Adafruit.IoT.Motors
{
    /// <summary>
    /// Specifies the motor direction.
    /// </summary>
    /// <remarks>
    /// The forward and reverse direction typically refers to clockwise and counter clockwise motor rotation.
    /// But individual implementations and motor wiring can alter this so that it means, for example, a 
    /// forward direction of the robot's wheels. So the precise meaning of forward and backward is implementation
    /// dependent.
    /// </remarks>
    public enum Direction : int
    {
        /// <summary>
        /// Motor will turn in the forward direction.
        /// </summary>
        Forward = 1,
        /// <summary>
        /// Motor will turn in the backward direction.
        /// </summary>
        Backward = 2,
        /// <summary>
        /// DC motor is 'off', not spinning but will also not hold its place
        /// </summary>
        Release = 3
    }
}
