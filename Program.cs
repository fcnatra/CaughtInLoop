using System.Diagnostics;

namespace CaughtInLoop;

class Program
{
	private const int LOOPS = 5001;
	private const int PARALLELISM = 50;

	static void Main(string[] args)
	{
		// dumb execution - not collecting logs - init calls to collect better metrics for the rest of the calls
		NotHandlingInnerExceptions(NotProducingExeptions, new Action<string>((m) => { }));

		// actual calls to collect metrics from
		Console.WriteLine("NotAsyncHandlingInnerExceptions (ProducingExeptions)");
		MeasureExecution(() => NotAsyncHandlingInnerExceptions(ProducingExeptions, Console.WriteLine));
		Console.WriteLine("-------------------------------------------------\n");

		Console.WriteLine("WithoutHandlingInnerExceptions (NotProducingExeptions)");
		MeasureExecution(() => NotHandlingInnerExceptions(NotProducingExeptions, Console.WriteLine));
		Console.WriteLine("-------------------------------------------------\n");

		Console.WriteLine("WithoutHandlingInnerExceptions (ProducingExeptions)");
		MeasureExecution(() => NotHandlingInnerExceptions(ProducingExeptions, Console.WriteLine));
		Console.WriteLine("-------------------------------------------------\n");

		Console.WriteLine("HandlingInnerExceptions (NotProducingExeptions)");
		MeasureExecution(() => HandlingInnerExceptions(NotProducingExeptions, Console.WriteLine));
		Console.WriteLine("-------------------------------------------------\n");

		Console.WriteLine("HandlingInnerExceptions (ProducingExeptions)");
		MeasureExecution(() => HandlingInnerExceptions(ProducingExeptions, Console.WriteLine));
	}

	private static void MeasureExecution(Action action)
	{
		var sw = Stopwatch.StartNew();
		action();
		sw.Stop();
		WaitForAllLogsToBeWritten();
		Console.WriteLine($"Execution took: {sw.Elapsed}");
	}

	private static void ProducingExeptions(int loopIndex)
	{
		if (loopIndex % 200 == 0) throw new Exception($"Bad behavior on {loopIndex}");
	}

	private static void NotProducingExeptions(int loopIndex)
	{
		// do nothing
	}

	private static async void NotHandlingInnerExceptions(Action<int> Operation, Action<string> log)
	{
		var totalProcessed = 0;
		try
		{
			await Parallel.ForEachAsync(Enumerable.Range(1, LOOPS),
			new ParallelOptions() { MaxDegreeOfParallelism = PARALLELISM },
			async (i, _) =>
			{
				await Task.Run(() => log($"Item: {i} "));
				Interlocked.Increment(ref totalProcessed);
				Operation(i);
			});

		}
		catch
		{
			Console.WriteLine($"Exception scalated outside.");
		}
		log($"\nTotal processed: {totalProcessed}");
	}

	private static async void HandlingInnerExceptions(Action<int> Operation, Action<string> log)
	{
		var totalProcessed = 0;
		try
		{
			await Parallel.ForEachAsync(Enumerable.Range(1, LOOPS),
			new ParallelOptions() { MaxDegreeOfParallelism = PARALLELISM },
			async (i, _) =>
			{
				try
				{
					await Task.Run(() => log($"Item: {i} "));
					Interlocked.Increment(ref totalProcessed);
					Operation(i);
				}
				catch (System.Exception ex)
				{
					log($"Exception caught in-loop: {ex.Message}");
				}
			});
		}
		catch
		{
			Console.WriteLine($"Exception scalated outside.");
		}
		log($"\nTotal processed: {totalProcessed}");
	}

	private static void NotAsyncHandlingInnerExceptions(Action<int> Operation, Action<string> log)
	{
		var totalProcessed = 0;
		Parallel.ForEach(Enumerable.Range(1, LOOPS),
			new ParallelOptions() { MaxDegreeOfParallelism = PARALLELISM },
			(i, _) =>
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

	private static void WaitForAllLogsToBeWritten()
	{
		Task.Delay(1000).Wait();
	}
}
