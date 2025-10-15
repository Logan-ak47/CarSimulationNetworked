using UnityEngine;

namespace CarSim.Shared
{
    #region Message Structs

    public struct HelloC2S
    {
        public byte[] token; // 16 bytes fixed
        public ushort clientUdpPort;
        public string clientName;

        public const int TOKEN_SIZE = 16;
    }

    public struct WelcomeS2C
    {
        public uint sessionId;
        public byte simTickRate;
        public byte carId;
    }

    public struct InputC2S
    {
        public float steer;       // -1..1
        public float throttle;    // 0..1
        public float brake;       // 0..1
        public byte handbrake;    // 0/1
    }

    public struct StateS2C
    {
        public Vector3 position;
        public Quaternion rotation;
        public float speedKmh;
        public float rpm;
        public sbyte currentGear;
        public float steerAngle;
        public float wheelSlipFL;
        public float wheelSlipFR;
        public float wheelSlipRL;
        public float wheelSlipRR;
        public LightFlags lights;
        public IndicatorMode indicator;
        public CameraPartId cameraPart;
        public ushort lastProcessedInputSeq;
    }

    public struct SetGearC2S
    {
        public sbyte gear; // -1=R, 0=N, 1..6
    }

    public struct ToggleHeadlightsC2S
    {
        public byte on; // 0/1
    }

    public struct SetIndicatorC2S
    {
        public IndicatorMode mode;
    }

    public struct SetCameraFocusC2S
    {
        public CameraPartId partId;
    }

    public struct ServerNoticeS2C
    {
        public byte code;
        public string text;
    }

    #endregion

    #region Serializers

    public static class Protocol
    {
        public const int MAX_PACKET_SIZE = 1400;

        // HELLO_C2S
        public static int SerializeHello(byte[] buffer, ushort seq, HelloC2S msg)
        {
            // Write payload first to measure length
            int payloadOffset = ByteCodec.HEADER_SIZE;
            int offset = payloadOffset;
            ByteCodec.WriteFixedBytes(buffer, ref offset, msg.token, HelloC2S.TOKEN_SIZE);
            ByteCodec.WriteUShort(buffer, ref offset, msg.clientUdpPort);
            ByteCodec.WriteString(buffer, ref offset, msg.clientName);
            ushort payloadLength = (ushort)(offset - payloadOffset);

            // Write header with length
            offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.HELLO_C2S, seq, StopwatchTime.TimestampMs(), payloadLength);

            return ByteCodec.HEADER_SIZE + payloadLength;
        }

        public static HelloC2S DeserializeHello(byte[] buffer, int offset)
        {
            HelloC2S msg = new HelloC2S();
            msg.token = new byte[HelloC2S.TOKEN_SIZE];
            ByteCodec.ReadFixedBytes(buffer, ref offset, msg.token, HelloC2S.TOKEN_SIZE);
            msg.clientUdpPort = ByteCodec.ReadUShort(buffer, ref offset);
            msg.clientName = ByteCodec.ReadString(buffer, ref offset);
            return msg;
        }

        // WELCOME_S2C
        public static int SerializeWelcome(byte[] buffer, ushort seq, WelcomeS2C msg)
        {
            int payloadOffset = ByteCodec.HEADER_SIZE;
            int offset = payloadOffset;
            ByteCodec.WriteUInt(buffer, ref offset, msg.sessionId);
            ByteCodec.WriteByte(buffer, ref offset, msg.simTickRate);
            ByteCodec.WriteByte(buffer, ref offset, msg.carId);
            ushort payloadLength = (ushort)(offset - payloadOffset);

            offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.WELCOME_S2C, seq, StopwatchTime.TimestampMs(), payloadLength);

            return ByteCodec.HEADER_SIZE + payloadLength;
        }

        public static WelcomeS2C DeserializeWelcome(byte[] buffer, int offset)
        {
            WelcomeS2C msg = new WelcomeS2C();
            msg.sessionId = ByteCodec.ReadUInt(buffer, ref offset);
            msg.simTickRate = ByteCodec.ReadByte(buffer, ref offset);
            msg.carId = ByteCodec.ReadByte(buffer, ref offset);
            return msg;
        }

        // INPUT_C2S
        public static int SerializeInput(byte[] buffer, ushort seq, InputC2S msg)
        {
            int payloadOffset = ByteCodec.HEADER_SIZE;
            int offset = payloadOffset;
            ByteCodec.WriteFloat(buffer, ref offset, msg.steer);
            ByteCodec.WriteFloat(buffer, ref offset, msg.throttle);
            ByteCodec.WriteFloat(buffer, ref offset, msg.brake);
            ByteCodec.WriteByte(buffer, ref offset, msg.handbrake);
            ushort payloadLength = (ushort)(offset - payloadOffset);

            offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.INPUT_C2S, seq, StopwatchTime.TimestampMs(), payloadLength);

            return ByteCodec.HEADER_SIZE + payloadLength;
        }

        public static InputC2S DeserializeInput(byte[] buffer, int offset)
        {
            InputC2S msg = new InputC2S();
            msg.steer = ByteCodec.ReadFloat(buffer, ref offset);
            msg.throttle = ByteCodec.ReadFloat(buffer, ref offset);
            msg.brake = ByteCodec.ReadFloat(buffer, ref offset);
            msg.handbrake = ByteCodec.ReadByte(buffer, ref offset);
            return msg;
        }

        // STATE_S2C
        public static int SerializeState(byte[] buffer, ushort seq, StateS2C msg)
        {
            int payloadOffset = ByteCodec.HEADER_SIZE;
            int offset = payloadOffset;
            ByteCodec.WriteVector3(buffer, ref offset, msg.position);
            ByteCodec.WriteQuaternion(buffer, ref offset, msg.rotation);
            ByteCodec.WriteFloat(buffer, ref offset, msg.speedKmh);
            ByteCodec.WriteFloat(buffer, ref offset, msg.rpm);
            ByteCodec.WriteSByte(buffer, ref offset, msg.currentGear);
            ByteCodec.WriteFloat(buffer, ref offset, msg.steerAngle);
            ByteCodec.WriteFloat(buffer, ref offset, msg.wheelSlipFL);
            ByteCodec.WriteFloat(buffer, ref offset, msg.wheelSlipFR);
            ByteCodec.WriteFloat(buffer, ref offset, msg.wheelSlipRL);
            ByteCodec.WriteFloat(buffer, ref offset, msg.wheelSlipRR);
            ByteCodec.WriteByte(buffer, ref offset, (byte)msg.lights);
            ByteCodec.WriteByte(buffer, ref offset, (byte)msg.indicator);
            ByteCodec.WriteByte(buffer, ref offset, (byte)msg.cameraPart);
            ByteCodec.WriteUShort(buffer, ref offset, msg.lastProcessedInputSeq);
            ushort payloadLength = (ushort)(offset - payloadOffset);

            offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.STATE_S2C, seq, StopwatchTime.TimestampMs(), payloadLength);

            return ByteCodec.HEADER_SIZE + payloadLength;
        }

        public static StateS2C DeserializeState(byte[] buffer, int offset)
        {
            StateS2C msg = new StateS2C();
            msg.position = ByteCodec.ReadVector3(buffer, ref offset);
            msg.rotation = ByteCodec.ReadQuaternion(buffer, ref offset);
            msg.speedKmh = ByteCodec.ReadFloat(buffer, ref offset);
            msg.rpm = ByteCodec.ReadFloat(buffer, ref offset);
            msg.currentGear = ByteCodec.ReadSByte(buffer, ref offset);
            msg.steerAngle = ByteCodec.ReadFloat(buffer, ref offset);
            msg.wheelSlipFL = ByteCodec.ReadFloat(buffer, ref offset);
            msg.wheelSlipFR = ByteCodec.ReadFloat(buffer, ref offset);
            msg.wheelSlipRL = ByteCodec.ReadFloat(buffer, ref offset);
            msg.wheelSlipRR = ByteCodec.ReadFloat(buffer, ref offset);
            msg.lights = (LightFlags)ByteCodec.ReadByte(buffer, ref offset);
            msg.indicator = (IndicatorMode)ByteCodec.ReadByte(buffer, ref offset);
            msg.cameraPart = (CameraPartId)ByteCodec.ReadByte(buffer, ref offset);
            msg.lastProcessedInputSeq = ByteCodec.ReadUShort(buffer, ref offset);
            return msg;
        }

        // SET_GEAR_C2S
        public static int SerializeSetGear(byte[] buffer, ushort seq, SetGearC2S msg)
        {
            int payloadOffset = ByteCodec.HEADER_SIZE;
            int offset = payloadOffset;
            ByteCodec.WriteSByte(buffer, ref offset, msg.gear);
            ushort payloadLength = (ushort)(offset - payloadOffset);

            offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.SET_GEAR_C2S, seq, StopwatchTime.TimestampMs(), payloadLength);

            return ByteCodec.HEADER_SIZE + payloadLength;
        }

        public static SetGearC2S DeserializeSetGear(byte[] buffer, int offset)
        {
            SetGearC2S msg = new SetGearC2S();
            msg.gear = ByteCodec.ReadSByte(buffer, ref offset);
            return msg;
        }

        // TOGGLE_HEADLIGHTS_C2S
        public static int SerializeToggleHeadlights(byte[] buffer, ushort seq, ToggleHeadlightsC2S msg)
        {
            int payloadOffset = ByteCodec.HEADER_SIZE;
            int offset = payloadOffset;
            ByteCodec.WriteByte(buffer, ref offset, msg.on);
            ushort payloadLength = (ushort)(offset - payloadOffset);

            offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.TOGGLE_HEADLIGHTS_C2S, seq, StopwatchTime.TimestampMs(), payloadLength);

            return ByteCodec.HEADER_SIZE + payloadLength;
        }

        public static ToggleHeadlightsC2S DeserializeToggleHeadlights(byte[] buffer, int offset)
        {
            ToggleHeadlightsC2S msg = new ToggleHeadlightsC2S();
            msg.on = ByteCodec.ReadByte(buffer, ref offset);
            return msg;
        }

        // SET_INDICATOR_C2S
        public static int SerializeSetIndicator(byte[] buffer, ushort seq, SetIndicatorC2S msg)
        {
            int payloadOffset = ByteCodec.HEADER_SIZE;
            int offset = payloadOffset;
            ByteCodec.WriteByte(buffer, ref offset, (byte)msg.mode);
            ushort payloadLength = (ushort)(offset - payloadOffset);

            offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.SET_INDICATOR_C2S, seq, StopwatchTime.TimestampMs(), payloadLength);

            return ByteCodec.HEADER_SIZE + payloadLength;
        }

        public static SetIndicatorC2S DeserializeSetIndicator(byte[] buffer, int offset)
        {
            SetIndicatorC2S msg = new SetIndicatorC2S();
            msg.mode = (IndicatorMode)ByteCodec.ReadByte(buffer, ref offset);
            return msg;
        }

        // SET_CAMERA_FOCUS_C2S
        public static int SerializeSetCameraFocus(byte[] buffer, ushort seq, SetCameraFocusC2S msg)
        {
            int payloadOffset = ByteCodec.HEADER_SIZE;
            int offset = payloadOffset;
            ByteCodec.WriteByte(buffer, ref offset, (byte)msg.partId);
            ushort payloadLength = (ushort)(offset - payloadOffset);

            offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.SET_CAMERA_FOCUS_C2S, seq, StopwatchTime.TimestampMs(), payloadLength);

            return ByteCodec.HEADER_SIZE + payloadLength;
        }

        public static SetCameraFocusC2S DeserializeSetCameraFocus(byte[] buffer, int offset)
        {
            SetCameraFocusC2S msg = new SetCameraFocusC2S();
            msg.partId = (CameraPartId)ByteCodec.ReadByte(buffer, ref offset);
            return msg;
        }

        // RESET_CAR_C2S
        public static int SerializeResetCar(byte[] buffer, ushort seq)
        {
            int offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.RESET_CAR_C2S, seq, StopwatchTime.TimestampMs(), 0);
            return ByteCodec.HEADER_SIZE;
        }

        // SERVER_NOTICE_S2C
        public static int SerializeServerNotice(byte[] buffer, ushort seq, ServerNoticeS2C msg)
        {
            int payloadOffset = ByteCodec.HEADER_SIZE;
            int offset = payloadOffset;
            ByteCodec.WriteByte(buffer, ref offset, msg.code);
            ByteCodec.WriteString(buffer, ref offset, msg.text);
            ushort payloadLength = (ushort)(offset - payloadOffset);

            offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.SERVER_NOTICE_S2C, seq, StopwatchTime.TimestampMs(), payloadLength);

            return ByteCodec.HEADER_SIZE + payloadLength;
        }

        public static ServerNoticeS2C DeserializeServerNotice(byte[] buffer, int offset)
        {
            ServerNoticeS2C msg = new ServerNoticeS2C();
            msg.code = ByteCodec.ReadByte(buffer, ref offset);
            msg.text = ByteCodec.ReadString(buffer, ref offset);
            return msg;
        }
    }

    #endregion
}
