using System.CodeDom.Compiler;
using System.Diagnostics;
using PvZOL.CmdDumper;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var inputFile = args[0];

        var ffdecOutDir = Path.Combine("decompiled");
        Directory.CreateDirectory(ffdecOutDir);
        
        await RunFFDec(inputFile, ffdecOutDir);
        await GenerateFiles(ffdecOutDir);
    }

    private static async Task RunFFDec(string inputFile, string ffdecOutDir)
    {
        var proc = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = @"C:\Program Files (x86)\FFDec\ffdec-cli",
                Arguments = $"-selectclass PVZ.++ -onerror abort -export script {ffdecOutDir} {inputFile}"
            }
        };
        proc.Start();
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
        {
            throw new Exception("ffdec failed");
        }
    }
    
    private static async Task GenerateFiles(string ffdecOutDir)
    {
        var pvzDir = Path.Combine(ffdecOutDir, "scripts", "PVZ");

        var outputDir = Path.Combine("output");
        var typesOutputDir = Path.Combine(outputDir, "Types");
        var enumsOutputDir = Path.Combine(outputDir, "Enums");
        Directory.CreateDirectory(typesOutputDir);
        Directory.CreateDirectory(enumsOutputDir);

        var messageIDs = new Dictionary<string, string>();

        //var sourceFilePath = Path.Combine(pvzDir, "Cmd", "CmdCommon.as");
        foreach (var sourceFilePath in Directory.EnumerateFiles(pvzDir, "*.as", SearchOption.AllDirectories))
        {
            var sourceFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            if (sourceFileName == "CmdConst")
            {
                // nothing helpful
                continue;
            }
    
            var sourceFile = await File.ReadAllTextAsync(sourceFilePath);
            if (sourceFile.Contains("extends Message"))
            {
                var pbType = new ProtoType(sourceFileName);
                pbType.DecompileFields(sourceFile);

                var csSource = pbType.Emit();
                await File.WriteAllTextAsync(Path.Combine(typesOutputDir, $"{pbType.m_typeName}.cs"), csSource);

                if (pbType.m_messageID != null)
                {
                    messageIDs.Add(pbType.m_messageID, pbType.m_typeName);
                }
                continue;
            }
            
            // assume this is an enum...
            var pbEnum = new ProtoEnum(sourceFileName);
            pbEnum.DecompileFields(sourceFile);

            var enumCsSource = pbEnum.Emit();
            await File.WriteAllTextAsync(Path.Combine(enumsOutputDir, $"{pbEnum.m_typeName}.cs"), enumCsSource);
        }

        await File.WriteAllTextAsync(Path.Combine(outputDir, "CmdFactory.cs"), EmitCmdFactory(messageIDs));
    }

    private static string EmitCmdFactory(Dictionary<string, string> messageIDs)
    {
        var writer = new IndentedTextWriter(new StringWriter());
        writer.WriteLine("namespace PvZOL.Protocol.Cmd;");
        writer.WriteLine("");
        writer.WriteLine("public static class CmdFactory");
        writer.WriteLine("{");
        writer.Indent++;
        
        writer.WriteLine("public static object CreateMessage(string messageID)");
        writer.WriteLine("{");
        writer.Indent++;
        
        writer.WriteLine("return messageID switch");
        writer.WriteLine("{");
        writer.Indent++;
        foreach (var pair in messageIDs.OrderBy(x => x.Key))
        {
            writer.WriteLine($"\"{pair.Key}\" => new {pair.Value}(),");
        }
        writer.WriteLine("_ => throw new InvalidDataException($\"unknown message id: {messageID}\")");
        writer.Indent--;
        writer.WriteLine("};"); // switch
        
        writer.Indent--;
        writer.WriteLine("}"); // method
        
        writer.Indent--;
        writer.WriteLine("}"); // class

        return writer.InnerWriter.ToString()!;
    }
}


