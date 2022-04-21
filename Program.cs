public class Program
{
    public static void Main(string[] args)
    {
        new Program().Executar();
    }
    public void Executar()
    {
        var vmm = new VirtualMemoryManager(64, 256);
        var teste = vmm.TraduzirEndereco(0, 0);
    }
}

public class VirtualMemoryManager
{
    byte[] physycalMemory;
    PageTable pageTable;

    public VirtualMemoryManager(int size_kb, int frames) //64, 256
    {
        this.physycalMemory = new byte[size_kb * 1024];
        this.pageTable = new PageTable(frames); //256
    }

    public int TraduzirEndereco(byte pageNumber, byte pageOffset)
    {        
        try
        {
            var x = pageTable.pageList.ElementAt(pageNumber).frame;
            return x;
        }
        catch (InvalidBitException)
        {
            Console.WriteLine($"Page {pageNumber} is not in physical memory! Recovering from disk...");
            var y = TratarPageFault();
            return y;
        }
    }

    public int TratarPageFault()
    {
        return -1;
    }
}

public class Page
{
    bool validBit {get; }
    public byte frame 
    {
        get
        {
            if(validBit) return frame;
            throw new InvalidBitException();
        } 
        set
        {
            frame = value;
        }
    }

    public Page()
    {
        this.validBit = false;
    }
}

public class PageTable
{
    public List<Page> pageList;
    public PageTable(int frames) //256 bytes
    {
        this.pageList = new List<Page>(frames);
        for (var i = 0; i < frames; i++)
        {
            this.pageList.Add(new Page());
        }
        Console.WriteLine($"Page table capacity: {this.pageList.Capacity}");
    }
}



public class InvalidBitException : Exception
{
    
}