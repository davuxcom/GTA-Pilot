using System;
using System.Diagnostics;

namespace GTAPilot
{
    class FlightDataComputer
    {
        private ModeControlPanel _mcp;
        private FlightController _control;

        double DesiredPitch;
        double DesiredRoll;
        double DesiredHeading;
        double DesiredSpeed;
        double DesiredThrottle;

        public PID _pitch_pid;
        public PID _roll_pid;
        public PID _airspeed_pid;
        public PID _heading_pid;

        public FlightDataComputer(ModeControlPanel mcp, FlightController control)
        {
            _mcp = mcp;
            _control = control;

            _mcp.PropertyChanged += MCP_PropertyChanged;

            // -100 - 100
            _roll_pid = new PID(TUNE.Roll.P, TUNE.Roll.I, TUNE.Roll.D,
                200, 0,
                UInt16.MaxValue - 1, 0,
                () => Timeline.Roll + 100,
                () => double.IsNaN(DesiredRoll) ? 100 : DesiredRoll + 100,
                (v) => Handle_Roll(v));

            // -50, 50
            _pitch_pid = new PID(TUNE.Pitch.P, TUNE.Pitch.I, TUNE.Pitch.D,
                100, 0,
                UInt16.MaxValue - 1, 0,
                () => Timeline.Pitch + 50,
                () => double.IsNaN(DesiredPitch) ? 50 : DesiredPitch + 50,
                Handle_Pitch);

            _heading_pid = new PID(TUNE.Heading.P, TUNE.Heading.I, TUNE.Heading.D,
                (360 * 2), 0,
                100, 0,
                Handle_Get_Compass,
                () => 360,
                Handle_Compass);

            // 0-175
            _airspeed_pid = new PID(TUNE.Speed.P, TUNE.Speed.I, TUNE.Speed.D,
                175, 0,
                255, 0,
                () => Timeline.Speed,
                () => double.IsNaN(DesiredSpeed) ? 0 : DesiredSpeed,
                Handle_Throttle);
        }

        void Handle_Roll(double power)
        {
            power -= short.MaxValue;

            if (double.IsNaN(power)) return;

            var pp = Math.Round((double)((power) / (short.MaxValue)) * 100, 2);

            power = RemoveDeadZone(power, TUNE.Roll_DeadZone, TUNE.Roll_Max);
            power = GetScaledValue(power, TUNE.RollScale);

            if (!double.IsNaN(DesiredRoll))
            {
                _control.SetRoll(power);


              //  _panel.Roll.OutputValues.Add(new Indicator.IndicatorValueData { Tick = _panel.Ticks, Value = pp });
              //  _panel.Roll.SetpointValues.Add(new Indicator.IndicatorValueData { Tick = _panel.Ticks, Value = DesiredRoll });
            }
        }

        void Handle_Pitch(double power)
        {
            power -= short.MaxValue;
            power *= -1;

            var rollAngle = Math.Abs(Timeline.Roll);
            if (rollAngle > 18)
            {
                // var add = rollAngle * TUNE.RollTrim;

                // Trace.WriteLine("Power change; " + add);
                power += TUNE.RollTrim;
            }

            /*
            double max = short.MaxValue;
            bool isAggressive = _panel.Computer.Nav.CurrentMode == FlightNavigator.FlightMode.Takeoff_Rotate ||
                    _panel.Computer.Nav.CurrentMode == FlightNavigator.FlightMode.Landing_Final;
            if (!isAggressive)
            {
                max = TUNE.Pitch_Max;
            }
            */
            double max = TUNE.Pitch_Max;
            var pp = Math.Round((double)((power) / (short.MaxValue)) * 100, 2);

            power = RemoveDeadZone(power, TUNE.Pitch_DeadZone, max);
            power = GetScaledValue(power, TUNE.PitchScale);



            if (!double.IsNaN(DesiredPitch))
            {
             //   _panel.Pitch.OutputValues.Add(new Indicator.IndicatorValueData { Tick = _panel.Ticks, Value = pp });
             //   _panel.Pitch.SetpointValues.Add(new Indicator.IndicatorValueData { Tick = _panel.Ticks, Value = DesiredPitch });
            }

            if (!double.IsNaN(DesiredPitch))
            {
                _control.SetPitch(power);
            }
        }


        double Handle_Get_Compass()
        {

            var val = Math.Round(Timeline.Heading);
            var target = DesiredHeading;

            if (val == target) return 360;

            double right_turn = 0;
            double left_turn = 0;

            var a = val;
            var b = target;
            var diff = ((a - b + 180 + 360) % 360) - 180;

            if (diff < 0)
            {
                right_turn = diff;
                left_turn = 360 - diff;
            }
            else
            {
                left_turn = diff;
                right_turn = 360 - diff;
            }

            double ret_normal = 0;
            double ret = 0;
            if (right_turn < left_turn)
            {
                ret_normal = Math.Abs(right_turn);
                ret = 360 + Math.Abs(right_turn);
            }
            else
            {
                ret_normal = -1 * Math.Abs(left_turn);
                ret = 360 - Math.Abs(left_turn);
            }

            // if (!double.IsNaN(DesiredHeading))
            {
               // _panel.Compass.InputValues.Add(new Indicator.IndicatorValueData { Tick = _panel.Ticks, Value = ret_normal });
            }

            if (double.IsNaN(DesiredHeading))
            {
                return 360;
            }

            return (int)Math.Round(ret);
        }

        void Handle_Compass(double power)
        {
            var v = Math.Round(power);
            double vx = 0;

            if (v > 50)
            {
                v -= 50;
                vx = v;
                /*
                if (v < 8)
                {
                    v = 1;
                    vx = 1;
                }
                else v -= 10;

                if (v > 20) v = 20;
                */

                if (!double.IsNaN(DesiredHeading))
                {
                    _control.SetLeftRudder(v);
                }
            }
            else if (v < 50)
            {
                v = 50 - v;
                vx = v * -1;

                /*
                if (v < 8)
                {
                    v = 1;
                    vx = -1;
                }
                else v -= 10;

                if (v > 20) v = 20;
                */

                if (!double.IsNaN(DesiredHeading))
                {
                    _control.SetRightRudder(v);
                }
            }

            if (!double.IsNaN(DesiredHeading))
            {
               // _panel.Compass.OutputValues.Add(new Indicator.IndicatorValueData { Tick = _panel.Ticks, Value = vx });
              //  _panel.Compass.SetpointValues.Add(new Indicator.IndicatorValueData { Tick = _panel.Ticks, Value = 0 });
            }
        }

        double lastThrottle = 0;

        public void Handle_Throttle(double throttle)
        {
            double pp = 0;
            if (!double.IsNaN(DesiredSpeed) || !double.IsNaN(DesiredThrottle))
            {
                if (!double.IsNaN(DesiredThrottle))
                {
                    throttle = DesiredThrottle;
                    lastThrottle = DesiredThrottle;
                }
                /*
                if (Math.Abs(throttle - lastThrottle) > 5)
                {
                    if (throttle > lastThrottle)
                    {
                        throttle = lastThrottle + 5;
                    }
                    else throttle = lastThrottle - 5;
                }
                */
                lastThrottle = throttle;

                _control.SetThrottle(throttle);

                pp = Math.Round((double)(throttle / 255) * 100);
            }

           // _panel.Speed.OutputValues.Add(new Indicator.IndicatorValueData { Tick = _panel.Ticks, Value = pp });
          //  _panel.Speed.SetpointValues.Add(new Indicator.IndicatorValueData { Tick = _panel.Ticks, Value = DesiredSpeed });

        //    ActualThrottle = pp;
        }

        double RemoveDeadZone(double power, double deadzone, double max)
        {
            if (power > 0)
            {
                power = Math2.MapValue(0, short.MaxValue, deadzone, max, power);
            }
            else if (power < 0)
            {
                power = Math2.MapValue(-1 * short.MaxValue, 0, -1 * max, -1 * deadzone, power);
            }
            return power;
        }

        double GetScaledValue(double value, double scaleValue)
        {

            if (value > 0)
            {
                value = GetScaledValue(value, short.MaxValue, scaleValue);
            }
            else if (value < 0)
            {
                value = -1 * GetScaledValue(Math.Abs(value), short.MaxValue, scaleValue);
            }
            return value;
        }

        double GetScaledValue(double value, double scale, double pow)
        {
            /*
if (power > 0)
{
    power = GetScaledValue(power, 32000, TUNE.PitchScale);
}
else if (power < 0)
{
    power = -1 * GetScaledValue(Math.Abs(power), 32000, TUNE.PitchScale);
}
*/

            return Math.Pow(value / scale, pow) * scale;
        }

        private void MCP_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_mcp.PitchHold):
                    DesiredPitch = Timeline.Pitch;
                    Trace.WriteLine($"A/P: Pitch: {Timeline.Pitch}");
                    break;
                case nameof(_mcp.BankHold):
                    DesiredRoll = Timeline.Roll;
                    Trace.WriteLine($"A/P: Roll: {Timeline.Roll}");
                    break;
                case nameof(_mcp.HeadingHold):
                    DesiredHeading = Timeline.Heading;
                    Trace.WriteLine($"A/P: Heading: {Timeline.Heading}");
                    break;
                case nameof(_mcp.SpeedHold):
                    DesiredSpeed = Timeline.Speed;
                    Trace.WriteLine($"A/P: Speed: {Timeline.Speed}");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        internal void OnRollDataSampled(int id)
        {
            if (_mcp.BankHold)
            {
                _roll_pid.Compute();
            }
            else if (_mcp.HeadingHold)
            {
                // TODO(2): respond in tandem with rudder
            }
        }

        internal void OnPitchDataSampled(int id)
        {
            if (_mcp.PitchHold)
            {
                _pitch_pid.Compute();
            }
        }

        internal void OnSpeedDataSampled(int id)
        {
            if (_mcp.SpeedHold)
            {
                _airspeed_pid.Compute();
            }
        }

        internal void OnAltidudeDataSampled(int id)
        {
        }

        internal void OnCompassDataSampled(int id)
        {
            if (_mcp.HeadingHold)
            {
                _heading_pid.Compute();
                // TODO(2): in tandem with bank
            }
        }
    }
}
