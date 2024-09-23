using Unity.Netcode;

/// <summary>
/// һ�����л�����RPC���õ��Զ�������,��NGO����������˲�������ҵ�״̬,
/// ����ֶο���ֱ�Ӹ��ƺ��޸�.
/// </summary>
public class PlayerData : INetworkSerializable
{
    public string name;
    public ulong id;
    public int score;
    public PlayerData() { }//���л���ȷ��Ҫһ��Ĭ�Ϲ��캯��
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
