namespace Adafruit.IoT.Motors
{
    /// <summary>
    /// Specifies the stepper motor stepping style.
    /// </summary>
    /// <remarks>
    /// <para><see cref="Full"/>
    /// is sometimes called DOUBLE stepping; referring to two motor windings being energized (powered) at a time.
    /// It is the highest power usage and torque setting.
    /// This is the same as the odd numbered half steps.
    /// </para>
    /// <para><see cref="Wave"/>
    /// is sometimes called SINGLE stepping; referring to one motor winding being energized (powered) at a time.
    /// It is the lowest power usage and torque.
    /// This is the same as the even numbered half steps.
    /// </para>
    /// <para><see cref="Half"/>
    /// is sometimes called INTERLEAVE stepping; referring to alternating between <see cref="Full"/> and <see cref="Wave"/> steps.
    /// It alternates between higher power/torque and lower power/torque steps.
    /// </para>
    /// <para><see cref="Microstep8"/>
    /// is sometimes called 8-step MICROSTEP stepping.
    /// This style changes the current energizing a pair of windings using a sine-cosine power curve so that there are 8 steps per <see cref="Full"/> step.
    /// The motor torque is on average higher than <see cref="Wave"/> or <see cref="Half"/> stepping, but lower than <see cref="Full"/> stepping.
    /// </para>
    /// </remarks>
    public enum SteppingStyle : int
    {
        /// <summary>
        /// Motor will perform 4 full steps per step cycle.
        /// </summary>
        Full = 1,
        /// <summary>
        /// Motor will perform 4 wave steps per step cycle.
        /// </summary>
        Wave = 2,
        /// <summary>
        /// Motor will perform 8 half steps per step cycle.
        /// </summary>
        Half = 3,
        /// <summary>
        /// Motor will perform 8 sine-cosine micro steps per full step, or 32 micro steps per step cycle.
        /// </summary>
        Microstep8 = 4
    }
}
