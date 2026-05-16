using System;

namespace TatehamaATS_v1.Exceptions
{
    /// <summary>
    /// CH:TrainCrew状態取得異常
    /// </summary>
    internal class RelayGetStateException : ATSCommonException
    {
        /// <summary>
        /// CH:TrainCrew状態取得異常
        /// </summary>
        public RelayGetStateException(int place) : base(place)
        {
        }
        /// <summary>
        /// CH:TrainCrew状態取得異常
        /// </summary>
        public RelayGetStateException(int place, string message)
            : base(place, message)
        {
        }
        /// <summary>
        /// CH:TrainCrew状態取得異常
        /// </summary>
        public RelayGetStateException(int place, string message, Exception inner)
            : base(place, message, inner)
        {
        }
        public override string ToCode()
        {
            return Place.ToString() + "CH";
        }
        public override ResetConditions ResetCondition()
        {
            return ResetConditions.StopDetection_RelayReset;
        }
        public override OutputBrake ToBrake()
        {
            return OutputBrake.EB;
        }
    }
}
