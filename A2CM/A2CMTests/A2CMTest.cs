using System;
using System.Collections;
using System.Diagnostics;
using System.IO; 
using ASquared.ModelOptimization;
using ASquared.ModelStatistics;
using ASquared;
using ASquared.SymbolicMath;

namespace ASquared.ModelOptimization
{
    public enum TestingOption { RootFinder, Matrix, LineSearch, LinearSolver, CondGradient } 

    public class A2CMTest
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    TestingOption[] opts = (TestingOption[])Enum.GetValues(typeof(TestingOption));
                    foreach (TestingOption opt in opts)
                        typeof(A2CMTest).GetMethod("Test" + opt.ToString()).Invoke(null, null);
                }
                else
                {
                    TestingOption opt = (TestingOption)Enum.Parse(typeof(TestingOption), args[0], true);
                    typeof(A2CMTest).GetMethod("Test" + opt.ToString()).Invoke(null, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred while testing: " + ex.ToString());
            }
            Console.ReadKey();
        }

        // Testing conditional gradient method
        public static void TestCondGradient()
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location)) + @"\";
            StreamWriter sw = new StreamWriter(dir + "log.txt");
            
            Console.WriteLine("Conditional Gradient Method");
            
            string outline = "Tolerance\tMax iters\tx\ty\tz\tTime";
            Console.WriteLine(outline);
            sw.WriteLine(outline); 
            
            Paraboloid p = new Paraboloid(1000, 10, 0.2, 4);
            //Paraboloid p = new Paraboloid(25, -10, 20.5); 
            Stopwatch s = new Stopwatch();
            //for (Double tol = 1; tol > 1e-12; tol /= 10)
            for (int i = 0; i < 1; i++)
            {
                Double tol = 0.0001;
                ConditionalGradientSolver cgSolver = new ConditionalGradientSolver(p);
                cgSolver.Tolerance = tol;
                cgSolver.LogFile = dir + "log_CGM.txt";
                cgSolver.MaxIterations = 20000;
                try
                {
                    s.Reset();
                    s.Start(); 
                    cgSolver.Solve();
                    s.Stop();
                    outline = cgSolver.Tolerance.ToString() + "\t" +
                        cgSolver.Iteration + "\t" +
                        cgSolver.Decisions[0] + "\t" +
                        cgSolver.Decisions[1] + "\t" +
                        cgSolver.Optimum + "\t" +
                        s.Elapsed.TotalMilliseconds;
                    Console.WriteLine(outline);
                    sw.WriteLine(outline); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString()); 
                } 
            }
            sw.Close(); 
            Console.WriteLine("Done"); 
        }

        // Testing linear solver
        public static void TestLinearSolver()
        {
            Matrix A = "[2 1 2; 3 3 1]"; 
            Matrix b = "[4; 3]";
            Matrix c = "[4; 1; 1]";
            SolveLinear(A, b, c);

            A = "[-2 1 1 0 0; -1 2 0 1 0; 1 0 0 0 1]";
            b = "[2; 7; 3]";
            c = "[-1; -2; 0; 0; 0]";
            SolveLinear(A, b, c);

            A = "[1 4 6; -1 2 4; -1 -3 -1]";
            b = "[4; 1; -6]";
            c = "[-1 -2 -1]";
            LinearSolver ls = new LinearSolver(A, b, c);
            ls.ConstraintTypes = new ConstraintType[] { ConstraintType.LeftLessThanRight, ConstraintType.LeftLessThanRight, ConstraintType.LeftGreaterThanRight };
            ls.VariableTypes = new VariableType[] { VariableType.NonNegative, VariableType.NonNegative, VariableType.NonNegative };
            
            ls.Solve();
            Console.WriteLine(ls.ToString());
            Console.WriteLine("Solution: \nx = \n" + ls.x);
            Console.WriteLine("\nz = " + ls.Optimum.ToString() + "\n\n");
        }
        private static void SolveLinear(Matrix A, Matrix b, Matrix c)
        {
            LinearSolver ls = new LinearSolver(A, b, c);
            ls.Solve();
            Console.WriteLine(ls.ToString());
            Console.WriteLine("Solution: \nx = \n" + ls.x);
            Console.WriteLine("\nz = " + ls.Optimum.ToString() + "\n\n");
        }

        // Testing line search
        public static void TestLineSearch()
        {
            Symbol x = 'x';
            LineSearchObjective f = (LineSearchObjective)(10 * ((x - 3) ^ 2));
            LineSearchSolver solver = new LineSearchSolver(f, LineSearchMethod.Golden);
            solver.Solve(new Object[] { -1000000.0, 1000000.0 });
            Console.WriteLine("f = " + f);
            Console.WriteLine("min f(x) = " + f.fxn.Eval(f.x).ToString()); 
            Console.WriteLine("    at x = " + f.x.ToString());
            for (Double tol = 0.001; tol >= 0.000000001; tol /= 10)
            {
                solver.Tolerance = tol;
                solver.Solve(new Object[] { -1000000, 1000000 });
                Console.WriteLine("Tol = " + tol.ToString() + " Iters = " + solver.Iteration + " x = " + f.x.ToString() + " f = " + f.fxn.Eval(f.x).ToString()); 
            }

            // Bounded line search 
            solver.Bounds = new Bounds(2.9, 50);
            Console.WriteLine("\nBounded line search... Bounds = " + solver.Bounds.ToString()); 
            for (Double tol = 0.001; tol >= 0.000000001; tol /= 10)
            {
                solver.Tolerance = tol;
                solver.Solve(new Object[] { -1000000, 1000000 });
                Console.WriteLine("Tol = " + tol.ToString() + " Iters = " + solver.Iteration + " x = " + f.x.ToString() + " f = " + f.fxn.Eval(f.x).ToString());
            }
        }

        // Testing matrix
        public static void TestMatrix()
        {

            Matrix m = "[0 1; 4 2]";
            Console.WriteLine("\nm = \n" + m);
            Console.WriteLine("\nm^-1 = \n" + m.Inverse());
            Console.WriteLine("\nm^-2 = \n" + m.Pow(-2));
            Console.WriteLine("\nm^2 = \n" + m.Pow(2));
            Console.WriteLine("\nm + m = \n" + (m + m));
            Console.WriteLine("\nm^2 - m = \n" + (m.Pow(2) - m));
            Matrix a = "[0.9649    0.9572    0.1419; 0.1576    0.4854    0.4218; 0.9706    0.8003    0.9157]";
            Console.WriteLine("\nbig = \n" + a);
            Console.WriteLine("\nbig^-1 = \n" + a.Inverse());
            Console.WriteLine("\nbig^-2 = \n" + a.Pow(-2));
            Console.WriteLine("\nbig^2 = \n" + a.Pow(2));
            Console.WriteLine("\nbig + a = \n" + (a + a));
            Console.WriteLine("\nbig^2 - a = \n" + (a.Pow(2) - a));
            Matrix big = @"    0.2277    0.4389    0.2217    0.8010    0.9631    0.3674    0.3354    0.7150
    0.4357    0.1111    0.1174    0.0292    0.5468    0.9880    0.6797    0.9037
    0.3111    0.2581    0.2967    0.9289    0.5211    0.0377    0.1366    0.8909
    0.9234    0.4087    0.3188    0.7303    0.2316    0.8852    0.7212    0.3342
    0.4302    0.5949    0.4242    0.4886    0.4889    0.9133    0.1068    0.6987
    0.1848    0.2622    0.5079    0.5785    0.6241    0.7962    0.6538    0.1978
    0.9049    0.6028    0.0855    0.2373    0.6791    0.0987    0.4942    0.0305
    0.9797    0.7112    0.2625    0.4588    0.3955    0.2619    0.7791    0.7441";
            Console.WriteLine("\nbig = \n" + big);
            Console.WriteLine("\nbig^-1 = \n" + big.Inverse());
            Console.WriteLine("\nbig^-2 = \n" + big.Pow(-2));
            Console.WriteLine("\nbig^2 = \n" + big.Pow(2));
            Console.WriteLine("\nbig + big = \n" + (big + big));
            Console.WriteLine("\nbig^2 - big = \n" + (big.Pow(2) - big));
            Matrix b = @"5
8
10
13
1
8
3
2";
            Console.WriteLine("\nb = \n" + b); 
            Console.WriteLine("\nbig \\ b = \n" + Matrix.SolveLinearSys(big, b));

            Matrix c = "[ 1 4 2; 2 9 15 ]";
            Matrix d = "[ 4; 1; 5 ]";
            Console.WriteLine("\nc = \n" + c);
            Console.WriteLine("\nd = \n" + d);
            Console.WriteLine("\nc * d = \n" + (c * d));

            Matrix e = "[ 4 1 2; 45 10 6 ]";
            Console.WriteLine("\ne = \n" + e);
            Console.WriteLine("\ne.ElementwiseMult(c) = \n" + e.ElementwiseMult(c));

            // poly fitting for SingleVariableFunction
            Double[] flows = new Double[] { 0, 25, 50, 75, 100, 125, 150, 175, 200, 225, 250, 275, 300, 325, 350, 375, 400 };
            Double[] TWelev = new Double[] { 438, 438.5, 439, 439.3, 440.5, 442, 443.5, 444.9, 446, 447.1, 448.2, 449.3, 450.4, 451.5, 452.6, 453.7, 454.8 };
            SingleVariableFunction s = PolynomialFxn.Fit(flows, TWelev, 4);
            Console.WriteLine("\nPolynomial fit:\n" + s); 
            Double r2 = 0.999;
            Console.WriteLine("\nPolynomial fit to R² = " + r2.ToString());
            Console.WriteLine(PolynomialFxn.GoodnessOfFit(flows, TWelev, r2));

            // poly fitting for Symbol 
            Symbol tw = Sym.FitPolynomial(flows, TWelev, 'p', (Int32)4);
            Console.WriteLine("\nPolynomial fit:\n" + tw);
            Console.WriteLine("\nPolynomial fit to R² = " + r2.ToString());
            Console.WriteLine(Sym.PolynomialGoodnessOfFit(flows, TWelev, 'p', r2));
            
        }

        // Testing root finder
        public static Int32 NumOfDigits(Double val)
        {
            return (Int32)Math.Floor(Math.Log10(val));
        }
        public static void TestRootFinder()
        {
            // Get the list of root-finding methods 
            ArrayList aList = new ArrayList(Enum.GetValues(typeof(RootFinderMethod)));
            RootFinderMethod[] methods = (RootFinderMethod[])aList.ToArray(typeof(RootFinderMethod));

            ISingleVariableFunction oneOverX = new MultiplyFxn(1, new PowerFxn(-1));
            for (Int32 i = 0; i < 10; i++)
            {
                Console.WriteLine(oneOverX.ToString());
                oneOverX = oneOverX.GetDeriv();
            }

            // Define the functions to test 
            SingleVariableFunction x = new SingleVariableFunction(); 
            SingleVariableFunction log = new LogFxn();
            SingleVariableFunction TwoXSqr = 2.0 * x.Pow(2);
            SingleVariableFunction logTwoXSqr = new LogFxn(Math.Exp(1), TwoXSqr);
            SingleVariableFunction log10Minus5 = new LogFxn(10); 
            log10Minus5 -= 5;
            SingleVariableFunction NegTwoXSqrPlus8 = 8 - TwoXSqr;
            Double[] a = new Double[] { 85, -4, 2, 5, 1, 8 };
            ISingleVariableFunction poly = new PolynomialFxn(a);
            Double[] actualRoots = new Double[] { 1, 0, 0.7071, 100000, 2, -1 };
            Double[] begInterval = new Double[] { 0.5, -1, -0.5, 1, -10, -1.5 };
            Double[] endInterval = new Double[] { 10, 10, 10, 100999, 0, 0 };
            Bounds[] bounds = new Bounds[] { new Bounds(0, Double.MaxValue), new Bounds(), new Bounds(0.0, Double.MaxValue), new Bounds(0.0, Double.MaxValue), new Bounds(), new Bounds() };
            ISingleVariableFunction[] fxn = new ISingleVariableFunction[] { log, TwoXSqr, logTwoXSqr, log10Minus5, NegTwoXSqrPlus8, poly };


            // Loop through all methods except user-defined 
            foreach (RootFinderMethod method in methods)
            {
                Double totalTime = 0.0;
                Int32 totalNumOfIters = 0;
                Console.WriteLine("\nUsing method: " + method.ToString());
                for (Int32 j = 0; j < fxn.Length; j++)
                {
                    // Get inputs for the function and method 
                    RootFinderInputs inputs = new RootFinderInputs();
                    inputs.rootInit = begInterval[j];
                    inputs.rootPrev1 = endInterval[j];
                    inputs.rootPrev2 = (begInterval[j] + endInterval[j]) / 2.0;
                    inputs.fxnValInit = fxn[j].GetValue(inputs.rootInit);
                    inputs.fxnValPrev1 = fxn[j].GetValue(inputs.rootPrev1);
                    inputs.fxnValPrev2 = fxn[j].GetValue(inputs.rootPrev2);
                    inputs.derivVal = fxn[j].GetDeriv().GetValue(inputs.rootInit);
                    inputs.step = Math.Abs(begInterval[j] - endInterval[j]) / 7.0;

                    try
                    {
                        string msg = "Working on " + fxn[j].ToString() + ": ";
                        Console.Write(msg);
                        RootFinder rf = new RootFinder(method, inputs, fxn[j]);
                        rf.Tolerance = 0.0000001;
                        rf.MaxIterations = 1000;
                        rf.Bounds = bounds[j];
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        Double root = rf.GetRoot();
                        sw.Stop();
                        root = Math.Round(root, Math.Max(NumOfDigits(1 / rf.Tolerance) - NumOfDigits((Int32)root), 0));
                        Double fxnVal = Math.Round(rf.FunctionValue, 5);
                        fxnVal = Math.Round(fxnVal, Math.Max(NumOfDigits(1 / rf.Tolerance) - NumOfDigits((Int32)fxnVal), 0));
                        string newmsg = "f(" + root.ToString() + ") = " + fxnVal.ToString() + "\tShould be: " + actualRoots[j].ToString();
                        Int32 noOfTabs = (Int32)Math.Floor((50 - msg.Length) / 4.0 + 0.49);
                        newmsg = newmsg.PadLeft(newmsg.Length + noOfTabs, '\t') + "\t\tNum of Iters = " + rf.NumOfIters.ToString() + "\t\tTime = " + sw.Elapsed.TotalMilliseconds.ToString();
                        totalTime += sw.Elapsed.TotalMilliseconds;
                        totalNumOfIters += rf.NumOfIters;
                        sw.Reset();
                        Console.Write(newmsg);
                        if (rf.Method != method)
                            Console.Write("\t\tUsed the " + rf.Method.ToString() + " instead because the original problem was not solvable with " + method.ToString());
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error occurred: " + ex.ToString());
                    }
                }
                Console.WriteLine("Total Num of Iters = \t" + totalNumOfIters.ToString());
                Console.WriteLine("Average Num of Iters = \t" + (totalNumOfIters / fxn.Length).ToString());
                Console.WriteLine("Total time = \t\t\t" + totalTime.ToString());
                Console.WriteLine("Average time = \t\t" + (totalTime / fxn.Length).ToString());

                
            }
        }
    }

    public class Paraboloid : IConditionalGradientSolvableFunction
    {
        private Symbol _fxn;
        private Matrix _c, _x, _A, _b;
        private Matrix _mask = new Double[2] { 1.0, 1.0 };
        private VariableType[] _vTypes = new VariableType[] { VariableType.Free, VariableType.Free }; 
        private ConstraintType[] _cTypes = new ConstraintType[] { ConstraintType.LeftGreaterThanRight, ConstraintType.LeftLessThanRight, ConstraintType.LeftGreaterThanRight, ConstraintType.LeftLessThanRight };
        private String[] _vars = new String[] { "x", "y" };
        private bool _isConverged;

        // Properties
        public Symbol fxn { get { return _fxn; } set { _fxn = value; } }
        public Matrix costs { get { return _c; } set { _c = value; } }
        public Matrix decisions { get { return _x; } set { _x = value; } }
        public Matrix variableMask { get { return _mask; } }
        public Matrix A { get { return _A; } }
        public Matrix b { get { return _b; } }
        public VariableType[] VariableTypes { get { return _vTypes; } }
        public ConstraintType[] ConstraintTypes { get { return _cTypes; } }
        public String[] variables { get { return _vars; } }
        public bool IsConverged { get { return _isConverged; } set { _isConverged = value; } }

        // Constructors 
        /// <summary>Constructs a paraboloid like so: z = (x - a)^2 + (y - b)^2</summary>
        public Paraboloid(Double a, Double b)
        {
            Symbol x = 'x', y = 'y';
            _fxn = ((x - a) ^ 2) + ((y - b) ^ 2);
        }
        /// <summary>Constructs a paraboloid like so: z = (x - a)^2 + (y - b)^2 + c</summary>
        public Paraboloid(Double a, Double b, Double c)
        {
            Symbol x = 'x', y = 'y';
            _fxn = ((x - a) ^ 2) + ((y - b) ^ 2) + c;
        }
        /// <summary>Constructs a paraboloid like so: z = a + b*x + c*x*y + d*y^2</summary>
        public Paraboloid(Double a, Double b, Double c, Double d)
        {
            Symbol x = 'x', y = 'y';
            _fxn = a + b * x + c * x * y + d * (y ^ 2);
        }

        public void Initialize()
        {
            _isConverged = false;
            _A = "[1 0; 1 0; 0 1; 0 1]";
            _b = "[-1000; 1000; -1000; 1000]";
            _c = "[1; 1]";
        }
    }
}
