using Core.Maths.Matrices;
using MathNet.Numerics.Statistics;
using System.Text;

namespace FiniteElementAnalysis.Solvers
{
    [Serializable]
    public class CoreSolverResult
    {
        public string OperationIdentifier { get; protected set; }
        public long TimeTakenToStamp { get; protected set; }
        public long TimeTakenToSolve { get; protected set; }

        public double ProportionOfKInMemoryAfterStamp { get; protected set; }
        public double ProportionOfMaxCacheSizeUsedAfterStamp { get; protected set; }
        public double ProportionOfKInMemoryAfterSolve { get; protected set; }
        public double ProportionOfMaxCacheSizeUsedAfterSolve { get; protected set; }
        public double[] UnknownsVector { get; protected set; }
        public double[] RHSVector { get; protected set; }
        public int ValidationHash { get; protected set; }
        public CoreSolverResult(
            string operationIdentifier,
            long timeTakenToStamp, 
            long timeTakenToSolve,
            double proportionOfKInMemoryAfterStamp,
            double proportionOfMaxCacheSizeUsedAfterStamp,
            double proportionOfKInMemoryAfterSolve,
            double proportionOfMaxCacheSizeUsedAfterSolve,
            double[] unknownsVector,
            double[] rhs,
            int validationHash)
        {
            OperationIdentifier = operationIdentifier;
            TimeTakenToStamp = timeTakenToStamp;
            TimeTakenToSolve = timeTakenToSolve;
            ProportionOfKInMemoryAfterStamp = proportionOfKInMemoryAfterStamp;
            ProportionOfMaxCacheSizeUsedAfterStamp = proportionOfMaxCacheSizeUsedAfterStamp;
            ProportionOfKInMemoryAfterSolve = proportionOfKInMemoryAfterSolve;
            ProportionOfMaxCacheSizeUsedAfterSolve = proportionOfMaxCacheSizeUsedAfterSolve;
            UnknownsVector = unknownsVector;
            RHSVector = rhs;
            ValidationHash = validationHash;
        }
        private CoreSolverResult() { 
        
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Statistics for operation \"{OperationIdentifier}\"");
            sb.AppendLine($"Took {TimeTakenToStamp / 1000} seconds to stamp");
            sb.AppendLine($"Took {TimeTakenToSolve / 1000} seconds to solve");
            sb.AppendLine($"Proportion of K in memory after stamp: {ProportionOfKInMemoryAfterStamp}");
            sb.AppendLine($"Proportion of max cache sized used after stamp: {ProportionOfMaxCacheSizeUsedAfterStamp}");
            sb.AppendLine($"Proportion of K in memory after solve: {ProportionOfKInMemoryAfterSolve}");
            sb.AppendLine($"Proportion of max cache sized used after solve: {ProportionOfMaxCacheSizeUsedAfterSolve}");
            return sb.ToString();
        }
        public void Print() { 
            Console.WriteLine(ToString());
        }
        public void SaveUnknownsVector(string filePath)
        {
            using (var writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                // Write the length of the unknowns vector
                writer.Write(UnknownsVector.Length);

                // Write each double value to the file
                foreach (var value in UnknownsVector)
                {
                    writer.Write(value);
                }
            }
        }
    }
}