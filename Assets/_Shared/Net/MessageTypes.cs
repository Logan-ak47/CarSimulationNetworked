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
        SERVER_NOTICE_S2C = 9,
        PING_C2S = 10,
        PONG_S2C = 11
    }

    public enum CameraPartId : byte
    {
        // General camera modes (0-9)
        FollowCamera = 0,
        HoodCamera = 1,
        OrbitCamera = 2,

        // Car part focus cameras (10+)
        FL_Wheel = 10,
        FR_Wheel = 11,
        RL_Wheel = 12,
        RR_Wheel = 13,
        Engine = 14,
        Exhaust = 15,
        SteeringLinkage = 16,
        BrakeCaliperFront = 17,
        SuspensionFront = 18,
        Dashboard = 19
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
