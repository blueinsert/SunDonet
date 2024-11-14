using NVelocity;
using NVelocity.App;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Protogen
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("args.count={0}", args.Count());  
            if (args.Count() < 4)
            {
                Console.WriteLine("Usage: Protogen [ProtoDir] [AotuGenDir] [ProtoCollectionName] [ProtoNameSpace]");
                return 1;
            }
            Console.WriteLine("Input: {0} {1} {2} {3}", args[0], args[1], args[2], args[3]);
            try
            {
                var protoDir = args[0];
                var autoGenDir = args[1];
                var protoCollectionName = args[2];
                var ptotoNamespace = args[3];
                if(Directory.Exists(autoGenDir))
                    Directory.Delete(autoGenDir,true);
                //1.调用google protoc产生协议类
                if (!AutoGenProtocolFiles(protoDir, autoGenDir))
                {
                    Console.WriteLine("Protocol cs gen error");
                    return 1;
                }
                //2. 产生协议字典类
                List<string> codeFileList = new List<string>();
                foreach (var file in Directory.GetFiles(autoGenDir, "*.cs", SearchOption.AllDirectories))
                {
                    codeFileList.Add(file);
                }
                var result = GetCompilerAssembly(codeFileList);
                var assembly = result.CompiledAssembly;
                if (assembly == null)
                {
                    Console.WriteLine(string.Format("Load protocol assembly fail"));
                    return 1;
                }
                if (!AutoGenProtocolDictionary(autoGenDir, assembly, ptotoNamespace, protoCollectionName))
                {
                    Console.WriteLine(string.Format("AutoGenProtocolDictionary fail"));
                    return 1;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Error: {0} \n callBack={1}", ex.Message, ex.StackTrace);
                return 1;
            }
            Console.WriteLine("Protogen success!");
            Console.WriteLine("Press any Key");
            //Console.ReadKey();
            return 0;
        }

        static bool AutoGenProtocolDictionary(string outpath, Assembly assembly, string protocolNameSpace, String protocolCollectionName)
        {
            List<Type> proTypeList = new List<Type>();
            foreach (var type in assembly.GetExportedTypes())
            {
                if (/*type.FullName.Contains(protocolNameSpace) &&*/
                    (type.Name.EndsWith("Req", StringComparison.OrdinalIgnoreCase) || type.Name.EndsWith("Ack", StringComparison.OrdinalIgnoreCase) || type.Name.EndsWith("Ntf", StringComparison.OrdinalIgnoreCase)))  // 只接收Ack、ntf和req结束的协议定义， 其余的相当于作为内部使用的类
                {
                    proTypeList.Add(type);
                }
            }
            int maxId = 1;
            List<KeyValuePair<string, int>> idMapingList = new List<KeyValuePair<string, int>>();
            foreach (var type in proTypeList)
            {
                var pair = idMapingList.Find((p) => { return p.Key == type.Name; });
                if (string.IsNullOrEmpty(pair.Key))
                {
                    idMapingList.Add(new KeyValuePair<string, int>(type.Name, maxId++));
                }
            }
            if (!Directory.Exists("./Template"))
            {
                Console.WriteLine("Template dir not exist!");
                return false;
            }
            // 为所有的 模板 进行执行,并输出结果
            Velocity.Init();
            VelocityContext ctx = new VelocityContext();
            ctx.Put("idMapingInfo", idMapingList);
            ctx.Put("namespace", protocolNameSpace);
            ctx.Put("name", protocolCollectionName);
            foreach (var file in Directory.GetFiles("./Template", "*.vm", SearchOption.AllDirectories))
            {
                try
                {
                    StringWriter resultWriter = new StringWriter();
                    Velocity.Evaluate(ctx, resultWriter, "", File.ReadAllText(file, UTF8Encoding.UTF8));
                    string filename = Path.GetFileNameWithoutExtension(file);
                    string outputFileName = string.Format("{0}\\{1}{2}.cs", outpath, protocolCollectionName, filename);
                    File.WriteAllText(outputFileName, resultWriter.ToString(), UTF8Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Message = {0}", ex.Message);
                }
            }
            return true;
        }

        protected static CompilerResults GetCompilerAssembly(List<String> codeFiles)
        {
            if (codeFiles == null || codeFiles.Count == 0)
                return null;

            //开始编译代码文件
            var complier = CodeDomProvider.CreateProvider("CSharp");
            CompilerParameters param = new CompilerParameters();
            param.GenerateExecutable = false;
            param.GenerateInMemory = true;
            param.TreatWarningsAsErrors = true;
            param.IncludeDebugInformation = false;
            param.ReferencedAssemblies.Add("./Google.Protobuf.dll");//Google.Protobuf.dll  protobuf-net
            param.ReferencedAssemblies.Add("System.dll");
            param.ReferencedAssemblies.Add("System.Core.dll");
            //将代码文件中的代码读出来
            String[] codes = new String[codeFiles.Count];
            for (Int32 i = 0; i < codeFiles.Count; ++i)
            {
                codes[i] = File.ReadAllText(codeFiles[i], Encoding.UTF8);
            }

            var clientResult = complier.CompileAssemblyFromSource(param, codes);
            if (clientResult.Errors.HasErrors)
            {
                StringBuilder errorString = new StringBuilder(String.Empty);
                foreach (var err in clientResult.Errors)
                {
                    errorString.Append(err.ToString());
                    errorString.Append(Environment.NewLine);
                }
                Console.WriteLine("GetCompilerAssembly failed," + errorString);
            }

            return clientResult;
        }

        static bool AutoGenProtocolFiles(string dirPath, string autoGenPath)
        {
            if (!Directory.Exists(dirPath))
                return false;
            if (!Directory.Exists(autoGenPath))
            {
                Directory.CreateDirectory(autoGenPath);
            }
            Console.WriteLine("EnvDir:" + Environment.CurrentDirectory);
            try
            {
                // 在编译proto文件
                foreach (var file in Directory.GetFiles(dirPath, "*.proto", SearchOption.AllDirectories))
                {
                    string filename = Path.GetFileName(file);

                    string command = String.Format("protoc.exe {0} --csharp_out={1} --proto_path={2}", filename, autoGenPath, dirPath);

                    bool bError = false;
                    var errCode = "";
                    Console.WriteLine(command);
                    var ts = Task.Run(() =>
                    {
                        errCode = ExecuteCmd("cmd.exe", "/C" + command, 0, out bError);
                        Console.WriteLine(errCode);
                    });
                    ts.Wait();

                    // 如果发生错误的话，则直接结束
                    if (bError && !errCode.Contains("but not used"))
                    {
                        return false;
                    }
                }
            }
            finally
            {

            }
            return true;
        }

        /// <summary>  
        /// 执行DOS命令，返回DOS命令的输出  
        /// </summary>  
        /// <param name="dosCommand">dos命令</param>  
        /// <param name="milliseconds">等待命令执行的时间（单位：毫秒），  
        /// 如果设定为0，则无限等待</param>  
        /// <param name="bError">是否输出了错误流</param>  
        /// <returns>返回DOS命令的输出</returns>  
        public static string ExecuteCmd(string runCom, string command, int seconds, out bool bError)
        {
            bError = false;
            string outputAndError = ""; //输出字符串  
            if (command != null && !command.Equals(""))
            {
                Process process = new Process();//创建进程对象  
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = runCom;//设定需要执行的命令  
                startInfo.Arguments = command;//“/C”表示执行完命令后马上退出  
                startInfo.UseShellExecute = false;//不使用系统外壳程序启动  
                startInfo.RedirectStandardInput = false;//不重定向输入  
                startInfo.RedirectStandardOutput = true; //重定向输出  
                startInfo.RedirectStandardError = true; //重定向输出  
                startInfo.CreateNoWindow = true;//不创建窗口  
                process.StartInfo = startInfo;
                try
                {
                    if (process.Start())//开始进程  
                    {
                        outputAndError = process.StandardOutput.ReadToEnd(); //读取进程的输出  
                        //process.BeginOutputReadLine();
                        var errorMsg = process.StandardError.ReadToEndAsync().Result;
                        if (errorMsg.Length != 0)
                        {
                            outputAndError += errorMsg; //读取进程的输出  
                            bError = true;
                        }

                        if (seconds == 0)
                        {
                            process.WaitForExit();//这里无限等待进程结束  
                        }
                        else
                        {
                            process.WaitForExit(seconds); //等待进程结束，等待时间为指定的毫秒  
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
                finally
                {
                    if (process != null)
                    {
                        process.Close();
                    }
                }
            }
            return outputAndError;
        }
    }
}
