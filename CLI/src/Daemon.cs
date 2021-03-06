﻿using System;
using System.IO.Pipes; // NamedPipeServerStream, PipeDirection
using Mono.Options;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TypeCobol.Compiler;
using TypeCobol.Compiler.CodeModel;
using TypeCobol.Compiler.Diagnostics;
using TypeCobol.Compiler.Text;
using SimpleMsgPack;
using TypeCobol.Server.Serialization;

namespace TypeCobol.Server {

    /// <summary>
    /// Config class that holds all the argument information like input files, output files, error file etc.
    /// </summary>
    public class Config
    {
        public TypeCobol.Compiler.DocumentFormat Format = TypeCobol.Compiler.DocumentFormat.RDZReferenceFormat;
        public bool AutoRemarks;
        public string HaltOnMissingCopyFilePath;
        public List<string> CopyFolders = new List<string>();
        public List<string> InputFiles = new List<string>();
        public List<string> OutputFiles = new List<string>();
        public ExecutionStep ExecToStep = ExecutionStep.Generate; //Default value is Generate
        public string ErrorFile = null;
        public string skeletonPath = "";
        public bool IsErrorXML
        {
            get { return ErrorFile != null && ErrorFile.ToLower().EndsWith(".xml"); }
        }
        public List<string> Copies = new List<string>();
        public List<string> Dependencies = new List<string>();

        public string EncFormat = null;
    }

    class Server {

        enum StartClient {
            No, HiddenWindow, NormalWindow
        }
        static int Main(string[] argv) {
			bool help = false;
			bool version = false;
			bool once = false;
            StartClient startClient = StartClient.No;
			var config = new Config();
			var pipename = "TypeCobol.Server";

            var p = new OptionSet() {
                "USAGE",
                "  "+PROGNAME+" [OPTIONS]... [PIPENAME]",
                "",
                "VERSION:",
                "  "+PROGVERSION,
                "",
                "DESCRIPTION:",
                "  Run the TypeCobol parser server.",
                { "k|startServer:", "Start the server if not already started, and executes commandline.\n" +
                                    "By default the server is started in window mode\n" +
                                    "'{hidden}' hide the window.", v =>
                {
                    if ("hidden".Equals(v, StringComparison.InvariantCultureIgnoreCase)) {
                        startClient = StartClient.HiddenWindow;
                    } else {
                        startClient = StartClient.NormalWindow;
                    }
                }  },
                { "1|once",  "Parse one set of files and exit. If present, this option does NOT launch the server.", v => once = (v!=null) },
                { "i|input=", "{PATH} to an input file to parse. This option can be specified more than once.", v => config.InputFiles.Add(v) },
                { "o|output=","{PATH} to an ouput file where to generate code. This option can be specified more than once.", v => config.OutputFiles.Add(v) },
                { "d|diagnostics=", "{PATH} to the error diagnostics file.", v => config.ErrorFile = v },
                { "s|skeletons=", "{PATH} to the skeletons files.", v => config.skeletonPath = v },
                { "a|autoremarks", "Enable automatic remarks creation while parsing and generating Cobol", v => config.AutoRemarks = true },
                { "hc|haltonmissingcopy=", "HaltOnMissingCopy will generate a file to list all the absent copies", v => config.HaltOnMissingCopyFilePath = v },
                { "ets|exectostep=", "ExecToStep will execute TypeCobol Compiler until the included given step (Scanner/0, Preprocessor/1, SyntaxCheck/2, SemanticCheck/3, Generate/4)", v => Enum.TryParse(v.ToString(), true, out config.ExecToStep) },
//				{ "p|pipename=",  "{NAME} of the communication pipe to use. Default: "+pipename+".", (string v) => pipename = v },
				{ "e|encoding=", "{ENCODING} of the file(s) to parse. It can be one of \"rdz\"(this is the default), \"zos\", or \"utf8\". "
                                +"If this option is not present, the parser will attempt to guess the {ENCODING} automatically.",
                                v => config.Format = CLI.CreateFormat(v, ref config)
                },
                { "y|intrinsic=", "{PATH} to intrinsic definitions to load.\nThis option can be specified more than once.", v => config.Copies.Add(v) },
                { "c|copies=",  "Folder where COBOL copies can be found.\nThis option can be specified more than once.", v => config.CopyFolders.Add(v) },
                { "dp|dependencies=", "Path to folder containing programs to load and to use for parsing a generating the input program.", v => config.Dependencies.Add(v) },
                { "h|help",  "Output a usage message and exit.", v => help = (v!=null) },
                { "V|version",  "Output the version number of "+PROGNAME+" and exit.", v => version = (v!=null) },
            };

      
            //Add DefaultCopies to running session
            var folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            config.CopyFolders.Add(folder + @"\DefaultCopies\");

		    try {
                List<string> args;
		        try {

		            args = p.Parse(argv);
		        } catch (OptionException ex) {
                    return exit(1, ex.Message);
		        }

		        if (help) {
		            p.WriteOptionDescriptions(Console.Out);
		            return 0;
		        }
		        if (version) {
		            Console.WriteLine(PROGVERSION);
		            return 0;
		        }
                if (config.OutputFiles.Count == 0 && config.ExecToStep >= ExecutionStep.Generate)
                    config.ExecToStep = ExecutionStep.SemanticCheck; //If there is no given output file, we can't run generation, fallback to SemanticCheck

		        if (config.OutputFiles.Count > 0 && config.InputFiles.Count != config.OutputFiles.Count)
		            return exit(2, "The number of output files must be equal to the number of input files.");

		        if (args.Count > 0) pipename = args[0];


                //"startClient" will be true when "-K" is passed as an argument in command line.
                if (startClient != StartClient.No && once) {
                    pipename= "TypeCobol.Server";
                    using (NamedPipeClientStream namedPipeClient = new NamedPipeClientStream(pipename))
                    {
                        try {
                            namedPipeClient.Connect(100);
		                } catch (TimeoutException tEx) {
                            System.Diagnostics.Process process = new System.Diagnostics.Process();
                            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
		                    if (startClient == StartClient.NormalWindow) {
		                        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
		                    } else {
                                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                            }
		                    startInfo.FileName = "cmd.exe";
                            startInfo.Arguments = @"/c " + folder + Path.DirectorySeparatorChar+ "TypeCobol.CLI.exe";
                            process.StartInfo = startInfo;
                            process.Start();
                            
                            namedPipeClient.Connect(1000);
		                }
                        namedPipeClient.WriteByte(68);
                        

                        ConfigSerializer configSerializer = new ConfigSerializer();
                        var configBytes = configSerializer.Serialize(config);

                        namedPipeClient.Write(configBytes, 0, configBytes.Length);
                        //Wait for the response "job is done"
                        namedPipeClient.ReadByte();
                    }
				}
                
                //option -1
                else if (once) {
                    CLI.runOnce(config);
                } else {
                    runServer(pipename);
                }
			}
            catch (Exception e) {
                return exit(1, e.Message);
			}
            return 0;
		}

        /// <summary>
        /// Add an error message
        /// </summary>
        /// <param name="writer">Error Writer</param>
        /// <param name="messageCode">Message's code</param>
        /// <param name="message">The text message</param>
        /// <param name="path">The source file path</param>
		internal static void AddError(AbstractErrorWriter writer, MessageCode messageCode, string message, string path)
		{
            AddError(writer, messageCode, 0, 0, 1, message, path);
		}

        /// <summary>
        /// Add an error message
        /// </summary>
        /// <param name="writer">Error Writer</param>
        /// <param name="messageCode">Message's code</param>
        /// <param name="columnStart">Start column in the source file</param>
        /// <param name="columnEnd">End column in the source file</param>
        /// <param name="lineNumber">Lien number in the source file</param>
        /// <param name="message">The text message</param>
        /// <param name="path">The source file path</param>
        internal static void AddError(AbstractErrorWriter writer, MessageCode messageCode, int columnStart, int columnEnd, int lineNumber, string message, string path)
        {
            Diagnostic diag = new Diagnostic(messageCode, columnStart, columnEnd, lineNumber,
                message != null
                ? (path != null ? new object[2] { message, path } : new object[1] { message })
                : (path != null ? new object[1] { path } : new object[0]));
            diag.Message = message;
            writer.AddErrors(path, diag);
            Console.WriteLine(diag.Message);
		}

        private static void runServer(string pipename) {
			var parser = new Parser();

            var pipe = new NamedPipeServerStream(pipename, PipeDirection.InOut, 4, PipeTransmissionMode.Message);

			Commands.Register(66, new Parse(parser, pipe, pipe));
			Commands.Register(67, new Initialize(parser, pipe, pipe));
            Commands.Register(68, new RunCommandLine(parser, pipe, pipe));
			var decoder = new TypeCobol.Server.Serialization.IntegerSerializer();
			System.Console.WriteLine("NamedPipeServerStream thread created. Wait for a client to connect on " + pipename);
			while (true) {
				pipe.WaitForConnection(); // blocking
                //System.Console.WriteLine("Client connected.");
			    int code = 0;
                try {
					code = decoder.Deserialize(pipe);
					var command = Commands.Get(code);
                    command.execute(pipe);
				}
				catch (IOException ex) { Console.WriteLine("Error: {0}", ex.Message); }
                catch (System.Runtime.Serialization.SerializationException ex) { Console.WriteLine("Error: {0}", ex.Message); }
                finally {
                    pipe.Disconnect();
                    //68 is a server that need to stay alive
                    if(code.Equals(66) || code.Equals(67)) {
                        pipe.Close();
                    }
                }
			}
		}
        


		private static readonly string PROGNAME = System.AppDomain.CurrentDomain.FriendlyName;
		private static readonly string PROGVERSION = GetVersion();

		private static string GetVersion() {
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
			var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
			return info.FileVersion;
		}

		static int exit(int code, string message) {
			string errmsg = PROGNAME+": "+message+"\n";
			errmsg += "Try "+PROGNAME+" --help for usage information.";
			Console.WriteLine(errmsg);
			return code;
		}

	}
}
