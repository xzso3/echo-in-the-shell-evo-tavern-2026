using System;
using System.IO;
using System.Linq;
using Echo.Tests.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace Echo.Editor.Contracts
{
    public static class ContractSmokeEntry
    {
        public static void Run()
        {
            int exit=1;
            try
            {
                string root=Directory.GetParent(Application.dataPath).FullName;
                string commit=Environment.GetEnvironmentVariable("ECHO_TESTED_COMMIT")??"working-tree";
                var report=ContractSmoke.Run(root,"Unity "+Application.unityVersion,commit);
                string path=Path.Combine(root,Environment.GetEnvironmentVariable("ECHO_EVIDENCE_DIR")??"Docs/Framework/Contracts/Evidence","unity-report.json");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,report.ToString(Formatting.Indented)+"\n");
                exit=((JArray)report["cases"]).Any(c=>(string)c["status"]!="passed")?1:0;
                Debug.Log("ECHO_CONTRACT_SMOKE exit="+exit+" report="+path);
            }
            catch(Exception error){Debug.LogException(error);}
            EditorApplication.Exit(exit);
        }
    }
}
