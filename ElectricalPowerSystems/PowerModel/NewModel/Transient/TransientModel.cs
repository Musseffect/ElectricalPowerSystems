﻿using ElectricalPowerSystems.Equations.DAE;
using ElectricalPowerSystems.MathUtils;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricalPowerSystems.PowerModel.NewModel.Transient
{
    public interface ITransientEventGenerator
    {
        List<TransientEvent> GenerateEvents(double t0, double t1);
    }
    public class TransientSolution
    {
        List<double[]> values;
        List<double> timeArray;
        string[] variableNames;
        int stateChangedCount;
        public void Plot()
        {
            GUI.ModelEditor.Windows.ChartWindow window = new GUI.ModelEditor.Windows.ChartWindow();
            window.Plot(values, timeArray, variableNames);
            window.Show();
        }
        public int GetPointCount()
        {
            return values.Count;
        }
        public TransientSolution(string[] variableNames)
        {
            this.values = new List<double[]>();
            this.timeArray = new List<double>();
            this.variableNames = variableNames;
        }
        public void AddPoint(double[] point, double time)
        {
            this.values.Add(point);
            this.timeArray.Add(time);
        }
        public void SetStateChangedCount(int stateChangedCount)
        {
            this.stateChangedCount = stateChangedCount;
        }
        public override string ToString()
        {
            return $"Transient solution: {{steps: {timeArray.Count}, changes of state: {stateChangedCount}}}";
        }
    }
    public abstract class TransientEvent
    {
        double time;
        public double Time { get { return time; } }
        public TransientEvent(double time)
        {
            this.time = time;
        }
        public abstract bool Execute(TransientState stateValues);
        public abstract List<Equations.DAE.Parameter> GetParameters();
    }
    public class TransientModel : IModel
    {
        List<ITransientElement> elements;
        List<TransientEvent> events;
        double t0;
        double t1;
        Equations.DAE.Implicit.DAEISolver solver;
        List<Pin> nodeList;
        int stateChangedCount;
        int maxPoints = 1024 * 1024;//8MB
        public TransientModel()
        {
            elements = new List<ITransientElement>();
            events = new List<TransientEvent>();
            nodeList = new List<Pin>();
        }
        private string GenerateEquations()
        {
            string equations = "";
            Dictionary<string, List<CurrentFlowBlock>> nodeEquationsIn = new Dictionary<string, List<CurrentFlowBlock>>();
            Dictionary<string, List<CurrentFlowBlock>> nodeEquationsOut = new Dictionary<string, List<CurrentFlowBlock>>();
            for (int i = 0; i < nodeList.Count; i++)
            {
                string[] variables = nodeList[i].GetNodeVariables();
                foreach (var variable in variables)
                {
                    nodeEquationsIn.Add(variable, new List<CurrentFlowBlock>());
                    nodeEquationsOut.Add(variable, new List<CurrentFlowBlock>());
                }
            }
            foreach (var element in elements)
            {
                List<EquationBlock> blocks = element.GenerateEquations();
                foreach (var block in blocks)
                {
                    if (block is CurrentFlowBlock currentFlowBlock)
                    {
                        if (currentFlowBlock.Node1 != null)
                            nodeEquationsIn[currentFlowBlock.Node1].Add(currentFlowBlock);
                        if (currentFlowBlock.Node2 != null)
                            nodeEquationsOut[currentFlowBlock.Node2].Add(currentFlowBlock);
                    }
                    else
                    {
                        equations += block.Equation + Environment.NewLine;
                    }
                }
            }
            equations += "//Current equations" + Environment.NewLine;
            for (int i = 0; i < nodeList.Count; i++)
            {
                string[] variables = nodeList[i].GetNodeVariables();
                foreach (var variable in variables)
                {
                    var nodeIn = nodeEquationsIn[variable];
                    var nodeOut = nodeEquationsOut[variable];
                    string left = "";
                    int k = 0;
                    //left side = right side
                    foreach (var block in nodeOut)
                    {
                        if (k != 0)
                            left += " + ";
                        left += block.Equation;
                        k++;
                    }
                    if (nodeOut.Count == 0)
                        left = "0";
                    string right = "";
                    k = 0;
                    foreach (var block in nodeIn)
                    {
                        if (k != 0)
                            right += " + ";
                        right += block.Equation;
                        k++;
                    }
                    if (nodeIn.Count == 0)
                        right = "0";
                    string eq = $"{left} = {right}; //node {variable}";
                    if (nodeOut.Count + nodeIn.Count > 0)
                        equations += eq + Environment.NewLine;
                }
            }
            foreach (var element in elements)
            {
                var blocks = element.GenerateParameters();
                foreach (var block in blocks)
                {
                    equations += block.Equation + Environment.NewLine;
                }
            }
            return equations;
        }
        private double UpdateState(
            double tPrev,
            double t,
            Vector<double> xPrev,
            Vector<double> x,
            Dictionary<string, int> variableMap,
            ref Equations.DAE.Implicit.DAEIAnalytic system)
        {
            double dt = t - tPrev;
            //for each event interpolate values and update stuff
            while (events.Count>0)
            {
                TransientEvent _event = events.First<TransientEvent>();
                if (_event.Time > t)
                {
                    return t;
                }
#if TEST||DEBUG
                if (_event.Time < tPrev)
                    throw new Exception("UpdateState exception");
#endif
                events.RemoveAt(0);
                Vector<double> xCurrent = MathUtils.MathUtils.Interpolate(xPrev, x, (_event.Time - tPrev) / dt);
                TransientState currentState = new TransientState(xCurrent,variableMap);
                if (_event.Execute(currentState)) 
                {
                    system.UpdateParameters(_event.GetParameters());
                    double eventTime = _event.Time;
                    while (events.Count>0)
                    {
                        _event = events.First<TransientEvent>();
                        if (_event.Time > eventTime)
                            break;
                        events.RemoveAt(0);
                        if (_event.Execute(currentState))
                        {
                            system.UpdateParameters(_event.GetParameters());
                        }
                    }
                    stateChangedCount++;
                    return eventTime;
                }
            }
            return t;
        }
        private void InitEvents(ref Equations.DAE.Implicit.DAEIAnalytic system)
        {
            foreach (var element in elements)
            {
                if (element is ITransientEventGenerator)
                {
                    events.AddRange((element as ITransientEventGenerator).GenerateEvents(t0,t1));
                }
            }
            //sort events by time
            events.OrderBy(x=>x.Time);
            while (events.Count > 0)
            {
                var _event = events.First();
                if (_event.Time >= t0)
                    break;
                system.UpdateParameters(_event.GetParameters());
            }
        }
        private TransientSolution Solve(string equations)
        {
            stateChangedCount = 0;
            List<IScopeElement> scopeElements = new List<IScopeElement>();
            foreach (var element in elements)
            {
                if (element is IScopeElement)
                {
                    scopeElements.Add(element as IScopeElement);
                }
            }
            Equations.DAE.Compiler compiler = new Equations.DAE.Compiler();
            Equations.DAE.Implicit.DAEIDescription compiledEquation = compiler.CompileDAEImplicit(equations);
            double t = t0;
            Equations.DAE.Implicit.DAEIAnalytic system = new Equations.DAE.Implicit.DAEIAnalytic(compiledEquation);
            Vector<double> x = Vector<double>.Build.SparseOfArray(compiledEquation.InitialValues);
            List<string> variableNames = new List<string>();
            foreach (var scope in scopeElements)
            {
                variableNames.AddRange(scope.GetTransientVariableNames());
            }
            //generate events
            InitEvents(ref system);
            TransientSolution result = new TransientSolution(variableNames.ToArray());
            int scopeVariableCount = variableNames.Count;
            Dictionary<string, int> variableMap = compiledEquation.GetVariableDictionary();

            {
                TransientState currentState = new TransientState(x, variableMap);
                List<double> currentValues = new List<double>();
                foreach (var scopeElement in scopeElements)
                {
                    currentValues.AddRange(scopeElement.GetReading(currentState));
                }
                result.AddPoint(currentValues.ToArray(), t);
            }
            while (t < t1 && maxPoints>(scopeVariableCount+1)*result.GetPointCount())
            {
                Vector<double> xPrev = x;
                double tPrev = t;
                //make step
                x = solver.IntegrateStep( system ,x, t);
                t += solver.Step;
                //update t
                if (events.Count !=0)
                {
                    //if state happened during [tprev,tcurr]
                    t = UpdateState(tPrev, t, xPrev, x, variableMap, ref system);
                    x = MathUtils.MathUtils.Interpolate(xPrev, x, (t - tPrev) / solver.Step);
                }
                TransientState currentState = new TransientState(x, variableMap);
                List<double> currentValues = new List<double>();
                foreach (var scopeElement in scopeElements)
                {
                    currentValues.AddRange(scopeElement.GetReading(currentState));
                }
                result.AddPoint(currentValues.ToArray(),t);
            }
            result.SetStateChangedCount(stateChangedCount);
            return result;
        }
        public string Solve()
        {
            string equations = GenerateEquations();
            TransientSolution solution = this.Solve(equations);
            solution.Plot();
            return solution.ToString();
        }
        public void SetT0(double t0)
        {
            this.t0 = t0;
        }
        public void SetT1(double t1)
        {
            this.t1 = t1;
        }
        public void AddPin(Pin pin)
        {
            nodeList.Add(pin);
        }
        public void SetSolver(Equations.DAE.Implicit.DAEISolver solver)
        {
            this.solver = solver;
        }
        public void AddElement(ITransientElement element)
        {
            (element as Element).SetIndex(elements.Count);
            elements.Add(element);
        }
        public string GetEquations()
        {
            return GenerateEquations();
        }
    }

    public class TransientState
    {
        private Vector<double> x;
        private Dictionary<string, int> variablesMap;

        public TransientState(Vector<double> x, Dictionary<string, int> variablesMap)
        {
            this.x = x;
            this.variablesMap = variablesMap;
        }
        public double GetValue(string name)
        {
            return x[variablesMap[name]];
        }
    }
}
