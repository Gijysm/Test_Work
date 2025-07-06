
using System.Collections.Frozen;
using System.Text;

struct Numb
{
    public byte header;
    public string meter_id;
    public byte AlertVDrop;
    public ulong PrincipalConsumption;
    public byte VBatt;
    public int[] OffsetConsumption; 
    public int PaddingBits;

    public struct PacketInfo
    {
        public bool IdDataType; 
        public int IdAsciiSize; 
        public bool ConsumptionSign; 
        public int ConsumptionLpSize; 
        public byte? Other1Status; 
        public byte? Other2Status; 
        public byte? Other3Status; 
    }

    public PacketInfo Packetinfo;
}

class Numeric
{

    public Numeric(byte[] information)
    {
        Numb numb = new Numb();

        if (information == null || information.Length < 15)
            throw new ArgumentException("Масив даних закороткий.");

        int offset = 0;

        numb.header = information[offset++];
        numb.AlertVDrop = information[offset++];
        numb.VBatt = information[offset++];
        ulong packet_info = (ushort)((information[offset++] << 8) | information[offset++]);
        numb.Packetinfo.IdDataType = (packet_info & 0x8000) != 0;
        numb.Packetinfo.ConsumptionSign = (packet_info & 0x4000) != 0;
        numb.Packetinfo.ConsumptionLpSize = (int)(packet_info >> 10) & 0xF;
        numb.Packetinfo.IdAsciiSize = (int)(packet_info >> 6) & 0xF;
        numb.Packetinfo.Other1Status = (byte)((packet_info >> 3) & 0x7);
        numb.Packetinfo.Other2Status = (byte)(packet_info & 0x7);
        if (numb.Packetinfo.ConsumptionLpSize < 4 || numb.Packetinfo.ConsumptionLpSize > 15)
            throw new ArgumentException($"Недійсне consumption_lp_size: {numb.Packetinfo.ConsumptionLpSize}");
        int Length = numb.Packetinfo.IdDataType ? numb.Packetinfo.IdAsciiSize + 1 : 5;
        if (numb.Packetinfo.IdDataType)
        {
            numb.meter_id = Encoding.ASCII.GetString(information, offset, Length);
            offset += Length;
        }
        else
        {
            ulong numericId = 0;
            for (int i = 0; i < 5; i++)
            {
                numericId = (numericId << 8) | information[offset++];

            }

            numb.meter_id = numericId.ToString();
        }

        if (offset + 5 > information.Length)
        {
            throw new ArgumentException("Недостатньо даних для principal_consumption.");
        }

        numb.PrincipalConsumption = 0;
        for (int i = 0; i < numb.Packetinfo.ConsumptionLpSize; i++)
        {
            numb.PrincipalConsumption = (numb.PrincipalConsumption << 8) | information[offset++];
        }

        byte[] other1 = null, other2 = null, other3 = null;
        if (numb.Packetinfo.Other1Status is > 0 and < 7)
        {
            other1 = new byte[numb.Packetinfo.Other1Status.Value];
            if (offset + other1.Length <= information.Length)
            {
                Array.Copy(information, offset, other1, 0, other1.Length);
            }

            offset += other1.Length;
        }        
        if (numb.Packetinfo.Other2Status is > 0 and < 7)
        {
            other2 = new byte[numb.Packetinfo.Other2Status.Value];
            if (offset + other2.Length <= information.Length)
            {
                Array.Copy(information, offset, other2, 0, other2.Length);
            }

            offset += other2.Length;
        }
        if (numb.Packetinfo.Other3Status is > 0 and < 7)
        {
            other3 = new byte[numb.Packetinfo.Other3Status.Value];
            if (offset + other3.Length <= information.Length)
            {
                Array.Copy(information, offset, other3, 0, other3.Length);
            }           
            
            offset += other3.Length;
        }

        numb.OffsetConsumption = new int[11]; 
        int bitRead = 0;
        int currentByte = offset < information.Length ? information[offset] : 0;
        int bitPosition = 7;
        for (int i = 0; i < 11; i++)
        {
            int value = 0;
            for (int j = 0; j < numb.Packetinfo.ConsumptionLpSize; j++)
            {
                if (bitPosition < 0)
                {
                    offset++;
                    bitPosition = 7;
                    currentByte = offset < information.Length ? information[offset] : 0;
                }
                value = (value << 1) | ((currentByte >> bitPosition) & 1);
                bitPosition--;
                bitRead++;
            }
            numb.OffsetConsumption[i] = numb.Packetinfo.ConsumptionSign ? -value : value;
        }

        if (bitRead % 8 != 0)
        {
            int remainingBits = 8 - (bitRead % 8);
            int padding = 0;
            for (int i = 0; i < remainingBits; i++)
            {
                if (bitPosition < 0)
                {
                    offset++;
                    bitPosition = 7;
                    currentByte = offset < information.Length ? information[offset] : 0; 
                }
                padding = (padding << 1) | ((currentByte >> bitPosition) & 1);
                bitPosition--;
            }
            numb.PaddingBits = padding;
        }
        
        Console.WriteLine($"Заголовок: 0x{numb.header:X2}");
        Console.WriteLine($"Alert VDrop: 0x{numb.AlertVDrop:X2}");
        Console.WriteLine($"VBatt: 0x{numb.VBatt:X2}");
        Console.WriteLine($"Тип ID: {(numb.Packetinfo.IdDataType ? "ASCII" : "numeric")}");
        Console.WriteLine($"Знак споживання: {(numb.Packetinfo.ConsumptionSign ? "negative" : "positive")}");
        Console.WriteLine($"Розмір LP споживання: {numb.Packetinfo.ConsumptionLpSize} бітів");
        Console.WriteLine($"Розмір ASCII ID: {numb.Packetinfo.IdAsciiSize}");
        Console.WriteLine($"ID лічильника: {numb.meter_id}");
        Console.WriteLine($"Основне споживання: {numb.PrincipalConsumption}");
        Console.WriteLine($"Статус Other1: {numb.Packetinfo.Other1Status ?? 0}");
        Console.WriteLine($"Статус Other2: {numb.Packetinfo.Other2Status ?? 0}");
        Console.WriteLine($"Статус Other3: {numb.Packetinfo.Other3Status ?? 0}");
        Console.WriteLine($"Зміщення: [{string.Join(", ", numb.OffsetConsumption)}]");
        Console.WriteLine($"Бітові заповнення: {numb.PaddingBits}");
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