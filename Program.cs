using System.Text;

public class Program
{
    public static int tamanhoPageMap = 256;
    public static byte tamanhoPageOffset = 255;
    public static int tamanhoMemoriaRAMemKb = 64;
    public static string nomeHardDrive = "HardDrive.csv";
    private static Random random = new Random();
    public static void Main(string[] args)
    {
        //realizar preenchimento do HD
        using (var sw = new StreamWriter(Program.nomeHardDrive))
        {
            for (var i = 0; i < Program.tamanhoPageMap; i++)
            {
                sw.WriteLine($"{i};{Program.RandomString(Program.tamanhoPageOffset)}");
            }
        }
        //Executar o VirtualMemoryManager
        new Program().Executar();
    }

    //função auxiliar para gerar strings aleatórias
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    //função para inicialiar a execução e testes do VirtualMemoryManager
    public void Executar()
    {
        var vmm = new VirtualMemoryManager(Program.tamanhoMemoriaRAMemKb, Program.tamanhoPageMap);
        byte offset = Program.tamanhoPageOffset;
        int teste;
        //teste onde ocorre todos os page fault
        for (int i = 0; i < Program.tamanhoPageMap; i++)
        {
            teste = vmm.TraduzirEndereco((byte)(i % 255), offset);
            Console.WriteLine($"Endereco encontrado para {i}: {teste}");
            if(teste != -1)
                Console.WriteLine($"Escrito na memoria fisica: {i} - {Encoding.ASCII.GetString(vmm.physycalMemory, teste, offset)}");

        }
        //teste sem page fault
        for (int i = 0; i < Program.tamanhoPageMap; i++)
        {
            teste = vmm.TraduzirEndereco((byte)(i % 255), offset);
            Console.WriteLine($"Endereco encontrado para {i}: {teste}");
            if(teste != -1)
                Console.WriteLine($"Escrito na memoria fisica: {i} - {Encoding.ASCII.GetString(vmm.physycalMemory, teste, offset)}");

        }
    }
}

public class VirtualMemoryManager
{
    public byte[] physycalMemory;
    PageTable pageTable;

    //construtor: recebe o tamanho da memoria fisica em kb e o numero de frames
    public VirtualMemoryManager(int size_kb, int frames) //64, 256
    {
        this.physycalMemory = new byte[size_kb * 1024];
        this.pageTable = new PageTable(frames); //256
    }

    //função que recebe o numero de pagina requerida e o page offset, e retorna o endereço fisico na memoria
    public int TraduzirEndereco(byte pageNumber, byte pageOffset)
    {
        try
        {
            //achar o endereco fisico gravado na page table, se estiver na memoria fisica
            return this.pageTable.pageList.ElementAt(pageNumber).get_frame();
        }
        catch (InvalidBitException)
        {
            //se nao estiver na memoria fisica, entra nesta exception e recupera do HD
            Console.WriteLine($"Page {pageNumber} nao esta na memoria fisica! Recuperando do HD...");
            return TratarPageFault(pageNumber, pageOffset);
        }
    }

    //função a ser chamada quando ocorrer page fault;
    //recebe o numero da pagina faltante e seu offset, 
    //e retorna o endereço fisico após inserir a pagina faltante na memoria fisica
    private int TratarPageFault(byte pageNumber, byte pageOffset)
    {
        //buscar no HD
        string pageContent = ReadFromDisk(Program.nomeHardDrive, pageNumber, pageOffset);
        if (!string.IsNullOrEmpty(pageContent))
        {
            //transformar a string do HD em array de bytes
            byte[] bytes = Encoding.ASCII.GetBytes(pageContent);

            //procurar primeiro espaço disponivel na memoria fisica com tamanho necessario
            byte enderecoFisico = 0;
            for (int i = 0; i < physycalMemory.Length; i++)
            {
                if (physycalMemory[i] == new byte())
                {
                    int TamanhoDoEspaco = 0;
                    for (int j = i; j < physycalMemory.Length && j < pageOffset; j++)
                    {
                        if (physycalMemory[j] == new byte())
                        {
                            TamanhoDoEspaco++;
                            if(TamanhoDoEspaco == pageOffset)
                            {
                                //chegando aqui, significa que o espaço iniciado em i é suficiente pra encaixar essa pagina
                                enderecoFisico = physycalMemory[i];
                                break;
                            }
                        }
                    }
                }
                if(enderecoFisico == 0) 
                {
                    //memoria fisica nao tem espaço, então vamos fazer um page replace
                    enderecoFisico = pageTable.PageReplace();
                }
                //inserir a pagina na memoria, byte a byte
                int k = 0;
                foreach (byte b in bytes)
                {
                    this.physycalMemory[enderecoFisico + k] = b;
                    k++;
                }
                break;
            }
            return enderecoFisico;
        }
        return -1;
    }

    private string ReadFromDisk(string fileName, int pageNumber, byte pageOffset)
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
                        // Console.WriteLine($"Linha lida do HD: {linha}");
                        string[] elementos = linha.Split(';');
                        if (elementos[0] == pageNumber.ToString())
                            return elementos[1].Substring(0, pageOffset);
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

    //função que retorna qual deve ser o endereço fisico da substituição a ser realizada 
    public byte PageReplace(byte address = default)
    {
        int replace_here;

        //encontra o primeiro invalidBit
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
        //FIXME isso aqui vai buscar coisa inexistente
        //recuperar o frame da page sendo substituida
        var frame = this.pageList[replace_here].get_frame();
        //inserir a pagina na pagetable substituindo uma antiga
        this.pageList[replace_here] = new Page(true, nextOrder);
        //se for passado com o que preencher, iremos preencher
        if(address != default)
            this.pageList[replace_here].set_frame(frame);
    
        return frame;
    }
}



public class InvalidBitException : Exception
{

}