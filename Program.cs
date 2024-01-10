using System;
using System.Diagnostics;
using System.IO;

namespace PythonExecute {
    class Program {
        static string filepath = "./python-scripts/WindowsTest.py"; //ReadCPU.py
        static string commandLine = "./python-scripts/CommandLine.py"; 
        static string errorTest = "./python-scripts/CatchError.py";

        static void Main(string[] args) {
            Console.WriteLine($"Running Python Demo...");
            
            PythonExe py = new PythonExe();
            List<string> filePaths = new List<string>(); 
            PythonResult? res;

            filePaths.Add(filepath);

            //This is a blocking call and gets the results all at once
            Console.WriteLine("Get Blocking Results:");
            py.ExecutePython(filepath);
            foreach (PythonResult r in py.GetResults()) {
                Console.WriteLine("Blocking Results: " + r.result);
            }

            //This can be treated "async" and returns them live
            Console.WriteLine("Get Live Results:");
            py.ExecutePython(filepath, true);
            while (!py.IsComplete()) {
                res = py.GetLatestResult();

                if (res == null) continue;

                Console.WriteLine("Live Results: " + res.result);
            }

            //This runs multiple files at once as a blocking call
            Console.WriteLine("Get multiple file results blocking call:");
            py.ExecutePython(filePaths);
            foreach (PythonResult r in py.GetResults()) {
                Console.WriteLine(r.resultFile + ": " + r.result);
            }

            //This runs multiple files at once
            Console.WriteLine("Get multiple file results async call:");
            py.ExecutePython(filePaths, true);
            while (!py.IsComplete()) {
                res = py.GetLatestResult();

                if (res == null) continue;

                Console.WriteLine(res.resultFile + ": " + res.result);
            }

            //This runs a file with arguments blocking
            Console.WriteLine("Get files with arguments results blocking call:");
            py.ExecutePython(commandLine, "TestArg1 TestArg2");
            foreach (PythonResult r in py.GetResults()) {
                Console.WriteLine(r.resultFile + ": " + r.result);
            }

            //This runs a file with arguments async
            Console.WriteLine("Get files with arguments results async call:");
            py.ExecutePython(commandLine, "Arg3 --version -j", isAsync: true);
            while (!py.IsComplete()) {
                res = py.GetLatestResult();

                if (res == null) continue;

                Console.WriteLine(res.resultFile + ": " + res.result);
            }

            //Catching errors blocking call
            Console.WriteLine("Testing Catching errors Blocking:");
            py.ExecutePython(errorTest);
            foreach (PythonResult r in py.GetResults()) {
                Console.WriteLine("Blocking Results: " + r.result);
            }
            foreach (Exception r in py.GetErrors()) {
                Console.WriteLine("Error Results: " + r.Message);
                Console.WriteLine("Inner Exception: " + r.InnerException!.Message);
            }

            //Catching errors async call
            Console.WriteLine("Testing Catching errors Async:");
            py.ExecutePython(errorTest, true);
            while (!py.IsComplete()) {
                res = py.GetLatestResult();

                if (py.ErrorThrown()) {
                    Console.WriteLine(py.GetLatestError()!.Message);
                    py.KillAllExecution(); //Make sure to do this if you've disabled closing on error! By default this is off, so this isn't needed for our case
                    break;
                }

                if (res == null) continue;

                Console.WriteLine("Live Results: " + res.result);
            }

            Console.WriteLine("Done");
        }
    }
}