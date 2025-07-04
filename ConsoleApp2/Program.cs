
using System.Collections.Frozen;

struct Numb
{
    public byte header;
    public int meter_id;
    public byte alert_vdrop;
    public int principal_consumption;
    public byte vbatt;
    public int[] _offsetConsumption;
    public int padding_bits;

    public struct PacketInfo
    {
        public string IdDataType;
        public int ConsumptionLpSize;
        public string ConsumptionSign;
        public int Other1Status;
        public int Other2Status;

        public PacketInfo()
        {
            IdDataType = null;
            ConsumptionLpSize = 0;
            ConsumptionSign = null;
            Other1Status = 0;
            Other2Status = 0;
        }
    }
    public PacketInfo packet_info;
    
}
class Numeric
{
    
    public Numeric(byte[] information)
    {
        Numb numb = new Numb();
        numb.header = information[0];
        numb.alert_vdrop  = information[1];
        numb.vbatt = information[2];
        byte packetInfoFirst = information[3];
        byte packetInfoSecond = information[4];
        string binaryHeader = Convert.ToString(packetInfoFirst, 2).PadLeft(8, '0');
        string binaryAlert = Convert.ToString(packetInfoSecond, 2).PadLeft(8, '0');
        int packetInfo = (packetInfoFirst << 8) | packetInfoSecond;
        int id_ascii_size = (packetInfo >> 10) & 0b1111;
        int idLength;
        Console.WriteLine(binaryHeader);
        Console.WriteLine(binaryAlert);
        numb.packet_info.IdDataType = binaryHeader.Substring(0, 1) switch
        {
            "0" => "numeric",
            "1" => "ASCII",
            _ => ""
        };
        numb.packet_info.ConsumptionSign = binaryHeader.Substring(1, 1) switch
        {
            "0" => "positive",
            "1" => "negative",
            _ => ""
        };

        if (numb.packet_info.IdDataType == "numeric")
        {
            idLength = 5;
            byte[] numericBytes = information.Skip(5).Take(idLength).ToArray();
            ulong numericId = 0;
            foreach (var b in numericBytes)
            {
                numericId = (numericId << 8) | b;
            }
            Console.WriteLine($"Numeric ID: {numericId}");
            
        }
        else
        {
            idLength = id_ascii_size + 1;

            string asciiId = System.Text.Encoding.ASCII.GetString(information, 5, idLength);
            Console.WriteLine($"ASCII ID: {asciiId}");
        }
    }
}

internal class Program
{
    public static void Main(string[] args)
    {
        byte[] data = new byte[]
        {
            0x2E, 0x13, 0xCD, 0x10,
            0x00, 0x00, 0x00, 0x06,
            0xDD, 0xF7, 0x00, 0x00,
            0x00, 0x00, 0x15, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF,
            0xF0
        };
        Numeric numeric = new Numeric(data);
    }
}