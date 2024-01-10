using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Configuration;
using System.Text.RegularExpressions;

namespace PythonExecute {
    public class PythonResult {
        public string resultFile { get; set; }
        public string? result { get; set; }

        public PythonResult(string result, string resultFile) {
            this.resultFile = resultFile;
            this.result = result;
        }
    }

    //This is so we can pass this data to our event handler
    public class PythonProcess : Process {
        public string? pythonFileName { get; set; }
    }

    public class PythonExe {
        static string pythonPath = "/usr/bin/python3";
        List<PythonResult> results;
        List<Exception> errors;
        Queue<PythonResult> processedResults;
        Queue<Exception> processedErrors;
        List<Task> allTasks;
        List<PythonProcess> allProcesses;
        Dictionary<string, List<string>?> exceptionMessages; //Key on file name. This stores any values we read from stderr until we get a clear exception message
        readonly object resultsLock = new object();
        bool closeOnError = true;

        public PythonExe() : this(true) { }

        public PythonExe(bool closeOnError) {
            string? pathFound = null;

            this.closeOnError = closeOnError;
            results = new List<PythonResult>();
            errors = new List<Exception>();
            processedResults = new Queue<PythonResult>();
            processedErrors = new Queue<Exception>();
            allTasks = new List<Task>();
            allProcesses = new List<PythonProcess>();
            exceptionMessages = new Dictionary<string, List<string>?>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                pathFound = Environment.GetEnvironmentVariable("LOCALAPPDATA") + ConfigurationManager.AppSettings["windowsPythonPath"];
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                pathFound = ConfigurationManager.AppSettings["linuxPythonPath"];
            }

            if (pathFound == null) {
                Console.Error.WriteLine("Unable to find python path from app settings!");
                throw new Exception("Unable to find python path from app settings!");
            }

            pythonPath = pathFound;
        }

        //TODO: Something about how I wrote all these constructors feels wrong, I think it can be simplified further
        public void ExecutePython(string filepath, bool isAsync = false) {
            List<string> filepaths = new List<string>();
            filepaths.Add(filepath);
            ExecutePython(filepaths, isAsync);
        }

        public void ExecutePython(string filepath, string arg, bool isAsync = false) {
            List<string> filepaths = new List<string>();
            List<string> args = new List<string>();
            filepaths.Add(filepath);
            args.Add(arg);
            ExecutePython(filepaths, args, isAsync);
        }

        public void ExecutePython(List<string> filepaths, bool isAsync = false) {
            List<string> args = new List<string>();
            ExecutePython(filepaths, args, isAsync);
        }

        public void ExecutePython(List<string> filepaths, List<string> args, bool isAsync = false) {
            Task tmpPy;
            string pArg, filepath;

            if (!IsComplete()) {
                Console.Error.WriteLine("ERROR: ExecutePython is not done running, and yet it was called again! Please wait until it's done!");
                throw new Exception("ExecutePython is not done running, and yet it was called again! Please wait until it's done!");
            }

            results = new List<PythonResult>();
            processedResults = new Queue<PythonResult>();
            allTasks = new List<Task>();
            allProcesses = new List<PythonProcess>();

            for (int i = 0; i < filepaths.Count; i++) {
                filepath = filepaths[i];
                if (args.Count > i) pArg = args[i];
                else pArg = "";

                tmpPy = Task.Factory.StartNew(() => ExecutePythonInternal(filepath, pArg));
                allTasks.Add(tmpPy);
            }

            if (!isAsync) {
                //This turns it into a blocking call
                Task.WhenAll(allTasks).Wait();
            }
        }

        public void KillAllExecution() {
            foreach (Process p in allProcesses) {
                p.Close();
            }

            allTasks = new List<Task>();
            allProcesses = new List<PythonProcess>();
        }

        public bool KillExecution(string filename, bool killAllOnFail = true) {
            PythonProcess? p = allProcesses.Find(x => x.pythonFileName == filename);

            if (p == null) {
                if (killAllOnFail) {
                    KillAllExecution();
                    return true;
                }
                return false;
            }

            p.Close();

            //TODO: Remove this from the processess and tasks list

            return true;
        }

        void ExecutePythonInternal(string filepath, string args) {
            ProcessStartInfo pythonExe = new ProcessStartInfo();
            PythonProcess process = new PythonProcess();

            pythonExe.FileName = pythonPath;
            pythonExe.Arguments = filepath + ' ' + args;
            pythonExe.UseShellExecute = false;
            pythonExe.RedirectStandardOutput = true;
            pythonExe.RedirectStandardError = true;
            pythonExe.RedirectStandardInput = true;

            process.StartInfo = pythonExe;
            process.pythonFileName = filepath;
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorHandler);
            allProcesses.Add(process); //NOTE: This may need to be made threadsafe, not sure

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();
        }

        public PythonResult? GetLatestResult() {
            PythonResult res;

            if (processedResults.Count == 0) {
                return null;
            }

            //Used a lock here in case tasks work similar to threads
            lock (resultsLock) {
                res = processedResults.Dequeue();
            }

            return res;
        }

        public Exception? GetLatestError() {
            Exception res;

            if (processedErrors.Count == 0) {
                return null;
            }

            //Used a lock here in case tasks work similar to threads
            lock (resultsLock) {
                res = processedErrors.Dequeue();
            }

            return res;
        }

        public List<PythonResult> GetResults() {
            return results;
        }

        public List<Exception> GetErrors() {
            return errors;
        }

        public bool IsComplete() {
            if (allTasks.Count == 0) return true;

            foreach (Task t in allTasks) {
                if (!t.IsCompleted) return false;
            }

            return true;
        }

        public bool ErrorThrown() {
            return processedErrors.Count > 0;
        }

        void OutputHandler(object SendingProcess, DataReceivedEventArgs args) {
            PythonProcess proc = (PythonProcess)SendingProcess;
            PythonResult res;

            if (proc.pythonFileName == null) proc.pythonFileName = "FILENAME NOT SET";

            if (args.Data != null) {
                //Since we are using task factories, we will use a lock here
                lock (resultsLock) {
                    res = new PythonResult(args.Data, proc.pythonFileName);
                    results.Add(res);
                    processedResults.Enqueue(res);
                }
            }
        }

        void ErrorHandler(object SendingProcess, DataReceivedEventArgs args) {
            PythonProcess proc = (PythonProcess)SendingProcess;
            Match match;
            Exception ex;
            string exceptionMessage;
            List<string> innerExceptions = new List<string>();

            if (proc.pythonFileName == null) proc.pythonFileName = "FILENAME NOT SET";

            if (args.Data == null) return;

            match = Regex.Match(args.Data, @"Exception: (.*)");

            if (!match.Success) {
                //This means we probably have some other error string, store it for later
                if (!exceptionMessages.ContainsKey(proc.pythonFileName)) exceptionMessages.Add(proc.pythonFileName, new List<string>());

                innerExceptions = exceptionMessages[proc.pythonFileName]!;
                innerExceptions.Add(args.Data);

                return;
            }

            exceptionMessage = $"{proc.pythonFileName}: {match.Groups[1].Value}";

            if (exceptionMessages.ContainsKey(proc.pythonFileName)) innerExceptions = exceptionMessages[proc.pythonFileName]!;

            //Since we are using task factories, we will use a lock here
            lock (resultsLock) {
                ex = new Exception(exceptionMessage, innerException: new Exception(string.Join("\n", innerExceptions)));
                if (exceptionMessages.ContainsKey(proc.pythonFileName)) exceptionMessages[proc.pythonFileName]!.Clear();
                
                errors.Add(ex);
                processedErrors.Enqueue(ex);
            }

            if (closeOnError) {
                KillExecution(proc.pythonFileName);
            }
        }
    }
}