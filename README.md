DispatchSharp
=============

An experimental library to make multi-threaded dispatch code more testable.

Models a job dispatch pattern and provides both threaded and non threaded implementations.

Plans and requirements
----------------------

Limited-life job handlers:
 * Deals with threading and names all threads
 * Can add items and have them started
 * can query number of items in flight
 * can set a maximum number of jobs
 * can block waiting for jobs to go below max level
 * can set max to zero (as part of shutdown)
 * can wait for all jobs to finish (as part of shutdown)
 * can do persistent store and forward for waiting jobs.

Job managers:
 * Given a ready/read/complete/cancel delegate, and a job handler
 * sleeps until ready
 * reads and sends to job handler
 * completes if no exception
 * cancels otherwise
 * Can be stopped -- waits for job handler
 * Can be started

Master manager:
 * Keeps a set of managers
 * handles starting and stopping
 * handles adding and removing managers
