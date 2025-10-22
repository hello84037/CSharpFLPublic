﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MathApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running the MathApp to demonstrate some of it's capabilities.\n");

            int a = 5;
            int b = 2;

            Console.WriteLine($"Adding {a} and {b} makes: {MathOperations.Add(a, b)}");
            Console.WriteLine($"Subtracting  {b} from {a} makes: {MathOperations.Subtract(a, b)}");
        }
    }
}
