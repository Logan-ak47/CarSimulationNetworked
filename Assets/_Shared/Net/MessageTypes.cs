namespace CarSim.Shared
{
    public enum MsgType : byte
    {
        HELLO_C2S = 0,
        WELCOME_S2C = 1,
        INPUT_C2S = 2,
        STATE_S2C = 3,
        SET_GEAR_C2S = 4,
        TOGGLE_HEADLIGHTS_C2S = 5,
        SET_INDICATOR_C2S = 6,
        SET_CAMERA_FOCUS_C2S = 7,
        RESET_CAR_C2S = 8,
        SERVER_NOTICE_S2C = 9
    }

    public enum CameraPartId : byte
    {
        FL_Wheel = 0,
        FR_Wheel = 1,
        RL_Wheel = 2,
        RR_Wheel = 3,
        Engine = 4,
        Exhaust = 5,
        SteeringLinkage = 6,
        BrakeCaliperFront = 7,
        SuspensionFront = 8,
        Dashboard = 9
    }

    public enum IndicatorMode : byte
    {
        Off = 0,
        Left = 1,
        Right = 2,
        Hazard = 3
    }

    [System.Flags]
    public enum LightFlags : byte
    {
        None = 0,
        Headlight = 1
    }
}
