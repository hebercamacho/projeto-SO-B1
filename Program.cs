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

    private int TratarPageFault()
    {
        ReadFromDisk("HardDrive.csv");
        return -1;
    }

    private void ReadFromDisk(string parameter)
    {
        try
        {
            // Open the text file using a stream reader.
            using (var sr = new StreamReader(parameter))
            {
                // Read the stream as a string, and write the string to the console.
                while(sr.Peek() != -1)
                {
                    string? linha = sr.ReadLine();
                    if(!string.IsNullOrEmpty(linha))
                    {
                        Console.WriteLine($"Linha lida do HD: {linha}");
                        string[] elementos = linha.Split(';');
                    }
                }
            }
        }
        catch (IOException e)
        {
            Console.WriteLine("The file could not be read: ");
            Console.WriteLine(e.Message);
        }
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