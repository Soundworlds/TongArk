//
//
// Licensed under the MIT license.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GeneticAlgorythm
{
    public class GA
    {
        public bool doneInit = false;

        private struct bCandidate
        {
            public float fitness;
            public int[] candidate;
        }

        int populationSize = 300;
        float crossoverProb = 0.9f;
        float mutationProb = 0.2f;
        int[] target;

        Random RandomNumber;

        int gen = 0;
        int[][][] generation;
        float avgFitness = 0;
        int numGenerations = 200;
        int mutationStep = 100;

        int seedLength = 24;

        private float fitness(int[] chromosome)
        {
            // higher fitness is better
            float f = 0f;

            for (int i = 0; i < chromosome.Length - 1; i++)
            {
                f -= Math.Abs(chromosome[i] - target[i]);
            }
            return f;
        }

        private int GetRandomNumber(int minim, int maxim)
        {
            return RandomNumber.Next(minim, maxim);
        }

        public void Init(int popSize, float coverProb, float mutProb, int numGen, int[] sourceData, int[] targetData)
        {

            RandomNumber = new Random();

            seedLength = targetData.Length;
            target = new int[seedLength];
            setTarget(targetData);

            populationSize = popSize;
            crossoverProb = coverProb;
            mutationProb = mutProb;

            gen = 0;
            numGenerations = numGen;
            generation = new int[numGenerations][][];
            for (int i = 0; i < numGenerations; i++)
            {
                generation[i] = new int[populationSize][];
                for (int j = 0; j < populationSize; j++)
                    generation[i][j] = new int[seedLength];
            }
            avgFitness = 0;

            seedPopulation(0, sourceData);

            doneInit = true;
        }

        public void setTarget(int[] targetData)
        {
            target = targetData;
        }

        public void seedPopulation(int populationNumber, int[] sourceData)
        {
            // seed population
            for (var i = 0; i < populationSize; i++)
            {
                generation[populationNumber][i] = sourceData;
                avgFitness += fitness(generation[0][i]);
            }
            avgFitness /= populationSize;
        }

        private int[] runEpoch_Old()
        {

            int yFactor = 3;
            int xFactor = 6;

            bCandidate bestCandidate;
            bCandidate worstCandidate;

            bestCandidate.candidate = new int[seedLength];
            worstCandidate.candidate = new int[seedLength];

            bestCandidate.fitness = Math.Max(Math.Abs(avgFitness / yFactor), 300) * yFactor + 1;
            worstCandidate.fitness = Math.Max(Math.Abs(avgFitness / yFactor), 300) * yFactor + 1;

            // start evolving:
            gen = 0;
            int bestGen = numGenerations;
            if (gen < numGenerations)
            {
                gen++;

                // tournament selection
                int r;
                Random rrand = new Random();
                float k = 0.5f;
                int[][] candidates = new int[2][];
                int[][] parents = new int[2][];
                for (int i = 0; i < 2; i++)
                {
                    candidates[i] = new int[seedLength];
                    parents[i] = new int[seedLength];
                }

                int crossoverPoint;
                double tournamentRandom;

                // fill the new generation with as many candidates as the population size
                for (int i = 0; i < populationSize; i += 2)
                {
                    // choose parental candidates
                    for (int j = 0; j < 2; j++)
                    {
                        // chose random member of previous generation
                        r = GetRandomNumber(0, populationSize - 1);
                        candidates[0] = generation[gen - 1][r];

                        // chose random member of previous generation
                        r = GetRandomNumber(0, populationSize - 1);
                        candidates[1] = generation[gen - 1][r];

                        // run tournament to determine winning candidate

                        tournamentRandom = Convert.ToDouble(rrand.Next(100)) / 100;

                        if (fitness(candidates[0]) > fitness(candidates[1]))
                        {
                            if (tournamentRandom < k)
                            {
                                // keep fittest candidate
                                parents[j] = candidates[0];
                            }
                            else
                            {
                                parents[j] = candidates[1];
                            }
                        }
                        else
                        {
                            if (tournamentRandom < k)
                            {
                                // keep fittest candidate
                                parents[j] = candidates[1];
                            }
                            else
                            {
                                parents[j] = candidates[0];
                            }
                        }
                    }

                    // produce offspring:
                    tournamentRandom = Convert.ToDouble(rrand.Next(100)) / 100;
                    if (tournamentRandom < crossoverProb)
                    {
                        // perform crossover on parents to produce new children
                        crossoverPoint = GetRandomNumber(1, seedLength - 2);
                        Array.Copy(parents[0], generation[gen][i], crossoverPoint);
                        Array.Copy(parents[1], crossoverPoint, generation[gen][i], crossoverPoint, seedLength - crossoverPoint);

                        Array.Copy(parents[1], generation[gen][i + 1], crossoverPoint);
                        Array.Copy(parents[0], crossoverPoint, generation[gen][i + 1], crossoverPoint, seedLength - crossoverPoint);
                    }
                    else
                    {
                        // attack of the clones
                        Array.Copy(parents[0], generation[gen][i], seedLength);
                        Array.Copy(parents[1], generation[gen][i + 1], seedLength);
                    }

                    // mutate each child
                    for (var j = 0; j < 2; j++)
                    {
                        tournamentRandom = Convert.ToDouble(rrand.Next(100)) / 100;
                        if (tournamentRandom < mutationProb)
                        {
                            // chose a point in the chromosome to mutate - can be anywhere
                            int mutationPoint;
                            mutationPoint = GetRandomNumber(0, seedLength - 1);
                            generation[gen][i + j][mutationPoint] += GetRandomNumber(1, 10) - 5;
                        }
                    }
                }

                // get average and best fitness and display
                float previousAvg = avgFitness;
                float previousBest = bestCandidate.fitness;
                float previousWorst = worstCandidate.fitness;
                worstCandidate.fitness = 0;
                float f = 0;
                avgFitness = 0;
                for (int j = 0; j < populationSize; j++)
                {

                    f = fitness(generation[gen][j]);
                    avgFitness += f;
                    if (f > bestCandidate.fitness || j == 0)
                    {
                        bestCandidate.candidate = generation[gen][j];
                        bestCandidate.fitness = f;
                    }
                    else if (f < worstCandidate.fitness)
                    {
                        worstCandidate.candidate = generation[gen][j];
                        worstCandidate.fitness = f;
                    }
                }
                avgFitness /= populationSize;
            }

            return bestCandidate.candidate;
        }

        public int[] runEpoch()
        {

			int yFactor = 3;
            int xFactor = 6;

            bCandidate bestCandidate;
            bCandidate worstCandidate;

            bestCandidate.candidate = new int[seedLength];
            worstCandidate.candidate = new int[seedLength];

            bestCandidate.fitness = Math.Max(Math.Abs(avgFitness/yFactor),300)*yFactor+1;
            worstCandidate.fitness = Math.Max(Math.Abs(avgFitness/yFactor),300)*yFactor+1;

            // start evolving:
            int gen = 0;
            int bestGen = numGenerations;

            if (gen < numGenerations)
            {
                gen++;

                // tournament selection
                int r;
                Random rrand = new Random();
                float k = 0.9f;
                int[][] candidates = new int[2][];
                int[][] parents = new int[2][];
                for (int i = 0; i < 2; i++)
                {
                    candidates[i] = new int[seedLength];
                    parents[i] = new int[seedLength];
                }

                int crossoverPoint;
                double tournamentRandom;

                // fill the new generation with as many candidates as the population size
                for (int i = 0; i < populationSize; i += 2)
                {
                    // choose parental candidates
                    for (int j = 0; j < 2; j++)
                    {
                        // chose random member of previous generation
                        r = GetRandomNumber(0, populationSize - 1);
                        candidates[0] = generation[gen - 1][r];

                        // chose random member of previous generation
                        r = GetRandomNumber(0, populationSize - 1);
                        candidates[1] = generation[gen - 1][r];

                        // run tournament to determine winning candidate

                        tournamentRandom = Convert.ToDouble(rrand.Next(100)) / 100;

                        if (fitness(candidates[0]) > fitness(candidates[1]))
                        {
                            if (tournamentRandom < k)
                            {
                                // keep fittest candidate
                                parents[j] = candidates[0];
                            }
                            else
                            {
                                parents[j] = candidates[1];
                            }
                        }
                        else
                        {
                            if (tournamentRandom < k)
                            {
                                // keep fittest candidate
                                parents[j] = candidates[1];
                            }
                            else
                            {
                                parents[j] = candidates[0];
                            }
                        }
                    }

                    // produce offspring:
                    tournamentRandom = Convert.ToDouble(rrand.Next(100)) / 100;
                    if (tournamentRandom < crossoverProb)
                    {
                        // perform crossover on parents to produce new children
                        crossoverPoint = GetRandomNumber(1, seedLength - 2);
                        Array.Copy(parents[0], generation[gen][i], crossoverPoint);
                        Array.Copy(parents[1], crossoverPoint, generation[gen][i], crossoverPoint, seedLength - crossoverPoint);

                        Array.Copy(parents[1], generation[gen][i + 1], crossoverPoint);
                        Array.Copy(parents[0], crossoverPoint, generation[gen][i + 1], crossoverPoint, seedLength - crossoverPoint);
                    }
                    else
                    {
                        // attack of the clones
                        Array.Copy(parents[0], generation[gen][i], seedLength);
                        Array.Copy(parents[1], generation[gen][i + 1], seedLength);
                    }

                    // mutate each child
                    for (var j = 0; j < 2; j++)
                    {
                        tournamentRandom = Convert.ToDouble(rrand.Next(100)) / 100;
                        if (tournamentRandom < mutationProb)
                        {
                            // chose a point in the chromosome to mutate - can be anywhere
                            int mutationPoint;
                            mutationPoint = GetRandomNumber(0, seedLength - 1);
                            generation[gen][i + j][mutationPoint] += (int)(GetRandomNumber(1, mutationStep) - (mutationStep/2));
                        }
                    }
                }

                // get average and best fitness and display
                float previousAvg = avgFitness;
                float previousBest = bestCandidate.fitness;
                float previousWorst = worstCandidate.fitness;
                worstCandidate.fitness = 0;
                float f = 0;
                avgFitness = 0;
                for (int j = 0; j < populationSize; j++)
                {

                    f = fitness(generation[gen][j]);
                    avgFitness += f;
                    if (f > bestCandidate.fitness || j == 0)
                    {
                        bestCandidate.candidate = generation[gen][j];
                        bestCandidate.fitness = f;
                    }
                    else if (f < worstCandidate.fitness)
                    {
                        worstCandidate.candidate = generation[gen][j];
                        worstCandidate.fitness = f;
                    }
                }
                avgFitness /= populationSize;

                


            }

            generation[0] = generation[1];

            return bestCandidate.candidate;
        }

        public void SetCrossover(float newCrossover)
        {
            crossoverProb = newCrossover;
        }

        public void SetMutation(float newMutation)
        {
            mutationProb = newMutation;
        }

        public void SetMutationStep(int newMutationStep)
        {
            mutationStep = newMutationStep;
        }
    }
}
