using System.Diagnostics;

namespace PvZOL.Tools.ClientDecompiler;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var inputDir = args[0];
        var outputDir = "decompiled";
        
        foreach (var swfFilePath in Directory.EnumerateFiles(inputDir, "*.swf", SearchOption.TopDirectoryOnly))
        {
            var swfFileName = Path.GetFileNameWithoutExtension(swfFilePath);
            
            if (swfFileName == "Game_n") continue; // Game_p contains everything from Game_n, + starling backend
            if (swfFileName == "loader_n") continue; // use _p version only
            if (swfFileName == "ModuleTD") continue; // use _p version only

            await RunFFDec(swfFilePath, outputDir);
        }
    }
    
    private static async Task RunFFDec(string inputFile, string ffdecOutDir)
    {
        var proc = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = @"C:\Program Files (x86)\FFDec\ffdec-cli",
                Arguments = $"-onerror abort -export script {ffdecOutDir} {inputFile}"
            }
        };
        proc.Start();
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
        {
            throw new Exception("ffdec failed");
        }
    }
}