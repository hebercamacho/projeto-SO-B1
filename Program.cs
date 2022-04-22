using System.Text;

public class Program
{
    public static void Main(string[] args)
    {
        new Program().Executar();
    }
    public void Executar()
    {
        var vmm = new VirtualMemoryManager(64, 256);
        byte offset = 255;
        var teste = vmm.TraduzirEndereco(0, offset);
        Console.WriteLine($"Endereco encontrado para {0}: {teste}");
        for (byte i = 0; i < 4; i++)
        {
            teste = vmm.TraduzirEndereco(i, offset);
            Console.WriteLine($"Endereco encontrado para {i}: {teste}");
            Console.WriteLine($"Escrito na memoria fisica: {i} - {Encoding.ASCII.GetString(vmm.physycalMemory, 0, offset)}");

        }
    }
}

public class VirtualMemoryManager
{
    public byte[] physycalMemory;
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
            var x = this.pageTable.pageList.ElementAt(pageNumber).get_frame();
            return x;
        }
        catch (InvalidBitException)
        {
            Console.WriteLine($"Page {pageNumber} is not in physical memory! Recovering from disk...");
            var y = TratarPageFault(pageNumber, pageOffset);
            return y;
        }
    }

    private int TratarPageFault(byte pageNumber, byte pageOffset)
    {
        string pageContent = ReadFromDisk("HardDrive.csv", pageNumber, pageOffset);
        if (!string.IsNullOrEmpty(pageContent))
        {
            var bytes = Encoding.ASCII.GetBytes(pageContent);

            int enderecoFisico = this.pageTable.PageReplace(pageNumber);
            int i = 0;
            foreach (byte b in bytes)
            {
                this.physycalMemory[enderecoFisico * pageOffset + i] = bytes[i];
                i++;
            }
            return enderecoFisico * pageOffset;
        }
        return -1;
    }

    private string ReadFromDisk(string fileName, byte pageNumber, byte pageOffset)
    {
        try
        {
            // Open the text file using a stream reader.
            using (var sr = new StreamReader(fileName))
            {
                while (sr.Peek() != -1)
                {
                    string? linha = sr.ReadLine();
                    if (!string.IsNullOrEmpty(linha))
                    {
                        Console.WriteLine($"Linha lida do HD: {linha}");
                        string[] elementos = linha.Split(';');
                        if (elementos[0] == pageNumber.ToString())
                            return elementos[1].PadLeft(pageOffset, '0');
                    }
                }
            }
            Console.WriteLine($"A pagina {pageNumber} nao se encontra no disco.");
            return string.Empty;
        }
        catch (IOException e)
        {
            Console.WriteLine("The file could not be read: ");
            Console.WriteLine(e.Message);
            throw e;
        }
    }
}

public class Page
{
    public bool validBit { get; private set; }
    public ulong order { get; set; }
    byte frame;

    public byte get_frame()
    {
        if (this.validBit)
            return this.frame;
        throw new InvalidBitException();
    }
    public void set_frame(byte value)
    {
        frame = value;
    }

    public Page(bool valid = false, ulong order = 0)
    {
        this.validBit = valid;
        this.order = order;
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

    public int PageReplace(byte page)
    {
        int replace_here;
        replace_here = this.pageList.FindIndex((pg) => pg.validBit == false);

        if (replace_here == -1)
        {
            // não achamos nenhum frame disponível! pegar o primeiro que entrou (FIFO)
            Page? item = this.pageList.MinBy((pg) => pg.order);
            if (item != null)
            {
                replace_here = this.pageList.IndexOf(item);
            }
        }
        if (replace_here == -1)
        {
            replace_here = 0; //shoudnt happen but just in case
        }
        //pegar ultima order pra incrementar;
        ulong nextOrder = this.pageList.Max((pg) => pg.order) + 1;
        this.pageList[replace_here] = new Page(true, nextOrder);
        this.pageList[replace_here].set_frame(page);
        return replace_here;
    }
}



public class InvalidBitException : Exception
{

}