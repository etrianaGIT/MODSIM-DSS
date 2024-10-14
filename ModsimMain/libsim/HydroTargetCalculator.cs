using System;
using System.Collections.Generic;
using ASquared.SymbolicMath;

namespace Csu.Modsim.ModsimModel
{
    public class HydroTargetCalculator
    {
        // Instance variables 
        private Model _model;
        private HydropowerTarget[] _targets;
        private HydroCalculator[] _hydroCalcs; 

        // Properties 
        /// <summary>Gets an array of hydropower calculator objects</summary>
        public HydroCalculator[] HydroCalculators
        {
            get
            {
                return _hydroCalcs; 
            }
        }

        // Constructors
        public HydroTargetCalculator(Model model)
            : this(model, model.hydro.HydroTargets)
        {
        }
        public HydroTargetCalculator(Model model, HydropowerTarget[] targets)
        {
            _model = model;
            _targets = targets;
        }

        // Initialize 
        /// <summary>Initializes the calculator after the model has initialized and created its </summary>
        public void Initialize()
        {
            // Fill the hydropower calculators array
            _hydroCalcs = new HydroCalculator[_targets.Length];
            for (int i = 0; i < _targets.Length; i++)
            {
                _hydroCalcs[i] = new HydroCalculator(_model, _targets[i].HydroUnits);
                _hydroCalcs[i].Initialize(); 
            }
        }
        /// <summary>Gets the symbolic representation of the energy production for a specified hydropower target.</summary>
        /// <param name="hydroTargetIndex">The index of the hydropower target in relation to the targets array used to construct this instance.</param>
        public Symbol Energy(int hydroTargetIndex)
        {
            return _hydroCalcs[hydroTargetIndex].TotalEnergy(); 
        }
        /// <summary>Gets the symbolic representation of the difference between energy production and energy targets.</summary>
        /// <param name="hydroTargetIndex">The index of the hydropower target in relation to the targets array used to construct this instance.</param>
        public Symbol EnergyDiff(int hydroTargetIndex)
        {
            return EnergyDiff(hydroTargetIndex, _targets[hydroTargetIndex].EnergyTarget); 
        }
        /// <summary>Gets the symbolic representation of the difference between energy production and energy targets.</summary>
        /// <param name="hydroTargetIndex">The index of the hydropower target in relation to the targets array used to construct this instance.</param>
        public Symbol EnergyDiff(int hydroTargetIndex, double energyTarget)
        {
            return _hydroCalcs[hydroTargetIndex].TotalEnergy() - energyTarget;
        }
        /// <summary>Gets the symbolic representation of the squared difference between energy production and energy targets.</summary>
        /// <param name="hydroTargetIndex">The index of the hydropower target in relation to the targets array used to construct this instance.</param>
        public Symbol SqrEnergyDiff(int hydroTargetIndex)
        {
            return EnergyDiff(hydroTargetIndex) ^ 2;
        }
        /// <summary>Gets the symbolic representation of the squared difference between energy production and energy targets.</summary>
        /// <param name="hydroTargetIndex">The index of the hydropower target in relation to the targets array used to construct this instance.</param>
        public Symbol SqrEnergyDiff(int hydroTargetIndex, double energyTarget)
        {
            return EnergyDiff(hydroTargetIndex, energyTarget) ^ 2; 
        }
        /// <summary>Gets the symbolic representation of the squared difference between energy production and energy targets.</summary>
        /// <param name="hydroTargetIndex">The index of the hydropower target in relation to the targets array used to construct this instance.</param>
        public Symbol SqrEnergyDiffNrml(int hydroTargetIndex)
        {
            return (EnergyDiff(hydroTargetIndex) / _targets[hydroTargetIndex].EnergyCapacityMWh)^ 2;
        }
        /// <summary>Gets the symbolic representation of the squared difference between energy production and energy targets.</summary>
        /// <param name="hydroTargetIndex">The index of the hydropower target in relation to the targets array used to construct this instance.</param>
        public Symbol SqrEnergyDiffNrml(int hydroTargetIndex, double energyTarget)
        {
            return (EnergyDiff(hydroTargetIndex, energyTarget) / _targets[hydroTargetIndex].EnergyCapacityMWh) ^ 2;
        }
        /// <summary>Gets the symbolic representation of the total sum of squared differences between energy production and energy targets</summary>
        public Symbol TotalSqrEnergyDiff()
        {
            Symbol energydiff = SqrEnergyDiff(0);
            for (int i = 1; i < _targets.Length; i++)
                energydiff += SqrEnergyDiff(i);
            return energydiff;
        }
        /// <summary>Gets the symbolic representation of the total sum of squared differences between energy production and energy targets</summary>
        public Symbol TotalSqrEnergyDiff(double[] targets)
        {
            Symbol energydiff = SqrEnergyDiff(0, targets[0]);
            for (int i = 1; i < _targets.Length; i++)
                energydiff += SqrEnergyDiff(i, targets[i]);
            return energydiff;
        }
        /// <summary>Gets the symbolic representation of the total sum of squared differences between energy production and energy targets</summary>
        public Symbol TotalSqrEnergyDiffNrml()
        {
            Symbol energydiff = SqrEnergyDiff(0);
            for (int i = 1; i < _targets.Length; i++)
                energydiff += SqrEnergyDiff(i);
            return energydiff;
        }
        /// <summary>Gets the symbolic representation of the total sum of squared differences between energy production and energy targets</summary>
        public Symbol TotalSqrEnergyDiffNrml(double[] targets)
        {
            Symbol energydiff = SqrEnergyDiff(0, targets[0]);
            for (int i = 1; i < _targets.Length; i++)
                energydiff += SqrEnergyDiff(i, targets[i]);
            return energydiff;
        }

    }
}
