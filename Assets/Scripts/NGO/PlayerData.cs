using Unity.Netcode;

/// <summary>
/// 一个序列化用于RPC调用的自定义类型,对NGO而言这代表了参与者玩家的状态,
/// 相关字段可以直接复制和修改.
/// </summary>
public class PlayerData : INetworkSerializable
{
    public string name;
    public ulong id;
    public int score;
    public PlayerData() { }//序列化明确需要一个默认构造函数
    public PlayerData(string name, ulong id, int score = 0)
    {
        this.name = name;
        this.id = id;
        this.score = score;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref score);
    }
}
