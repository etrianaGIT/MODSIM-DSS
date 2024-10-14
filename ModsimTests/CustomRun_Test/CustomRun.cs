using Csu.Modsim.ModsimIO;
using Csu.Modsim.ModsimModel;
using System;

public static class CustomMODSIM
{
	public static Model myModel = new Model();
	public static void Main(string[] CmdArgs)
	{
		string FileName = CmdArgs[0];
		myModel.Init += OnInitialize;
		myModel.IterBottom += OnIterationBottom;
		myModel.IterTop += OnIterationTop;
		myModel.Converged += OnIterationConverge;
		myModel.End += OnFinished;
		myModel.OnMessage += OnMessage;
		myModel.OnModsimError += OnError;
        try
        {
            XYFileReader.Read(myModel, FileName);
            Modsim.RunSolver(myModel);

            Console.WriteLine("Finished run MODSIM!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        Console.ReadKey();
	}

	private static void OnInitialize()
	{
	}

	private static void OnIterationTop()
	{
	}

	private static void OnMessage(string message)
	{
		Console.WriteLine(message);
	}

	private static void OnError(string message)
	{
		Console.WriteLine(message);
	}

	private static void OnIterationBottom()
	{
	}

	private static void OnIterationConverge()
	{
	}

	private static void OnFinished()
	{
	}
}
