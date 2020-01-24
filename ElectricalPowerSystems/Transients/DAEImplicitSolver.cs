﻿using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricalPowerSystems.Transients
{
    abstract class DESolver
    {
        public double Step { get; set; }
    }
    interface IDAEImplicitSolver
    {
       Vector<double> IntegrateStep(DAEImplicitSystem system,Vector<double> x,double t);
    }
    interface IDAESemiExplicitSolver
    {
        Vector<double> IntegrateStep(DAESemiExplicitSystem system, Vector<double> x, Vector<double> z, double t);
    }
    //TODO add BDF2 and Trapezoid
    class ImplicitEulerDAESolver:DESolver, IDAEImplicitSolver, IDAESemiExplicitSolver
    {
        int newtonIterations;
        public Vector<double> IntegrateStep(DAESemiExplicitSystem system, Vector<double> x, Vector<double> z, double t)
        {
            Vector<double> xNew = x;
            Vector<double> zNew = z;
            for (int i = 0; i < newtonIterations; i++)
            {
                Matrix<double> jacobiMatrix = Matrix<double>.Build.Dense(system.Size, system.Size);
                Vector<double> f = Vector<double>.Build.Dense(system.Size);
                double time = t + Step;
                double[] F = system.F(xNew, zNew, time);
                double[] G = system.G(xNew, zNew, time);
                double[,] dFdX = system.dFdX(xNew,zNew,time);
                double[,] dFdZ = system.dFdZ(xNew,zNew,time);
                double[,] dGdX = system.dGdX(xNew, zNew, time);
                double[,] dGdZ = system.dGdZ(xNew, zNew, time);
                for (int j = 0; j < system.SizeX; j++)
                {
                    f[j] = xNew[j]-x[j]-Step*F[j];
                    for (int k = 0; k < system.SizeX; k++)
                    {
                        jacobiMatrix[j,k] = 1.0-Step*dFdX[j,k];
                    }
                    for (int k = 0; k < system.SizeZ; k++)
                    {
                        jacobiMatrix[j, k + system.SizeX] = -Step*dFdZ[j,k];
                    }
                }
                for (int j = 0; j < system.SizeZ; j++)
                {
                    int row=j+system.SizeX;
                    f[row] = G[j];
                    for (int k = 0; k < system.SizeX; k++)
                    {
                        jacobiMatrix[row, k] = dGdX[j,k];
                    }
                    for (int k = 0; k < system.SizeZ; k++)
                    {
                        jacobiMatrix[row, k + system.SizeX] = dGdZ[j,k];
                    }

                }
                Vector<double> delta = jacobiMatrix.Solve(-f);
                xNew += delta.SubVector(0,system.SizeX);
                zNew += delta.SubVector(0, system.SizeZ);
            }
            //return x and z
            throw new NotImplementedException();
        }
        public Vector<double> IntegrateStep(DAEImplicitSystem system, Vector<double> x, double t)
        {
            //solve f(xn+1,(xn+1-xn)/h,t+h)
            Vector<double> xNew = x;
            for (int i = 0; i < newtonIterations; i++)
            {
                Vector<double> dx = (xNew-x)/Step;
                Matrix<double> jacobiMatrix = Matrix<double>.Build.Dense(system.Size, system.Size);
                Vector<double> f = Vector<double>.Build.Dense(system.Size);
                double time = t + Step;
                double[,] dFdX = system.dFdX(xNew, dx, time); //use k as dx
                double[,] dFddX = system.dFddX(xNew, dx, time);
                double[] F = system.F(xNew, dx, time);
                for (int m = 0; m < system.Size; m++)
                {
                    for (int l = 0; l < system.Size; l++)
                    {
                        jacobiMatrix[m, l] = dFdX[m, l]  + dFddX[m, l]/Step;
                    }
                }
                Vector<double> deltax = jacobiMatrix.Solve(-f);
                xNew += deltax;
            }
            return xNew;
        }
    }
    abstract class ImplicitRungeKuttaDAESolver : DESolver, IDAEImplicitSolver, IDAESemiExplicitSolver
    {
        protected double[,] a;
        protected double[] b;
        protected double[] c;
        public int Stages { get; protected set; }
        int newtonIterations;
        public Vector<double> IntegrateStep(DAESemiExplicitSystem system, Vector<double> x, Vector<double> z, double t)
        {
            int sizeX = system.SizeX * Stages;
            int sizeZ = system.SizeZ * Stages;
            Vector<double>[] kx = new Vector<double>[Stages];// vector k=[k1_1,k1_2,...k1_n,...,kn_1,...,kn_n]
            Vector<double>[] kz = new Vector<double>[Stages];// vector k=[k1_1,k1_2,...k1_n,...,kn_1,...,kn_n]

            for (int i = 0; i < Stages; i++)
            {
                kx[i] = Vector<double>.Build.Dense(system.SizeX);
                kz[i] = Vector<double>.Build.Dense(system.SizeZ);
            }


            throw new NotImplementedException();
        }
        // system is f(x,x',t)=0
        // x(n+1)=x(n)+sum k_i*b_i
        // k_i = x'(x_n + sum k_j * a_ij, t_n + h*c_i) where i is [1,Stages] and j is [1,SystemSize]
        // k_i is a vector and sizeOf(k_i) == sizeOf(x)
        // or f(x_n + sum k_j * a_ij, k_i, t_n + h * c_i)
        // solve this implicit nonlinear system for all k's 
        public Vector<double> IntegrateStep(DAEImplicitSystem system,Vector<double> x,double t)
        {
            int size = system.Size * Stages;
            Vector<double> [] k = new Vector<double> [Stages];// vector k=[k1_1,k1_2,...k1_n,...,kn_1,...,kn_n]

            for (int i = 0; i < Stages; i++)
            {
                k[i] = Vector<double>.Build.Dense(system.Size);
            }

            for (int i = 0; i < newtonIterations; i++)
            {
                Matrix<double> jacobiMatrix = Matrix<double>.Build.Dense(size, size);
                Vector<double> f = Vector<double>.Build.Dense(size);
                for (int j = 0; j < Stages; j++)
                {
                    Vector<double> t_x = x;
                    for (int m = 0; m < Stages; m++)
                    {
                        t_x += Step * a[j, m] * k[m];
                    }
                    double time = t + Step * c[j];
                    double[,] dFdX = system.dFdX(t_x, k[j], time); //use k as dx
                    double[,] dFddX = system.dFddX(t_x, k[j], time);
                    double[] F = system.F(t_x, k[j], time);
                    for (int p = 0; p < Stages; p++)
                    {
                        //subMatrix Size*Size
                        for (int m = 0; m < system.Size; m++)
                        {
                            int row = m + j * system.Size;
                            f[row] = F[m];
                            for (int l = 0; l < system.Size; l++)
                            {
                                    int column = l + p * system.Size;
                                    jacobiMatrix[row, column] = dFdX[m, l] * Step * a[j, p] + dFddX[m, l];
                            }
                        }
                    }
                }
                Vector<double> dk = jacobiMatrix.Solve(-f);
                for(int j=0;j<Stages;j++)
                    k[i] += dk.SubVector(j*system.Size,system.Size);
            }

            for (int i = 0; i < Stages; i++)
            {
                x +=k[i]* Step * b[i];
            }
            return x;
        }
    }
    class RADAUIIA3: ImplicitRungeKuttaDAESolver
    {
        public RADAUIIA3()
        {
            Stages = 2;
            a = new double[2, 2] { 
                {5.0 / 12.0, -1.0 / 12.0},
                {3.0 / 4.0, 1.0 / 4.0}
            };
            b = new double[2] {
                3.0 / 4.0,
                1.0 / 4.0
            };
            c = new double[2] {
                1.0 / 3.0,
                1.0
            };
        }
    }
    class RADAUIIA5 : ImplicitRungeKuttaDAESolver
    {
        public RADAUIIA5()
        {
            Stages = 3;
            a = new double[3, 3] {
                {
                    11.0 / 45.0 - 7.0 * Math.Sqrt(6.0) / 360.0,
                    37.0 / 225.0 - 169.0 * Math.Sqrt(6.0) / 1800.0,
                    -2.0 / 225.0 + Math.Sqrt(6.0) / 75.0
                },
                {
                    37.0 / 225.0 + 169.0 * Math.Sqrt(6.0) / 1800.0,
                    11.0 / 45.0 + 7.0 * Math.Sqrt(6.0) / 360.0,
                    -2.0 / 225.0 - Math.Sqrt(6.0) / 75.0
                },
                {
                    4.0 / 9.0 - Math.Sqrt(6.0) / 36.0,
                    4.0 / 9.0 + Math.Sqrt(6.0),
                    1.0 / 9.0
                }
            };
            b = new double[3] {
                4.0 / 9.0 - Math.Sqrt(6.0) / 36.0,
                4.0 / 9.0 + Math.Sqrt(6.0),
                1.0 / 9.0
            };
            c = new double[3] {
                2.0 / 5.0 - Math.Sqrt(6.0) / 10.0,
                2.0 / 5.0 + Math.Sqrt(6.0) / 10.0,
                1.0
            };
        }
    }
}