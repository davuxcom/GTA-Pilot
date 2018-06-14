namespace GTAPilot
{
    class FlightDataComputer
    {
        private ModeControlPanel _mcp;
        private FlightController _control;

        public FlightDataComputer(ModeControlPanel mcp, FlightController control)
        {
            _mcp = mcp;
            _control = control;
        }

        internal void OnRollDataSampled(int id)
        {
            if (_mcp.BankHold)
            {
                // TODO: respond
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
                // TODO
            }
        }

        internal void OnSpeedDataSampled(int id)
        {
            // TODO
        }

        internal void OnAltidudeDataSampled(int id)
        {
            // TODO
        }

        internal void OnCompassDataSampled(int id)
        {
            if (_mcp.HeadingHold)
            {
                // TODO(2): in tandem with bank
            }
        }
    }
}
