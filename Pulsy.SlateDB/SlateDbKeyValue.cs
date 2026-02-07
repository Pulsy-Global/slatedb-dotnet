namespace Pulsy.SlateDB;

public record SlateDbKeyValue(byte[] Key, byte[] Value)
{
    public string KeyString => SlateDbConvert.FromBytes<string>(Key);
    public string ValueString => SlateDbConvert.FromBytes<string>(Value);
    public int ValueInt => SlateDbConvert.FromBytes<int>(Value);
    public long ValueLong => SlateDbConvert.FromBytes<long>(Value);
    public bool ValueBool => SlateDbConvert.FromBytes<bool>(Value);
    public double ValueDouble => SlateDbConvert.FromBytes<double>(Value);
}
