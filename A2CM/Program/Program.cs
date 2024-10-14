using System;
using ASquared.SymbolicMath;

namespace MainProgram
{
    class Program
    {
        static void Main(string[] args)
        {
			//Symbol x = new Symbol('x');
			//string p = "x -";
			//Symbol y = Symbol.Parse(p);
			Symbol m = Symbol.Parse("_e^x - exp(x)");
			//Console.WriteLine(p);
			//Console.WriteLine(y);
			Console.WriteLine(m.Eval());

			//Double[] vals = y.Eval(new Double[] { 1, 2, 3 });
			//foreach (var v in vals)
			//    Console.WriteLine(v);

			//Console.WriteLine(y.SymbolType);

			//// Test polynomials subtraction, addition, multiplication and division 
			//Symbol q = "q";
			//Symbol p1 = 10 * (q ^ 3) - 2 * (q ^ 2) + 300;
			//Symbol p2 = 20 * q + 3 * (q ^ 2);
			//Symbol power = ((p1 - p2) ^ 2); 
			//Console.WriteLine("\n\nMultiplication: " + (p1 * p2));
			//Console.WriteLine("Addition: " + (p1 + p2));
			//Console.WriteLine("Subtraction: " + (p1 - p2)); // this is the wrong string representation, but it calculates correctly
			//Console.WriteLine("Division: " + (p1 / p2));
			//Console.WriteLine("Power: " + power);
			//Console.WriteLine("\nDifferentiation:\n\nMultiplication: " + Sym.Diff(p1 * p2));
			//Console.WriteLine("Addition: " + Sym.Diff(p1 + p2));
			//Console.WriteLine("Subtraction: " + Sym.Diff(p1 - p2)); // this is the wrong string representation, but it calculates correctly
			//Console.WriteLine("Division: " + Sym.Diff(p1 / p2));
			//Console.WriteLine("Power: " + Sym.Diff(power));
			//Console.WriteLine("\n\nExample:q\t" + power + "\tdiff(" + power + ")");
			//for (int i = 0; i <= 10; i++)
			//    Console.WriteLine(i + "\t" + power.Eval(i) + "\t" + Sym.Diff(power).Eval(i)); 

            // end 
            Console.WriteLine("\nPress any key to continue..."); 
            Console.ReadKey();
        }
    }
}
