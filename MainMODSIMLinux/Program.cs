// See https://aka.ms/new-console-template for more information
using Csu.Modsim.ModsimIO;
using Csu.Modsim.ModsimModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

Console.WriteLine("Hello, World!");
// Program.cs

string fileName = args[0];

Model myModel = new Model();
myModel.OnMessage += OnMessage;

void OnMessage(string message)
{
    Console.WriteLine(message);
}

myModel.OnModsimError += OnError;

void OnError(string message)
{
    Console.WriteLine(message);
}

XYFileReader.Read(myModel, fileName);

// Adding plug-ins

// foreach (Node res in myModel.Nodes_Reservoirs)
//     res.m.min_volume = res.m.min_volume;

Modsim.RunSolver(myModel);

Console.ReadLine();
