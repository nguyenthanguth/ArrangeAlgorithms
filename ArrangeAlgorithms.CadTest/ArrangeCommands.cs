using Autodesk.AutoCAD.Runtime;
using ArrangeAlgorithms;

namespace ArrangeAlgorithms.CadTest
{
    /// <summary>
    /// Đăng ký các lệnh CommandMethod tĩnh với AutoCAD.
    /// </summary>
    public static class ArrangeCommands
    {
        [CommandMethod("T1_Greedy")]
        public static void RunArrangeTestGreedy()
        {
            new ArrangeTestRunner().RunArrangeTest(ArrangeAlgorithmType.Greedy, "Greedy");
        }

        [CommandMethod("T1_BoundedBacktracking")]
        public static void RunArrangeTestBacktracking()
        {
            new ArrangeTestRunner().RunArrangeTest(ArrangeAlgorithmType.BoundedBacktracking, "Bounded Backtracking");
        }

        [CommandMethod("T1_SimulatedAnnealing")]
        public static void RunArrangeTestSimulatedAnnealing()
        {
            new ArrangeTestRunner().RunArrangeTest(ArrangeAlgorithmType.SimulatedAnnealing, "Simulated Annealing");
        }

        [CommandMethod("T1_ForceDirected")]
        public static void RunArrangeTestForceDirected()
        {
            new ArrangeTestRunner().RunArrangeTest(ArrangeAlgorithmType.ForceDirected, "Force Directed");
        }

        [CommandMethod("T1_ConstraintSatisfaction")]
        public static void RunArrangeTestCSP()
        {
            new ArrangeTestRunner().RunArrangeTest(ArrangeAlgorithmType.ConstraintSatisfaction, "Constraint Satisfaction");
        }
    }
}
