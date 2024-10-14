using System; 
using ASquared.SymbolicMath;

namespace Csu.Modsim.ModsimModel
{
    public class HydroCalculator
    {
        // Instance variables
        private Model _model;
        private Link[] _links;
        private string[] _varnames;
        private Symbol[] _variables, _flows;
        private HydropowerUnit[] _units;

        // Properties
        /// <summary>Gets an array of the symolic representation of the flows with the same elements as the number of units used to construct this instance.</summary>
        public Symbol[] Flows
        {
            get
            {
                return _flows;
            }
        }
        /// <summary>Gets and sets the array of hydropower units that define the various portions of the hydropower production or consumption functions.</summary>
        public HydropowerUnit[] Units
        {
            get
            {
                return _units;
            }
        }

        // Constructor
        public HydroCalculator(Model model)
            : this(model, model.hydro.HydroUnits)
        {
        }
        public HydroCalculator(Model model, HydropowerUnit[] units)
        {
            _model = model;
            _units = units;
        }

        // Methods
        /// <summary>Initializes the instance for optimization.</summary>
        public void Initialize()
        {
            // Gets the links and associated variables
            _links = _model.Links_All; // Gets an array of links sorted by the link number
            _varnames = new string[_links.Length];
            _variables = new Symbol[_links.Length];
            for (int i = 0; i < _links.Length; i++)
            {
                _varnames[i] = "q_" + _links[i].number;
                _variables[i] = _varnames[i];
            }

            // Fill the discharge variables
            _flows = new Symbol[_units.Length];
            for (int i = 0; i < _units.Length; i++)
                _flows[i] = this.Flow(_units[i]);
        }
        /// <summary>Gets the variable representing discharge through the links in the hydropower unit.</summary>
        /// <param name="unit">The hydropower unit for which to obtain the variable for discharge.</param>
        public Symbol Flow(HydropowerUnit unit)
        {
            if (unit.FlowLinks.Length < 1)
                throw new Exception("Must have at least one link to define discharge within hydropower unit '" + unit.Name + "'.");
            Symbol q = _variables[unit.FlowLinks[0].number - 1];
            for (int i = 1; i < unit.FlowLinks.Length; i++)
            {
                CheckBounds(unit.FlowLinks[i]); 
                q += _variables[unit.FlowLinks[i].number - 1];
            }
            return q; 
        }
        private void CheckBounds(Link l)
        {
            if (l.mlInfo.hi >= _model.defaultMaxCap || l.mlInfo.lo >= _model.defaultMaxCap)
                throw new ArgumentException("Bounds on the hydropower unit link '" + l.name + "' are equal to or exceed the maximum default capacity (" + (_model.defaultMaxCap/_model.ScaleFactor).ToString() + "). Realistic bounds on the links are required for the hydropower optimization routine to work properly."); 
        }
        /// <summary>Gets the symbol representing the head used for power calculations at the hydropower unit.</summary>
        /// <param name="hydroUnitIndex">The index of the hydropower unit within the array of units.</param>
        public Symbol Head(int hydroUnitIndex)
        {
            return _units[hydroUnitIndex].HeadFunction.Subs("q", _flows[hydroUnitIndex]);
        }
        /// <summary>Gets the symbolic representation of the efficiency curve at a specified head for the hydropower unit.</summary>
        /// <param name="hydroUnitIndex">The index of the hydropower unit within the array of units.</param>
        public Symbol Efficiency(int hydroUnitIndex)
        {
            return _units[hydroUnitIndex].EfficiencyFunction.Subs("q", _flows[hydroUnitIndex]);
        }
        /// <summary>Gets the symbolic representation of the hydropower function given the index of the hydropower unit.</summary>
        /// <param name="hydroUnitIndex">The index of the hydropower unit.</param>
        public Symbol Power(int hydroUnitIndex)
        {
            // P = k*q*H*eff

            // k
            double k = HydropowerUnit.SpecWeightH2O * HydropowerUnit.kWperlbftsec;
            k *= HydropowerUnit.DefaultFlowUnits.ConvertFrom(1, _model.FlowUnits);
            k *= HydropowerUnit.DefaultHeadUnits.ConvertFrom(1, _model.LengthUnits);

            // q 
            Symbol q = _flows[hydroUnitIndex];

            // H 
            Symbol H = this.Head(hydroUnitIndex);

            // eff 
            Symbol eff = this.Efficiency(hydroUnitIndex);

            // P 
            if (_units[hydroUnitIndex].Type == HydroUnitType.Pump)
                return -k * q * H / eff;
            else
                return k * q * H * eff;
        }
        /// <summary>Gets the symbolic representation of the hydropower energy production function given the index of the hydropower unit.</summary>
        /// <param name="hydroUnitIndex">The index of the hydropower unit in relation to the array of units passed to this instance.</param>
        public Symbol Energy(int hydroUnitIndex)
        {
            DateTime start = _model.mInfo.CurrentBegOfPeriodDate;
            DateTime end = start.AddHours(_units[hydroUnitIndex].GeneratingHours);
            return HydropowerUnit.DefaultPowerUnits.Integrate(1, start, end, HydropowerUnit.DefaultEnergyUnits) * (1 - _units[hydroUnitIndex].DowntimeFactor) * this.Power(hydroUnitIndex);
        }
        /// <summary>Gets the symbolic representation of the total hydropower energy production from all units.</summary>
        public Symbol TotalEnergy()
        {
            Symbol energy = this.Energy(0);
            for (int i = 1; i < _units.Length; i++)
                energy += this.Energy(i);
            return energy;
        }
    }
}
