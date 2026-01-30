using System.Collections.Generic;
using ProtoBuf;

namespace IndustrialRevolution;

[ProtoContract]
public class PhysicsDebugData
{
    [ProtoMember(1)]
    public List<BodyDebugInfo> DynamicBodies { get; set; } = new();

    [ProtoMember(2)]
    public List<BodyDebugInfo> StaticBodies { get; set; } = new();
}

[ProtoContract]
public class BodyDebugInfo
{
    [ProtoMember(1)] public float PosX { get; set; }
    [ProtoMember(2)] public float PosY { get; set; }
    [ProtoMember(3)] public float PosZ { get; set; }
    [ProtoMember(4)] public float RotX { get; set; }
    [ProtoMember(5)] public float RotY { get; set; }
    [ProtoMember(6)] public float RotZ { get; set; }
    [ProtoMember(7)] public float RotW { get; set; } // w because quaternion

    [ProtoMember(8)] public float Height { get; set; }
    [ProtoMember(9)] public float Width { get; set; }
    [ProtoMember(10)] public float Length { get; set; }

    [ProtoMember(11)] public bool IsCompound { get; set; }
    [ProtoMember(12)] public List<CompoundChildDebug> Children { get; set; } = new();
}

[ProtoContract]
public class CompoundChildDebug
{
    [ProtoMember(1)] public float LocalX { get; set; }
    [ProtoMember(2)] public float LocalY { get; set; }
    [ProtoMember(3)] public float LocalZ { get; set; }
}
