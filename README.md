# Caught IN-LOOP
Performance check - handling exceptions inside an awaited **Parallel.ForEachAsync** to avoid a short-circuit

## Problem
Parallel ForEach Awaited halts when an exeption is thrown inside one of the loops, producing that only part of the parallel operations are executed.
How does the performance get impacted if each of the parallel loops is executed handling the possible exceptions?
