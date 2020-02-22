﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectricalPowerSystems.ACGraph;
using MathNet.Numerics;

namespace ElectricalPowerSystems.PowerGraph
{
    public class GraphFaultOC:GraphElement
    {
        public enum Phases
        {
            A = 0x0001,
            B = 0x0010,
            C = 0x0100,
            AB = 0x0011,
            AC = 0x0101,
            BC = 0x0110,
            ABC = 0x0111
        };
        public Phases Type;
        public GraphFaultOC(string node1, string node2, Phases type) : base()
        {
            nodes.Add(node1);
            nodes.Add(node2);
            this.Type = type;
        }
        public override PowerElementScheme generateACGraph(List<ABCNode> nodes, ACGraph.ACGraph acGraph)
        {
            return new FaultOCScheme(nodes,acGraph,this);
            //throw new NotImplementedException();
        }

    }
    public class FaultOCScheme : PowerElementScheme
    {
        int inA;
        int inB;
        int inC;
        int outA;
        int outB;
        int outC;

        int lineA;
        int lineB;
        int lineC;

        public FaultOCScheme(List<ABCNode> nodes, ACGraph.ACGraph acGraph, GraphFaultOC fault) : base()
        {
            inA = nodes[0].A;
            inB = nodes[0].B;
            inC = nodes[0].C;
            outA = nodes[1].A;
            outB = nodes[1].B;
            outC = nodes[1].C;
            generate(acGraph, fault);
        }
        private void generate(ACGraph.ACGraph acGraph, GraphFaultOC fault)
        {
            lineA = -1;
            lineB = -1;
            lineC = -1;
            if (((int)fault.Type & (int)GraphFaultOC.Phases.A) ==0 )
            {
                lineA = acGraph.CreateLine(inA, outA);
            }
            if (((int)fault.Type & (int)GraphFaultOC.Phases.B) == 0)
            {
                lineB = acGraph.CreateLine(inB, outB);
            }
            if (((int)fault.Type & (int)GraphFaultOC.Phases.C) == 0)
            {
                lineC = acGraph.CreateLine(inC, outC);
            }
        }
    }

    public class GraphFaultSCLLG : GraphElement
    {
        public enum Phases
        {
            AB,
            BC,
            CA
        };
        public Complex32 Zl;
        public Complex32 Zg;
        public Phases phases;
        public GraphFaultSCLLG(string node1, string node2, Complex32 Zg, Complex32 Zl,Phases phases) : base()
        {
            nodes.Add(node1);
            nodes.Add(node2);
            this.Zg = Zg;
            this.Zl = Zl;
            this.phases = phases;
        }
        public override PowerElementScheme generateACGraph(List<ABCNode> nodes, ACGraph.ACGraph acGraph)
        {
            return new FaultSCLLGScheme(nodes, acGraph, this);
        }
    }
    public class FaultSCLLGScheme : PowerElementScheme
    {
        int inA;
        int inB;
        int inC;
        int outA;
        int outB;
        int outC;
        int cA;
        int cB;
        int cC;
        int lineInA;
        int lineInB;
        int lineInC;
        int lineOutA;
        int lineOutB;
        int lineOutC;
        int commonNode;
        int groundNode;
        public FaultSCLLGScheme(List<ABCNode> nodes, ACGraph.ACGraph acGraph, GraphFaultSCLLG fault) : base()
        {
            inA = nodes[0].A;
            inB = nodes[0].B;
            inC = nodes[0].C;
            outA = nodes[1].A;
            outB = nodes[1].B;
            outC = nodes[1].C;
            commonNode = acGraph.AllocateNode();
            groundNode = acGraph.AllocateNode();
            generate(acGraph, fault);
        }
        private void generate(ACGraph.ACGraph acGraph, GraphFaultSCLLG fault)
        {
            acGraph.CreateGround(groundNode);
            cA = acGraph.AllocateNode();
            cB = acGraph.AllocateNode();
            cC = acGraph.AllocateNode();
            lineInA = acGraph.CreateLine(cA, inA);
            lineInB = acGraph.CreateLine(cB, inB);
            lineInC = acGraph.CreateLine(cC, inC);
            lineOutA = acGraph.CreateLine(cA, outA);
            lineOutB = acGraph.CreateLine(cB, outB);
            lineOutC = acGraph.CreateLine(cC, outC);
            acGraph.CreateImpedanceWithCurrent(groundNode,commonNode,fault.Zg);
            switch (fault.phases)
            {
                case GraphFaultSCLLG.Phases.AB:
                    acGraph.CreateImpedance(commonNode, cA, fault.Zl);
                    acGraph.CreateImpedance(commonNode, cB, fault.Zl);
                    break;
                case GraphFaultSCLLG.Phases.BC:
                    acGraph.CreateImpedance(commonNode, cB, fault.Zl);
                    acGraph.CreateImpedance(commonNode, cC, fault.Zl);
                    break;
                case GraphFaultSCLLG.Phases.CA:
                    acGraph.CreateImpedance(commonNode, cC, fault.Zl);
                    acGraph.CreateImpedance(commonNode, cA, fault.Zl);
                    break;
            }
        }
    }

    public class GraphFaultSCLLLG : GraphElement
    {
        public Complex32 Zl;
        public Complex32 Zg;
        public GraphFaultSCLLLG(string node1, string node2, Complex32 Zg, Complex32 Zl) : base()
        {
            nodes.Add(node1);
            nodes.Add(node2);
            this.Zg = Zg;
            this.Zl = Zl;
        }
        public override PowerElementScheme generateACGraph(List<ABCNode> nodes, ACGraph.ACGraph acGraph)
        {
            return new FaultSCLLLGScheme(nodes, acGraph, this);
        }
    }
    public class FaultSCLLLGScheme:PowerElementScheme
    {
        int inA;
        int inB;
        int inC;
        int outA;
        int outB;
        int outC;
        int cA;
        int cB;
        int cC;
        int commonNode;
        int groundNode;


        int lineInA;
        int lineInB;
        int lineInC;
        int lineOutA;
        int lineOutB;
        int lineOutC;
        public FaultSCLLLGScheme(List<ABCNode> nodes, ACGraph.ACGraph acGraph, GraphFaultSCLLLG fault) : base()
        {
            inA = nodes[0].A;
            inB = nodes[0].B;
            inC = nodes[0].C;
            outA = nodes[1].A;
            outB = nodes[1].B;
            outC = nodes[1].C;
            commonNode = acGraph.AllocateNode();
            groundNode = acGraph.AllocateNode();
            generate(acGraph, fault);
        }
        private void generate(ACGraph.ACGraph acGraph, GraphFaultSCLLLG fault)
        {
            acGraph.CreateGround(groundNode);
            cA = acGraph.AllocateNode();
            cB = acGraph.AllocateNode();
            cC = acGraph.AllocateNode();
            lineInA = acGraph.CreateLine(cA, inA);
            lineInB = acGraph.CreateLine(cB, inB);
            lineInC = acGraph.CreateLine(cC, inC);
            lineOutA = acGraph.CreateLine(cA, outA);
            lineOutB = acGraph.CreateLine(cB, outB);
            lineOutC = acGraph.CreateLine(cC, outC);
            acGraph.CreateImpedance(commonNode, cA, fault.Zl);
            acGraph.CreateImpedance(commonNode, cB, fault.Zl);
            acGraph.CreateImpedance(commonNode, cC, fault.Zl);
            acGraph.CreateImpedanceWithCurrent(groundNode, commonNode, fault.Zg);
        }
    }
    public class GraphFaultSCLG : GraphElement
    {
        public enum Phase
        {
            A,
            B,
            C
        };
        public Phase phase;
        public Complex32 Zg;
        public GraphFaultSCLG(string node1, string node2, Complex32 Zg, Phase phase):base()
        {
            nodes.Add(node1);
            nodes.Add(node2);
            this.Zg = Zg;
            this.phase = phase;
        }
        public override PowerElementScheme generateACGraph(List<ABCNode> nodes, ACGraph.ACGraph acGraph)
        {
            return new FaultSCLGScheme(nodes, acGraph, this);
        }
    }
    public class FaultSCLGScheme : PowerElementScheme
    {
        int inA;
        int inB;
        int inC;
        int outA;
        int outB;
        int outC;
        int cA;
        int cB;
        int cC;
        int groundNode;


        int lineInA;
        int lineInB;
        int lineInC;
        int lineOutA;
        int lineOutB;
        int lineOutC;
        public FaultSCLGScheme(List<ABCNode> nodes, ACGraph.ACGraph acGraph, GraphFaultSCLG fault) : base()
        {
            inA = nodes[0].A;
            inB = nodes[0].B;
            inC = nodes[0].C;
            outA = nodes[1].A;
            outB = nodes[1].B;
            outC = nodes[1].C;
            groundNode = acGraph.AllocateNode();
            generate(acGraph, fault);
        }
        private void generate(ACGraph.ACGraph acGraph, GraphFaultSCLG fault)
        {
            cA = acGraph.AllocateNode();
            cB = acGraph.AllocateNode();
            cC = acGraph.AllocateNode();
            lineInA = acGraph.CreateLine(cA, inA);
            lineInB = acGraph.CreateLine(cB, inB);
            lineInC = acGraph.CreateLine(cC, inC);
            lineOutA = acGraph.CreateLine(cA, outA);
            lineOutB = acGraph.CreateLine(cB, outB);
            lineOutC = acGraph.CreateLine(cC, outC);
            acGraph.CreateGround(groundNode);
            switch (fault.phase)
            {
                case GraphFaultSCLG.Phase.A:
                    acGraph.CreateImpedanceWithCurrent(cA,groundNode,fault.Zg);
                    break;
                case GraphFaultSCLG.Phase.B:
                    acGraph.CreateImpedanceWithCurrent(cB, groundNode, fault.Zg);
                    break;
                case GraphFaultSCLG.Phase.C:
                    acGraph.CreateImpedanceWithCurrent(cC, groundNode, fault.Zg);
                    break;
            }
        }
    }
    public class GraphFaultSCLL : GraphElement
    {
        public enum Phases
        {
            AB,
            BC,
            CA
        };
        public Phases phases;
        public Complex32 Zl;
        public GraphFaultSCLL(string node1, string node2, Complex32 Zl,Phases phases):base()
        {
            nodes.Add(node1);
            nodes.Add(node2);
            this.Zl = Zl;
            this.phases = phases;
        }
        public override PowerElementScheme generateACGraph(List<ABCNode> nodes, ACGraph.ACGraph acGraph)
        {
            return new FaultSCLLScheme(nodes, acGraph, this);
        }
    }
    public class FaultSCLLScheme : PowerElementScheme
    {
        int inA;
        int inB;
        int inC;
        int outA;
        int outB;
        int outC;
        int cA;
        int cB;
        int cC;

        int lineInA;
        int lineInB;
        int lineInC;
        int lineOutA;
        int lineOutB;
        int lineOutC;
        public FaultSCLLScheme(List<ABCNode> nodes, ACGraph.ACGraph acGraph, GraphFaultSCLL fault) : base()
        {
            inA = nodes[0].A;
            inB = nodes[0].B;
            inC = nodes[0].C;
            outA = nodes[1].A;
            outB = nodes[1].B;
            outC = nodes[1].C;
            generate(acGraph, fault);
        }
        private void generate(ACGraph.ACGraph acGraph, GraphFaultSCLL fault)
        {
            cA = acGraph.AllocateNode();
            cB = acGraph.AllocateNode();
            cC = acGraph.AllocateNode();
            lineInA = acGraph.CreateLine(cA, inA);
            lineInB = acGraph.CreateLine(cB, inB);
            lineInC = acGraph.CreateLine(cC, inC);
            lineOutA = acGraph.CreateLine(cA, outA);
            lineOutB = acGraph.CreateLine(cB, outB);
            lineOutC = acGraph.CreateLine(cC, outC);
            switch (fault.phases)
            {
                case GraphFaultSCLL.Phases.AB:
                    acGraph.CreateImpedance(cA, cB, fault.Zl);
                    break;
                case GraphFaultSCLL.Phases.BC:
                    acGraph.CreateImpedance(cB, cC, fault.Zl);
                    break;
                case GraphFaultSCLL.Phases.CA:
                    acGraph.CreateImpedance(cC, cA, fault.Zl);
                    break;
            }
        }
    }
    public class GraphFaultSCLLL : GraphElement
    {
        public Complex32 Zl;
        public GraphFaultSCLLL(string node1,string node2, Complex32 Zl) : base()
        {
            nodes.Add(node1);
            nodes.Add(node2);
            this.Zl = Zl;
        }
        public override PowerElementScheme generateACGraph(List<ABCNode> nodes, ACGraph.ACGraph acGraph)
        {
            return new FaultSCLLLScheme(nodes, acGraph, this);
        }
    }
    public class FaultSCLLLScheme : PowerElementScheme
    {
        int inA;
        int inB;
        int inC;
        int outA;
        int outB;
        int outC;
        int cA;
        int cB;
        int cC;
        int commonNode;
        int lineInA;
        int lineInB;
        int lineInC;
        int lineOutA;
        int lineOutB;
        int lineOutC;
        public FaultSCLLLScheme(List<ABCNode> nodes, ACGraph.ACGraph acGraph, GraphFaultSCLLL fault) : base()
        {
            inA = nodes[0].A;
            inB = nodes[0].B;
            inC = nodes[0].C;
            outA = nodes[1].A;
            outB = nodes[1].B;
            outC = nodes[1].C;
            commonNode = acGraph.AllocateNode();
            generate(acGraph, fault);
        }
        private void generate(ACGraph.ACGraph acGraph, GraphFaultSCLLL fault)
        {
            cA = acGraph.AllocateNode();
            cB = acGraph.AllocateNode();
            cC = acGraph.AllocateNode();
            lineInA = acGraph.CreateLine(cA, inA);
            lineInB = acGraph.CreateLine(cB, inB);
            lineInC = acGraph.CreateLine(cC, inC);
            lineOutA = acGraph.CreateLine(cA, outA);
            lineOutB = acGraph.CreateLine(cB, outB);
            lineOutC = acGraph.CreateLine(cC, outC);
            acGraph.CreateGround(commonNode);
            acGraph.CreateImpedanceWithCurrent(cA, commonNode, fault.Zl);
            acGraph.CreateImpedanceWithCurrent(cB, commonNode, fault.Zl);
            acGraph.CreateImpedanceWithCurrent(cC, commonNode, fault.Zl);
        }
    }
}

