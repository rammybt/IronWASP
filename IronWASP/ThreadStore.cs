﻿//
// Copyright 2011-2012 Lavakumar Kuppan
//
// This file is part of IronWASP
//
// IronWASP is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3 of the License.
//
// IronWASP is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with IronWASP.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace IronWASP
{
    public class ThreadStore
    {
        static Dictionary<int, Dictionary<string, object>> InternalStore = new Dictionary<int,Dictionary<string,object>>();

        static Dictionary<int, Thread> Threads = new Dictionary<int, Thread>();

        static int CleanUpCounter = 0;

        public static int ThisThread()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }

        public static void Put(string Name, object Value)
        {
            int ThreadID = Thread.CurrentThread.ManagedThreadId;
            if (!Threads.ContainsKey(ThreadID))
            {
                lock (Threads)
                {
                    try
                    {
                        Threads.Add(ThreadID, Thread.CurrentThread);
                    }
                    catch { }
                }
            }
            if (!InternalStore.ContainsKey(ThreadID))
            {
                lock (InternalStore)
                {
                    InternalStore.Add(ThreadID, new Dictionary<string, object>());
                }
            }
            lock (InternalStore[ThreadID])
            {
                if (InternalStore[ThreadID].ContainsKey(Name))
                {
                    InternalStore[ThreadID][Name] = Value;
                }
                else
                {
                    InternalStore[ThreadID].Add(Name, Value);
                }
            }
        }

        public static object Get(string Name)
        {
            try
            {
                int ThreadID = Thread.CurrentThread.ManagedThreadId;
                if (InternalStore.ContainsKey(ThreadID))
                {
                    if (InternalStore[ThreadID].ContainsKey(Name))
                    {
                        return InternalStore[ThreadID][Name];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool Has(string Name)
        {
            try
            {
                int ThreadID = Thread.CurrentThread.ManagedThreadId;
                if (InternalStore.ContainsKey(ThreadID))
                {
                    if (InternalStore[ThreadID].ContainsKey(Name))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void Remove(string Name)
        {
            try
            {
                int ThreadID = Thread.CurrentThread.ManagedThreadId;
                if (InternalStore.ContainsKey(ThreadID))
                {
                    lock (InternalStore)
                    {
                        InternalStore[ThreadID].Remove(Name);
                    }
                }
            }
            catch { }
        }

        public static void Clear()
        {
            try
            {
                int ThreadID = Thread.CurrentThread.ManagedThreadId;
                if (Threads.ContainsKey(ThreadID))
                {
                    lock (Threads)
                    {
                        Threads.Remove(ThreadID);
                    }
                }
                if (InternalStore.ContainsKey(ThreadID))
                {
                    lock (InternalStore)
                    {
                        InternalStore.Remove(ThreadID);
                    }
                }
            }
            catch { }
        }

        internal static void CleanUp()
        {
            CleanUpCounter++;
            if (CleanUpCounter < 20) return;
            CleanUpCounter = 0;

            List<int> DeadThreads = new List<int>();
            foreach (int ThreadID in Threads.Keys)
            {
                try
                {
                    if (Threads[ThreadID] == null)
                    {
                        DeadThreads.Add(ThreadID);
                    }
                    else if (Threads[ThreadID].ThreadState == ThreadState.Aborted || Threads[ThreadID].ThreadState == ThreadState.Stopped || Threads[ThreadID].ThreadState == ThreadState.Suspended)
                    {
                        DeadThreads.Add(ThreadID);
                    }
                }
                catch
                {
                    DeadThreads.Add(ThreadID);
                }
            }
            if (DeadThreads.Count > 0)
            {
                lock (InternalStore)
                {
                    foreach (int ThreadID in DeadThreads)
                    {
                        try
                        {
                            if (InternalStore.ContainsKey(ThreadID))
                            {
                                InternalStore.Remove(ThreadID);
                            }
                        }
                        catch { }
                    }
                }
                lock (Threads)
                {
                    foreach (int ThreadID in DeadThreads)
                    {
                        try
                        {
                            if (Threads.ContainsKey(ThreadID))
                            {
                                Threads.Remove(ThreadID);
                            }
                        }
                        catch { }
                    }
                }
            }
        }
    }
}