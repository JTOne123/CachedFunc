﻿using MagicEastern.CachedFunc;
using MagicEastern.CachedFunc.Core;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace TestCore
{
    class Program
    {
        static int SlowFunc(int n) {
            Console.WriteLine("SlowFunc is running ... ");
            Thread.Sleep(1000);
            return n;
        }

        static int SlowFunc()
        {
            Console.WriteLine("SlowFunc is running ... ");
            Thread.Sleep(1000);
            return 123;
        }

        static Task<int> CreateTask(int n, int taskid, Func<int, int> func) {
            Task<int> t = new Task<int>(() => {
                int ret = func(n);
                Console.WriteLine($"Task {taskid} finished!");
                return ret;
            });
            return t;
        }

        static void ConcurrentTest() {
            Random rand = new Random();
            int n = rand.Next();

            CachedFunc<int> cachedFunc = ((Func<int>)SlowFunc).ToCachedFunc(new CachedFuncOptions { AbsoluteExpirationRelativeToNow = new TimeSpan(1, 0, 0) });  //CachedFunc.Create<int>(SlowFunc, new CachedFuncOptions { AbsoluteExpirationRelativeToNow = new TimeSpan(1, 0, 0) });
            cachedFunc();
            var t1 = CreateTask(n, 1, (i) => cachedFunc());
            var t2 = CreateTask(n, 2, (i) => cachedFunc());
            t1.Start();
            t2.Start();
            Task.WaitAll(t1, t2);
        }

        static void Main(string[] args)
        {
            ConcurrentTest();
            Console.WriteLine("");
            PerformanceTest();
            Console.ReadKey();
        }

        static int SomeFunc(int n)
        {
            for (int i = 0; i < 1000; i++)
            {
                n = n.GetHashCode();
            }
            return n;
        }



        static void PerformanceTest()
        {
            int arySize = 100000;
            int[] ary = new int[arySize];

            Random rand = new Random();
            for (int i = 0; i < ary.Length; i++)
            {
                ary[i] = rand.Next();
            }

            Console.WriteLine("Using MemoryCache");
            CachedFunc<int, int> cachedFunc = ((Func<int, int>)SomeFunc).ToCachedFunc(new CachedFuncOptions { AbsoluteExpirationRelativeToNow = new TimeSpan(1, 0, 0) }); //CachedFunc.Create<int, int>(SomeFunc, new CachedFuncOptions { AbsoluteExpirationRelativeToNow = new TimeSpan(1, 0, 0) });
            BenchMarkCachedFunc<int, int>(SomeFunc, cachedFunc, ary, VerifyResults);
            Console.WriteLine("");
            cachedFunc = ((Func<int, int>)SomeFunc).ToCachedFunc(); 
            Console.WriteLine("Using Dictionary");
            BenchMarkCachedFunc<int, int>(SomeFunc, cachedFunc, ary, VerifyResults);
        }

        static void BenchMarkCachedFunc<T, TResult>(Func<T, TResult> func, CachedFunc<T, TResult> cachedFunc, T[] inputAry, Action<TResult[]> verifyFunc)
        {
            DateTime start;
            DateTime end;

            TResult[] res = new TResult[inputAry.Length];

            start = DateTime.Now;
            for (int i = 0; i < inputAry.Length; i++)
            {
                res[i] = func(inputAry[i]);
            }
            end = DateTime.Now;
            verifyFunc(res);
            Console.WriteLine($"Normal pass: {end.Subtract(start).TotalMilliseconds}ms");

            start = DateTime.Now;
            for (int i = 0; i < inputAry.Length; i++)
            {
                res[i] = cachedFunc(inputAry[i]);
            }
            end = DateTime.Now;
            verifyFunc(res);
            Console.WriteLine($"CachedFunc 1st pass: {end.Subtract(start).TotalMilliseconds}ms");

            //Thread.Sleep(500);

            start = DateTime.Now;
            for (int i = 0; i < inputAry.Length; i++)
            {
                res[i] = cachedFunc(inputAry[i]);
            }
            end = DateTime.Now;
            verifyFunc(res);
            Console.WriteLine($"CachedFunc 2st pass: {end.Subtract(start).TotalMilliseconds}ms");
        }

        static void VerifyResults(int[] res)
        {
            using (var hash = MD5.Create())
            {
                byte[] buf = new byte[sizeof(int) * res.Length];
                for (int i = 0; i < res.Length; i++)
                {
                    byte[] b = BitConverter.GetBytes(res[i]);
                    Array.Copy(b, 0, buf, i * sizeof(int), b.Length);
                }
                byte[] hashed = hash.ComputeHash(buf);
                string hex = BitConverter.ToString(hashed);
                Console.WriteLine($"Result's hash value: {hex}");
            }
        }
    }
}
