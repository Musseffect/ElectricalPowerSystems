﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//https://ptolemy.berkeley.edu/projects/embedded/eecsx44/fall2011/lectures/01-modelling.pdf
namespace ElectricalPowerSystems
{
    public class Element
    {
        public int[] nodes;
        public Element()
        {
            nodes = null;
        }
    }
    public class Element2N : Element
    {
        public Element2N(int node1, int node2)
        {
            nodes = new int []{ node1,node2};
        }
    }
    public class Element1N : Element
    {
        public Element1N(int node)
        {
            nodes = new int[] { node};
        }
    }
    public class Resistor : Element2N
    {
        public float resistance;
        public Resistor(int node1, int node2,float resistance):base(node1,node2)
        {
            this.resistance = resistance;
        }
    }
    public class Capacitor : Element2N
    {
        public float capacity;
        public Capacitor(int node1, int node2, float capacity) : base(node1, node2)
        {
            this.capacity = capacity;
        }
    }
    public class Inductor : Element2N
    {
        public float inductivity;
        public Inductor(int node1, int node2, float inductivity) : base(node1, node2)
        {
            this.inductivity = inductivity;
        }
    }
    public class Ground : Element1N
    {
        public Ground(int node) : base(node)
        {
        }
    }
    public class Line : Element2N
    {
        public Line(int node1, int node2) : base(node1, node2)
        {
        }
    }
    public class VoltageSource : Element2N
    {
        public float voltage;
        public VoltageSource(int node1, int node2, float voltage) : base(node1, node2)
        {
            this.voltage = voltage;
        }
    }
    public class CurrentSource : Element2N
    {
        public float current;
        public CurrentSource(int node1, int node2, float current) : base(node1, node2)
        {
            this.current = current;
        }
    }
    public class Node
    {
        public bool grounded;
        public List<int> connectedElements;
        public Node()
        {
            grounded = false;
            connectedElements = new List<int>();
        }
    }
    class ModelGraphCreator
    {
        Dictionary<string,int> nodes;
        public List<Element> elements;
        public int nodeId;
        public List<int> voltageSources;
        public List<int> currentSources;
        public List<Node> nodesList;
        public List<int> outputCurrents;
        public List<int> lines;
        public List<int> inductors;
        public int groundsCount;
        public class NodePair
        {
            public int node1;
            public int node2;
            public NodePair(int node1,int node2)
            {
                this.node1 = node1;
                this.node2 = node2;
            }
        }
        List<NodePair> outputVoltageDifference;
        List<int> outputNodeVoltage;
        public void addOutputCurrent(int element)
        {
            if (element >= elements.Count)
                throw new Exception("Invalid element index");
            outputCurrents.Add(element);
        }
        public void addOutputVoltage(int element)
        {
            if (element >= elements.Count)
                throw new Exception("Invalid element index");
            Element el = elements[element];
            if (el is Element1N)
            {
                outputNodeVoltage.Add(((Element1N)el).nodes[0]);
            }else
                outputVoltageDifference.Add(new NodePair(((Element2N)el).nodes[1], ((Element2N)el).nodes[0]));
        }
        public void addOutputVoltage(string node)
        {
            int nodeId;
            try
            {
                nodeId = nodes[node];
            } catch (KeyNotFoundException)
            {
                throw new Exception("Invalid node label");
            }
            outputNodeVoltage.Add(nodeId);
        }
        public void addOutputVoltage(string node1, string node2)
        {
            int node1Id;
            int node2Id;
            try
            {
                node1Id = nodes[node1];
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("Invalid node label");
            }
            try
            {
                node2Id = nodes[node2];
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("Invalid node label");
            }
            outputVoltageDifference.Add(new NodePair(node2Id, node1Id));
        }
        private int retrieveNodeId(string key)
        {
            int node=nodeId;
            try
            {
                node = nodes[key];
            }
            catch (KeyNotFoundException)
            {
                nodes.Add(key,node);
                nodeId++;
                Node nd = new Node();
                nodesList.Add(nd);
            }
            return node;
        }
        public ModelGraphCreator()
        {
            nodes = new Dictionary<string, int>();
            elements = new List<Element>();
            nodeId = 0;
            voltageSources=new List<int>();
            currentSources=new List<int>();
            nodesList=new List<Node>();
            lines = new List<int>();
            inductors = new List<int>();
            outputNodeVoltage = new List<int>();
            outputVoltageDifference = new List<NodePair>();
            outputCurrents = new List<int>();
            groundsCount = 0;
        }
        public int addResistor(string node1, string node2,float resistance)
        {
            int node1Id = retrieveNodeId(node1);
            int node2Id = retrieveNodeId(node2);
            nodesList[node1Id].connectedElements.Add(elements.Count);
            nodesList[node2Id].connectedElements.Add(elements.Count);
            elements.Add(new Resistor(node1Id, node2Id, resistance));
            return elements.Count - 1;
        }
        public int addLine(string node1, string node2)
        {
            int node1Id = retrieveNodeId(node1);
            int node2Id = retrieveNodeId(node2);
            nodesList[node1Id].connectedElements.Add(elements.Count);
            nodesList[node2Id].connectedElements.Add(elements.Count);
            lines.Add(elements.Count);
            elements.Add(new Line(node1Id, node2Id));
            return elements.Count - 1;
        }
        public int addCapacitor(string node1, string node2,float capacity)
        {
            int node1Id = retrieveNodeId(node1);
            int node2Id = retrieveNodeId(node2);
            nodesList[node1Id].connectedElements.Add(elements.Count);
            nodesList[node2Id].connectedElements.Add(elements.Count);
            elements.Add(new Capacitor(node1Id, node2Id, capacity));
            return elements.Count - 1;
        }
        public int addVoltageSource(string node1, string node2,float voltage)
        {
            int node1Id = retrieveNodeId(node1);
            int node2Id = retrieveNodeId(node2);
            nodesList[node1Id].connectedElements.Add(elements.Count);
            nodesList[node2Id].connectedElements.Add(elements.Count);
            voltageSources.Add(elements.Count);
            elements.Add(new VoltageSource(node1Id, node2Id, voltage));
            return elements.Count - 1;
        }
        public int addCurrentSource(string node1, string node2, float current)
        {
            int node1Id = retrieveNodeId(node1);
            int node2Id = retrieveNodeId(node2);
            nodesList[node1Id].connectedElements.Add(elements.Count);
            nodesList[node2Id].connectedElements.Add(elements.Count);
            currentSources.Add(elements.Count);
            elements.Add(new CurrentSource(node1Id, node2Id, current));
            return elements.Count - 1;
        }
        public void addGround(string node)
        {
            int nodeId = retrieveNodeId(node);
            nodesList[nodeId].connectedElements.Add(elements.Count);
            nodesList[nodeId].grounded = true;
            elements.Add(new Ground(nodeId));
            groundsCount++;
        }
        public int addInductor(string node1, string node2,int inductivity)
        {
            int node1Id = retrieveNodeId(node1);
            int node2Id = retrieveNodeId(node2);
            nodesList[node1Id].connectedElements.Add(elements.Count);
            nodesList[node2Id].connectedElements.Add(elements.Count);
            inductors.Add(elements.Count);
            elements.Add(new Inductor(node1Id, node2Id, inductivity));
            return elements.Count - 1;
        }
        public bool validate(ref List<string> errors)
        {
            //no loops with only voltage sources
            //no series connected current sources
            //each region should have a ground node
            BitArray nodesUsed=new BitArray(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodesUsed[i] == true)
                    continue;
                bool hasGround = false;
                Stack<int> nodesStack = new Stack<int>();
                nodesStack.Push(i);
                while (nodesStack.Count > 0)
                {
                    int node = nodesStack.Pop();
                    nodesUsed[node] = true;
                    if (nodesList[node].grounded)
                        hasGround = true;
                    foreach (int elementId in nodesList[node].connectedElements)
                    {
                        Element element = elements[elementId];
                        foreach (int index in element.nodes)
                        {
                            if (index != node&& nodesUsed[index]==false)
                                nodesStack.Push(index);
                        }

                    }
                }
                if (hasGround == false)
                {
                    errors.Add("Add ground to the circuit.");
                    return false;
                }
            }
            return true;
        }
    }
}
