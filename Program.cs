using System.Diagnostics;

namespace CaughtInLoop;

class Program
{
    private const int LOOPS = 5001;
    private const int PARALLELISM = 50;

    static void Main(string[] args)
    {
        WithoutHandlingInnerExceptions(NotProducingExeptions, new Action<string>((m) => {}));

        Console.WriteLine("WithoutHandlingInnerExceptions(NotProducingExeptions)");
        MeasureExecution(() => WithoutHandlingInnerExceptions(NotProducingExeptions, Console.WriteLine));
        Console.WriteLine("-------------------------------------------------");

        Console.WriteLine("WithoutHandlingInnerExceptions(ProducingExeptions)");
        MeasureExecution(() => WithoutHandlingInnerExceptions(ProducingExeptions, Console.WriteLine));
        Console.WriteLine("-------------------------------------------------");

        Console.WriteLine("HandlingInnerExceptions(NotProducingExeptions)");
        MeasureExecution(() => HandlingInnerExceptions(NotProducingExeptions, Console.WriteLine));
        Console.WriteLine("-------------------------------------------------");

        Console.WriteLine("HandlingInnerExceptions(ProducingExeptions)");
        MeasureExecution(() => HandlingInnerExceptions(ProducingExeptions, Console.WriteLine));
    }

    private static void ProducingExeptions(int loopIndex)
    {
        if (loopIndex%1000 == 0) throw new Exception($"Bad behavior on {loopIndex}");
    }

    private static void NotProducingExeptions(int loopIndex)
    {
        // do nothing
    }

    private static async void HandlingInnerExceptions(Action<int> Operation, Action<string> log)
    {
        var totalProcessed = 0;
        await Parallel.ForEachAsync(Enumerable.Range(1, LOOPS),
            new ParallelOptions() { MaxDegreeOfParallelism = PARALLELISM },
            async (i, _) =>
            {
                try
                {
                    log($"Item: {i} ");
                    Interlocked.Increment(ref totalProcessed);
                    Operation(i);
                }
                catch (System.Exception ex)
                {
                    log($"Exception caught in-loop: {ex.Message}");
                }
            });
            
        log($"\nTotal processed: {totalProcessed}");
    }


    private static async void WithoutHandlingInnerExceptions(Action<int> Operation, Action<string> log)
    {
        var totalProcessed = 0;
        await Parallel.ForEachAsync(Enumerable.Range(1, LOOPS),
            new ParallelOptions() { MaxDegreeOfParallelism = PARALLELISM },
            async (i, _) =>
            {
                log($"Item: {i} ");
                Interlocked.Increment(ref totalProcessed);
                Operation(i);
            });
            
        log($"\nTotal processed: {totalProcessed}");
    }

    private static void MeasureExecution(Action action)
    {
        var sw = Stopwatch.StartNew();
        try { action(); }
        catch {
            Console.WriteLine($"Exception scalated outside {action.Method.Name}");
        }
        sw.Stop();
        Console.WriteLine($"Execution took: {sw.Elapsed}");
    }
}
