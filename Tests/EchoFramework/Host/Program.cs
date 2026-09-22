using System;
using System.IO;
using System.Linq;
using Echo.Tests.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
class Program
{
    static int Main(string[] args)
    {
        if(args.Length<2){Console.Error.WriteLine("Usage: <repo-root> <tested-commit> [--export]");return 2;}
        string root=Path.GetFullPath(args[0]);
        if(args.Contains("--export"))ContractSmoke.Export(root);
        var report=ContractSmoke.Run(root,"dotnet",args[1]);
        string path=Path.Combine(root,Environment.GetEnvironmentVariable("ECHO_EVIDENCE_DIR")??"Docs/Framework/Contracts/Evidence","dotnet-report.json");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,report.ToString(Formatting.Indented)+"\n");
        foreach(var c in (JArray)report["cases"])Console.WriteLine(c["case_id"]+": "+c["status"]+((string)c["status"]=="failed"?" "+c["actual"]:""));
        Console.WriteLine("Report: "+path);return ((JArray)report["cases"]).Any(c=>(string)c["status"]!="passed")?1:0;
    }
}
