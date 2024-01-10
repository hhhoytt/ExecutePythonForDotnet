# Execute Python for .NET
This code provides a helper class that makes it easy to execute python scripts and get their results in .NET v6. It uses your local copy of Python for the execution, so any packages you have installed locally it can use.

The provided python files in `python-scripts` serves as a demo to see this code in action.

## How to Run
First you must specify where it can find a copy of Python. In the `app.config`, change `linuxPythonPath` or `windowsPythonPath` to point to where python is installed on your machine. You can specify it for both Linux and Windows, as this code will work with both systems. For the windows path, the provided path must be relative to `%LOCALAPPDATA%`.

Next, navigate to the root of the project. Run `dotnet run` in your terminal.

## How to write Python for use with this library
This library reads off of `stdin` from whatever python script is running, so all data you want to read must be outputted on `stdin`. 

**When printing, be sure to flush the buffer immediatly using `print("Test", flush=True)`. Otherwise nothing will be printed until execution is done.** Otherwise nothing will be printed until execution is done. This will make it appear as if nothing is happening if you try to run your code asynchronously and don't get any data back.

Any exceptions thrown by a python script will be read off of `stderr`.

# Documentation
### `Program.cs`
This file provides an example of how the `PythonExe` class works. Just run it as described in the "How to Run" section to see it in action.

### `PythonExe`
This is the main class that you need to instantiate in order to run python scripts. It exists within the `PythonExecute` namespace.

It accepts the following constructors:
* `PythonExe()`
* `PythonExe(bool closeOnError)`: By default, `closeOnError` is true. This means the process running the python script will end if an exception is thrown. If you don't want it to end, you can turn that off here.

### `ExecutePython(string filepath)`
This is the method call to run a python script. It accepts a bunch of different calls:
* `ExecutePython(string filepath, bool isAsync = false)`: Execute the python script at the given filepath
* `ExecutePython(string filepath, string arg, bool isAsync = false)`: Execute the python script with the given command line arguments at the given filepath
* `ExecutePython(List<string> filepaths, bool isAsync = false)`: Executes all given python scripts at the same time based on the provided filepaths
* `ExecutePython(List<string> filepaths, List<string> args, bool isAsync = false)`: Executes all given python scripts with the given command line arguments based on the provided filepaths. The index of the filepath should match with the index of the provided arguments.

`isAsync`: This variable is used to make `ExecutePython()` not act as a blocking call and lets you get the results live by calling `GetLatestResult()`. You can call `IsComplete()` to check when the python code is done running. Please look at the examples in `Program.cs` for more information.

If `isAsync` remains false, `ExecutePython()` will be a blocking call. You can get the results after with `GetResults()`.

### `PythonResult`
This object is used to return results to you, the programmer. This objects has two properties:
* `resultFile`: The name of the python script that was executed
* `result`: The text outputted to `stdin`

### `KillAllExecution()`
This method call will immediately kill all currently running python processes.

### `KillExecution(string filename, bool killAllOnFail = true)`
This method will kill the processes running the provided file name. By default, if it fails to find the process, it will kill all processes. Based on a TODO I left this may not be working correctly.

### Getting Results
To get results back, you will use these method calls:
* `GetResults()`: Returns all results at once, used for blocking calls
* `GetLatestResult()`: Returns the most recent result, used for async calls
* `IsComplete()`: Returns true when all processes are done running, used for async calls in conjunction with `GetLatestResult()` 

### Error Handling

* `GetErrors()`: Returns all exceptions, used for blocking calls
* `GetLatestError()`: Returns the most recent exception, used for async calls
* `ErrorThrown()`: Returns true when an exception is thrown, used for async calls in conjunction with `GetLatestError()`

By default, execution of all python scripts will stop when an exception is thrown. To disable this, pass `false` into the `ExecutePython` constructor. Only do this if you know what you are doing, as this can cause further errors if not handled correctly.

## Important Notes:

* When printing out your data from your python script, be sure to flush the buffer immediatly using `print("Test", flush=True)`. Otherwise nothing will be printed until execution is done. This will cause unexpected results if you try to run this "asynchronously".
* If you try to execute the same file more than once (`ExecutePython(new List<string> { "file.py", "file.py" })`), this will cause unexpected results. I recommend not doing this.