using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

// Load deps
var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Debug", "net8.0");
Environment.CurrentDirectory = baseDir;
foreach (var dll in Directory.GetFiles(baseDir, "*.dll"))
{
    try { Assembly.LoadFrom(dll); } catch { }
}

var builderType = Type.GetType("EnterpriseApiAutomationFramework.Core.Builders.AddMultipleMemberByExcelBuilder, EnterpriseApiAutomationFramework");
var method = builderType!.GetMethod("BuildFromExcel");
var req = method!.Invoke(null, new object[] { "Sample_File_Member.xlsx", "Sheet1", "677920c1-831f-11ea-bd7d-04d9f5ab62dc", false, 2222222222L });
Console.WriteLine("Newtonsoft PascalCase:");
Console.WriteLine(JsonConvert.SerializeObject(req));
